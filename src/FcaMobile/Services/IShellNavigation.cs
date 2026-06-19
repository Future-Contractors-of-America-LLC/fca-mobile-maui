namespace Fca.Mobile.Services;

public interface IShellNavigation
{
    Task GoToAsync(string route);

    Task GoToMainAsync(string tabRoute = "//main/command");

    Task GoToWelcomeAsync();
}

public sealed class ShellNavigationService : IShellNavigation
{
    public Task GoToAsync(string route) => Shell.Current.GoToAsync(route);

    public Task GoToMainAsync(string tabRoute = "//main/command") => Shell.Current.GoToAsync(tabRoute);

    public Task GoToWelcomeAsync() => Shell.Current.GoToAsync("//welcome");
}
