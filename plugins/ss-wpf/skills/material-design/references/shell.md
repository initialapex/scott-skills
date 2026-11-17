# Shell window

The shell is the main window: app bar, navigation, the page host bound to
`CurrentViewModel` (ViewModel-first navigation from `ss-wpf:mvvm`), the dialog overlay,
and the Snackbar. Two navigation layouts:

| Layout | Use when |
|--------|----------|
| **Drawer** (hamburger menu, `DrawerHost`) | Many destinations, or the work area needs the full width; pages are switched now and then |
| **Navigation rail** (`MaterialDesignNavigationRailTabControl`) | Three to seven top-level destinations the user switches between all the time |

## Layers, outside in

1. `Window` with `MaterialDesignWindow` (or the project's `AppWindowStyle`).
2. `DialogHost Identifier="RootDialog"`, so dialogs cover everything
   ([dialogs-snackbar.md](dialogs-snackbar.md)).
3. `DrawerHost` (drawer layout only).
4. App bar (`ColorZone`) on top, page host below, `Snackbar` at the bottom of the page
   area.

## Shell ViewModel

The navigation list is data in the ViewModel; selecting an item navigates. Icons are
`PackIcon` kind names as strings, so the ViewModel does not reference the toolkit.

```csharp
public sealed class NavigationItem
{
    private readonly Action _navigate;

    public NavigationItem(string title, string icon, Action navigate)
    {
        Title = title;
        Icon = icon;
        _navigate = navigate;
    }

    public string Title { get; }
    public string Icon { get; }   // PackIcon kind name, e.g. "FormatListBulleted"

    public void Navigate()
    {
        _navigate();
    }
}

public partial class MainViewModel : ObservableObject
{
    public MainViewModel(INavigationService navigationService)
    {
        NavigationItems = new[]
        {
            new NavigationItem("Recipes", "FormatListBulleted", () => navigationService.NavigateTo<RecipeListViewModel>()),
            new NavigationItem("Reports", "ChartLine", () => navigationService.NavigateTo<ReportViewModel>()),
            new NavigationItem("Settings", "Cog", () => navigationService.NavigateTo<SettingsViewModel>())
        };
    }

    public IReadOnlyList<NavigationItem> NavigationItems { get; }

    [ObservableProperty]
    private object _currentViewModel;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Title))]
    private NavigationItem _selectedNavigationItem;

    [ObservableProperty]
    private bool _isMenuOpen;

    public string Title => SelectedNavigationItem?.Title ?? "My App";

    // Called once from the composition root after the shell is attached to the
    // navigation service; not from the constructor (the service may not be ready).
    public void Start()
    {
        SelectedNavigationItem = NavigationItems[0];
    }

    partial void OnSelectedNavigationItemChanged(NavigationItem value)
    {
        if (value == null)
        {
            return;
        }

        value.Navigate();
        IsMenuOpen = false;
    }
}
```

- Pages opened from inside another page (`NavigateTo<RecipeEditorViewModel>` from the
  list) keep the parent's item selected, which is what the user expects.
- Tests check that selecting an item requests the right ViewModel from a
  `FakeNavigationService` and closes the menu.
- In env B without generators, write the properties with `SetProperty` and navigate in
  the `SelectedNavigationItem` setter.

## Drawer layout

```xml
<Window x:Class="MyApp.Views.MainWindow"
        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        xmlns:materialDesign="http://materialdesigninxaml.net/winfx/xaml/themes"
        Style="{StaticResource MaterialDesignWindow}"
        Title="{Binding Title}" Height="720" Width="1200">

    <materialDesign:DialogHost Identifier="RootDialog" DialogTheme="Inherit">
        <materialDesign:DrawerHost IsLeftDrawerOpen="{Binding IsMenuOpen, Mode=TwoWay}">

            <materialDesign:DrawerHost.LeftDrawerContent>
                <DockPanel MinWidth="240">
                    <TextBlock DockPanel.Dock="Top" Margin="16"
                               Style="{StaticResource MaterialDesignHeadline6TextBlock}"
                               Text="My App" />
                    <ListBox ItemsSource="{Binding NavigationItems}"
                             SelectedItem="{Binding SelectedNavigationItem}"
                             Style="{StaticResource MaterialDesignNavigationPrimaryListBox}">
                        <ListBox.ItemTemplate>
                            <DataTemplate>
                                <StackPanel Orientation="Horizontal" Margin="8,4">
                                    <materialDesign:PackIcon Kind="{Binding Icon}" Width="24" Height="24"
                                                             Margin="0,0,16,0" VerticalAlignment="Center" />
                                    <TextBlock Text="{Binding Title}" VerticalAlignment="Center" />
                                </StackPanel>
                            </DataTemplate>
                        </ListBox.ItemTemplate>
                    </ListBox>
                </DockPanel>
            </materialDesign:DrawerHost.LeftDrawerContent>

            <DockPanel>
                <materialDesign:ColorZone DockPanel.Dock="Top" Mode="PrimaryMid" Padding="8,8,16,8"
                                          materialDesign:ElevationAssist.Elevation="Dp4">
                    <DockPanel>
                        <ToggleButton DockPanel.Dock="Left"
                                      Style="{StaticResource MaterialDesignHamburgerToggleButton}"
                                      IsChecked="{Binding IsMenuOpen, Mode=TwoWay}" />
                        <!-- Global actions, right-aligned -->
                        <StackPanel DockPanel.Dock="Right" Orientation="Horizontal" />
                        <TextBlock Margin="16,0" VerticalAlignment="Center"
                                   Style="{StaticResource MaterialDesignHeadline6TextBlock}"
                                   Text="{Binding Title}" />
                    </DockPanel>
                </materialDesign:ColorZone>

                <Grid>
                    <!-- Pages: a DataTemplate per page ViewModel supplies the View -->
                    <ContentControl Content="{Binding CurrentViewModel}" Focusable="False" />

                    <materialDesign:Snackbar x:Name="MainSnackbar"
                                             HorizontalAlignment="Stretch"
                                             VerticalAlignment="Bottom" />
                </Grid>
            </DockPanel>
        </materialDesign:DrawerHost>
    </materialDesign:DialogHost>
</Window>
```

- Pages set their own padding (16 or 24, see [controls.md](controls.md#spacing)) and
  their own scrolling (`ScrollViewer` inside the page where needed), so a page with a
  `DataGrid` can fill the height.
- Global actions in the app bar are `MaterialDesignToolForegroundButton` or icon buttons
  with tooltips; page-specific actions stay on the page.
- `ColorZone Mode="PrimaryMid"` gives the bar the primary color with a matching
  foreground. `Standard` gives a surface-colored bar for quieter applications.

## Navigation rail layout

The rail is a `TabControl` whose items are the navigation items; its content area shows
the shell's current page.

```xml
<materialDesign:DialogHost Identifier="RootDialog" DialogTheme="Inherit">
    <Grid>
        <TabControl Style="{StaticResource MaterialDesignNavigationRailTabControl}"
                    TabStripPlacement="Left"
                    ItemsSource="{Binding NavigationItems}"
                    SelectedItem="{Binding SelectedNavigationItem}">
            <TabControl.ItemTemplate>
                <DataTemplate>
                    <StackPanel>
                        <materialDesign:PackIcon Kind="{Binding Icon}" Width="24" Height="24"
                                                 HorizontalAlignment="Center" />
                        <TextBlock Text="{Binding Title}" HorizontalAlignment="Center" />
                    </StackPanel>
                </DataTemplate>
            </TabControl.ItemTemplate>
            <TabControl.ContentTemplate>
                <DataTemplate>
                    <!-- Show the shell's current page, not the navigation item -->
                    <ContentControl Focusable="False"
                                    Content="{Binding DataContext.CurrentViewModel,
                                              RelativeSource={RelativeSource AncestorType=TabControl}}" />
                </DataTemplate>
            </TabControl.ContentTemplate>
        </TabControl>

        <materialDesign:Snackbar x:Name="MainSnackbar"
                                 HorizontalAlignment="Stretch"
                                 VerticalAlignment="Bottom" />
    </Grid>
</materialDesign:DialogHost>
```

- The same `MainViewModel` serves both layouts (`IsMenuOpen` is simply unused here).
- A page-creating action can sit at the top of the rail with
  `materialDesign:NavigationRailAssist.FloatingContent` (a
  `MaterialDesignFloatingActionSecondaryButton` bound to a command).
- Material Design 3 projects use the MD3 navigation styles instead (see
  [setup.md](setup.md#material-design-2-or-3)).

## Startup

The composition root shows the window, then starts navigation:

```csharp
// Env A: after resolving MainWindow from the host
mainWindow.Show();
mainViewModel.Start();

// Env B: after navigationService.AttachShell(mainViewModel)
new MainWindow(mainViewModel, snackbarQueue).Show();
mainViewModel.Start();
```
