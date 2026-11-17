# Messenger

Use the messenger when two ViewModels must react to each other but neither owns the
other: a list refreshing after an editor saves, a status bar showing a device state.
A parent that holds its child talks to it directly (method call or property), and a
child reports to its parent through an event or a callback passed in its constructor.

## Messages

A message is a small immutable class in `Messages/`, named for what happened
(`RecipeSavedMessage`) or what is asked (`RecipeRequestMessage`).

```csharp
// Env A/B (C# 9+ records are fine in env A; env B uses a class unless LangVersion >= 9)
public sealed class RecipeSavedMessage
{
    public RecipeSavedMessage(Recipe recipe)
    {
        Recipe = recipe;
    }

    public Recipe Recipe { get; }
}
```

In env A/B, `ValueChangedMessage<T>` from the toolkit is also fine for "a value
changed" messages. Env C writes a full property with a private readonly field.

## Registering and sending

Inject `IMessenger`; do not call `WeakReferenceMessenger.Default` inside ViewModels, so
tests can pass their own instance.

The registration form below compiles against both the toolkit (env A/B) and the
template (env C):

```csharp
public RecipeListViewModel(IRecipeService recipeService, IMessenger messenger)
{
    _recipeService = recipeService;
    _messenger = messenger;

    // The handler uses its recipient argument r, not "this", so the messenger's weak
    // reference to this ViewModel is the only reference it keeps.
    _messenger.Register<RecipeSavedMessage>(this, (r, m) => ((RecipeListViewModel)r).OnRecipeSaved(m));
}

private void OnRecipeSaved(RecipeSavedMessage message)
{
    // Update the list.
}
```

```csharp
// Sender
_messenger.Send(new RecipeSavedMessage(recipe));
```

In env A/B the toolkit also offers `IRecipient<TMessage>` plus
`messenger.RegisterAll(this)`, or deriving from `ObservableRecipient` and setting
`IsActive = true`. Use one style per project.

## Rules

- **Unregister when the ViewModel goes away**: in `OnNavigatedFrom`, when a dialog
  closes, or in `Dispose`. Call `_messenger.UnregisterAll(this)`. Weak references stop
  leaks, but an unregistered page that is still alive keeps reacting until the garbage
  collector runs.
- **Handlers run on the sender's thread.** Send from the UI thread (after `await` you
  are on it). If a service raises a message from a background thread, marshal to the
  UI thread in the View layer or in the service, not with `Dispatcher` in the
  ViewModel.
- **One message, one meaning.** Do not add a "Kind" enum to a message; create another
  message type.
- **Do not use the messenger as a request/response bus** between a ViewModel and a
  service. Call the service through its interface.
- **Message names are the contract**; keep them in `Messages/` so the senders and
  receivers of each message are easy to find.
