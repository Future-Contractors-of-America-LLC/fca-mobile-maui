using Fca.Mobile.Services;

namespace Fca.Mobile;

public partial class App : Application
{
    private readonly CustomerStore _store;

    public App(AppShell shell, CustomerStore store)
    {
        _store = store;
        InitializeComponent();
        MainPage = shell;
    }

    protected override async void OnStart()
    {
        base.OnStart();

        // Route returning users directly to the command center.
        if (_store.IsSignedIn)
            await Shell.Current.GoToAsync("//main/command");
    }
}
