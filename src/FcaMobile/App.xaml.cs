namespace Fca.Mobile;

public partial class App : Application
{
    public App(AppShell shell)
    {
        InitializeComponent();
        MainPage = shell;
        shell.RestoreSession();
    }
}
