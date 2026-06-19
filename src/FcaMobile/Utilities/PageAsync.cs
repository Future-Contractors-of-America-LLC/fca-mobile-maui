namespace Fca.Mobile.Utilities;

public static class PageAsync
{
    public static async Task SafeOnAppearingAsync(Page page, Func<Task> action)
    {
        try
        {
            await action().ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            await MainThread.InvokeOnMainThreadAsync(() =>
                page.DisplayAlert("Something went wrong", ex.Message, "OK"));
        }
    }

    public static async Task RunWithLoadingAsync(
        ActivityIndicator indicator,
        Label? errorLabel,
        Func<Task> action)
    {
        errorLabel?.IsVisible = false;
        indicator.IsVisible = true;
        indicator.IsRunning = true;

        try
        {
            await action().ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            if (errorLabel is not null)
            {
                errorLabel.Text = ex.Message;
                errorLabel.IsVisible = true;
            }
        }
        finally
        {
            indicator.IsRunning = false;
            indicator.IsVisible = false;
        }
    }
}
