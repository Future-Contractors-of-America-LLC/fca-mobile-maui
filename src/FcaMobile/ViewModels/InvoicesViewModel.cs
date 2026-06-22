using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
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

    [ObservableProperty]
    private string billingSummaryLabel = string.Empty;

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
        var invoices = await _api.GetInvoicesAsync().ConfigureAwait(false);
        var billing = await _api.GetBillingSummaryAsync().ConfigureAwait(false);

        if (!invoices.IsSuccess)
        {
            SetError(invoices.ErrorMessage ?? "Unable to load invoices.");
            return;
        }

        Invoices.Clear();
        foreach (var invoice in invoices.Value!)
            Invoices.Add(invoice);

        if (billing.IsSuccess && billing.Value?.Count > 0)
        {
            BillingSummaryLabel =
                $"{billing.Value.Count} construction billing record{(billing.Value.Count == 1 ? "" : "s")} on file";
        }
        else
        {
            BillingSummaryLabel = string.Empty;
        }
    });
}
