using Fca.Mobile.Services;
using Microsoft.Extensions.Logging;

namespace Fca.Mobile.Pages;

public partial class CommandCenterPage : ContentPage
{
    private readonly FcaApiClient _api;
    private readonly CustomerStore _store;
    private readonly ILogger<CommandCenterPage> _logger;

    public CommandCenterPage(FcaApiClient api, CustomerStore store, ILogger<CommandCenterPage> logger)
    {
        _api = api;
        _store = store;
        _logger = logger;
        InitializeComponent();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadAsync();
    }

    async Task LoadAsync()
    {
        SetLoading(true);
        try
        {
            var profile = _store.Load();
            GreetingLabel.Text = profile?.Company is { Length: > 0 } company
                ? $"{company} Command Center"
                : "Your Command Center";

            var leads = await _api.GetLeadsAsync();
            var jobs = await _api.GetJobsAsync();
            var docs = await _api.GetDocumentsAsync();
            var training = await _api.GetTrainingAsync();

            LeadCountLabel.Text = leads.Count.ToString();
            JobCountLabel.Text = jobs.Count.ToString();
            DocCountLabel.Text = docs.Count.ToString();
            TrainingCountLabel.Text = (training?.Catalog?.Programs?.Count ?? 0).ToString();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "CommandCenter LoadAsync failed");
            await DisplayAlert("Connection error", "Unable to refresh your dashboard. Pull down to retry.", "OK");
        }
        finally
        {
            SetLoading(false);
        }
    }

    async void OnRefreshing(object sender, EventArgs e)
    {
        await LoadAsync();
        RefreshHost.IsRefreshing = false;
    }

    void SetLoading(bool loading)
    {
        LoadingIndicator.IsRunning = loading;
        LoadingIndicator.IsVisible = loading;
    }

    async void OnLeadsClicked(object sender, EventArgs e) => await Shell.Current.GoToAsync("//main/leads");
    async void OnPlanRoomClicked(object sender, EventArgs e) => await Shell.Current.GoToAsync("planroom");
    async void OnInvoicesClicked(object sender, EventArgs e) => await Shell.Current.GoToAsync("invoices");
    async void OnCommsClicked(object sender, EventArgs e) => await Shell.Current.GoToAsync("communications");
    async void OnSupportClicked(object sender, EventArgs e) => await Shell.Current.GoToAsync("support");
}
