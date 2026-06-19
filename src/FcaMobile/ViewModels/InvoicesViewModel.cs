using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.Input;
using Fca.Mobile.Models;
using Fca.Mobile.Services;

namespace Fca.Mobile.ViewModels;

public partial class InvoicesViewModel : ViewModelBase
{
    private readonly FcaApiClient _api;

    public InvoicesViewModel(FcaApiClient api, IConnectivityMonitor connectivity)
        : base(connectivity) =>
        _api = api;

    public ObservableCollection<PortalInvoice> Invoices { get; } = new();

    [RelayCommand]
    private Task InitializeAsync() => LoadAsync();

    [RelayCommand]
    private async Task RefreshAsync()
    {
        await LoadAsync().ConfigureAwait(false);
        IsRefreshing = false;
    }

    private Task LoadAsync() => ExecuteAsync(async () =>
    {
        var result = await _api.GetInvoicesAsync().ConfigureAwait(false);
        if (!result.IsSuccess)
        {
            SetError(result.ErrorMessage ?? "Unable to load invoices.");
            return;
        }

        Invoices.Clear();
        foreach (var invoice in result.Value!)
            Invoices.Add(invoice);
    });
}
