using CommunityToolkit.Mvvm.ComponentModel;
using Fca.Mobile.Services;

namespace Fca.Mobile.ViewModels;

public abstract partial class ViewModelBase : ObservableObject
{
    private readonly IConnectivityMonitor _connectivity;

    protected ViewModelBase(IConnectivityMonitor connectivity)
    {
        _connectivity = connectivity;
        _connectivity.Changed += OnConnectivityChanged;
        IsOffline = _connectivity.IsOffline;
    }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasError))]
    private string? errorMessage;

    [ObservableProperty]
    private bool isBusy;

    [ObservableProperty]
    private bool isRefreshing;

    [ObservableProperty]
    private bool isOffline;

    public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);

    protected void SetError(string? message) => ErrorMessage = message;

    protected void ClearError() => ErrorMessage = null;

    protected async Task ExecuteAsync(Func<Task> action, bool clearError = true)
    {
        if (IsBusy)
            return;

        IsBusy = true;
        if (clearError)
            ClearError();

        try
        {
            await action().ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            SetError(ex.Message);
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void OnConnectivityChanged(object? sender, EventArgs e) =>
        IsOffline = _connectivity.IsOffline;
}
