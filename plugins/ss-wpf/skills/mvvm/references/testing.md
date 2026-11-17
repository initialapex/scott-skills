# ViewModel unit tests (MSTest, hand-written fakes)

Every ViewModel can be constructed in a test without a window, a dispatcher, or a
device. Tests replace each service interface with a small hand-written fake. No
mocking library is used, in any environment: fakes are plain classes that every
toolchain compiles, and they read clearly in a review.

## Test project

| Env | Project | Framework |
|-----|---------|-----------|
| A | SDK-style MSTest project, same `net8.0-windows` target, `<UseWPF>true</UseWPF>` only if a test needs WPF types | `MSTest` package (MSTest.Sdk or MSTest.TestFramework + MSTest.TestAdapter) |
| B | Unit Test Project (.NET Framework) on the same framework version | `MSTest.TestFramework` + `MSTest.TestAdapter` packages |
| C | Visual Studio 2012 "Unit Test Project" template | `Microsoft.VisualStudio.QualityTools.UnitTestFramework` (built in) |

Layout mirrors the application: `MyApp.Tests/ViewModels/RecipeEditorViewModelTests.cs`,
`MyApp.Tests/Fakes/FakeRecipeService.cs`.

## Fakes

A fake records what was called and returns what the test set up.

```csharp
// C# 5 compatible, so the same fake works in every environment.
public sealed class FakeRecipeService : IRecipeService
{
    private readonly List<Recipe> _saved = new List<Recipe>();

    public FakeRecipeService()
    {
        RecipesToLoad = new List<Recipe>();
    }

    public List<Recipe> RecipesToLoad { get; set; }
    public Exception LoadException { get; set; }

    public IList<Recipe> Saved
    {
        get { return _saved; }
    }

    public Task<IList<Recipe>> LoadAllAsync(CancellationToken token)
    {
        if (LoadException != null)
        {
            TaskCompletionSource<IList<Recipe>> failed = new TaskCompletionSource<IList<Recipe>>();
            failed.SetException(LoadException);
            return failed.Task;
        }

        return Task.FromResult<IList<Recipe>>(RecipesToLoad);
    }

    public Task SaveAsync(Recipe recipe)
    {
        _saved.Add(recipe);
        return Task.FromResult(0);
    }
}

public sealed class FakeDialogService : IDialogService
{
    public FakeDialogService()
    {
        Errors = new List<string>();
    }

    public bool ConfirmAnswer { get; set; }
    public string FileToPick { get; set; }
    public List<string> Errors { get; private set; }

    public Task<bool> ConfirmAsync(string title, string message)
    {
        return Task.FromResult(ConfirmAnswer);
    }

    public Task ShowErrorAsync(string title, string message)
    {
        Errors.Add(message);
        return Task.FromResult(0);
    }

    // Other members return Task.FromResult(...) with a settable answer.
}
```

- One fake per interface, in `Fakes/`, shared by all tests.
- To test "still running" states, give the fake a `TaskCompletionSource` the test
  completes when it wants.
- The messenger needs no fake: pass a new `WeakReferenceMessenger()` instance per test
  and register a test recipient on it, or check the ViewModel's reaction to a sent
  message.

## Tests

```csharp
[TestClass]
public class RecipeEditorViewModelTests
{
    private FakeRecipeService _recipeService;
    private FakeDialogService _dialogService;
    private RecipeEditorViewModel _viewModel;

    [TestInitialize]
    public void SetUp()
    {
        _recipeService = new FakeRecipeService();
        _dialogService = new FakeDialogService();
        _viewModel = new RecipeEditorViewModel(_recipeService, _dialogService, new WeakReferenceMessenger());
    }

    [TestMethod]
    public void SaveCommand_IsDisabled_WhenNameIsEmpty()
    {
        _viewModel.Name = "";

        Assert.IsFalse(_viewModel.SaveCommand.CanExecute(null));
    }

    [TestMethod]
    public void Name_RaisesPropertyChanged_AndRefreshesSaveCommand()
    {
        var changed = new List<string>();
        bool canExecuteChanged = false;
        _viewModel.PropertyChanged += (s, e) => changed.Add(e.PropertyName);
        _viewModel.SaveCommand.CanExecuteChanged += (s, e) => canExecuteChanged = true;

        _viewModel.Name = "Bread";

        CollectionAssert.Contains(changed, "Name");
        Assert.IsTrue(canExecuteChanged);
    }

    [TestMethod]
    public async Task SaveCommand_SavesRecipe()
    {
        _viewModel.Name = "Bread";

        await _viewModel.SaveCommand.ExecuteAsync(null);

        Assert.AreEqual(1, _recipeService.Saved.Count);
        Assert.AreEqual("Bread", _recipeService.Saved[0].Name);
    }

    [TestMethod]
    public async Task LoadCommand_ShowsError_WhenServiceFails()
    {
        _recipeService.LoadException = new IOException("disk not ready");

        await _viewModel.LoadCommand.ExecuteAsync(null);

        CollectionAssert.Contains(_dialogService.Errors, "disk not ready");
    }
}
```

These tests compile as C# 5 (`var` and lambdas are older than C# 5), so env C uses them
unchanged; `async Task` test methods are supported by the Visual Studio 2012 test runner.

## What to test

- **Commands**: `CanExecute` in each relevant state; what `Execute` / `ExecuteAsync`
  changes and which service calls it makes; error and cancellation paths.
- **Property notifications**: dependent properties and command refreshes fire when a
  source property changes.
- **Validation**: `HasErrors` and `GetErrors("Name")` for valid and invalid input;
  save blocked while invalid.
- **Navigation and dialogs**: through fakes of `INavigationService` and
  `IDialogService` (for example, `NavigateTo<RecipeEditorViewModel>` was requested
  with the selected recipe; delete does nothing when the confirmation is declined).
- **Messages**: a ViewModel reacts to a message sent on the test's messenger, and
  sends the expected message after an action.

## Rules

- Name tests `Member_ExpectedResult_WhenCondition` (or the project's existing style).
- Arrange, act, assert, separated by a blank line; one behavior per test.
- Tests await `ExecuteAsync(null)`; they never call `Execute` and sleep.
- No test touches the file system, a device, `Application.Current`, or a `Window`. If
  a ViewModel needs one, the ViewModel is missing an interface.
