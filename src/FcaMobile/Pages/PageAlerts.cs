namespace Fca.Mobile.Pages;

internal static class PageAlerts
{
    public static Task ShowLoadErrorAsync(this Page page, string contentName)
        => page.DisplayAlert("Could not load data", $"We could not load {contentName}. Pull to refresh or try again shortly.", "OK");

    public static Task ShowSaveErrorAsync(this Page page, string action)
        => page.DisplayAlert("Request failed", $"We could not {action}. Check your connection and try again.", "OK");
}
