using Fca.Mobile.Services;
using Fca.Mobile.Utilities;

namespace Fca.Mobile.Pages;

public partial class CommandCenterPage : ContentPage
{
    private readonly FcaApiClient _api;
    private readonly CustomerStore _store;

    public CommandCenterPage(FcaApiClient api, CustomerStore store)
    {
        _api = api;
        _store = store;
        InitializeComponent();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _ = PageAsync.SafeOnAppearingAsync(this, LoadAsync);
    }

    async Task LoadAsync()
    {
        await PageAsync.RunWithLoadingAsync(LoadingIndicator, ErrorLabel, async () =>
        {
            var profile = _store.Load();
            GreetingLabel.Text = profile?.Company is { Length: > 0 } company
                ? $"{company} Command Center"
                : "Your Command Center";

            var leads = await _api.GetLeadsAsync().ConfigureAwait(false);
            var jobs = await _api.GetJobsAsync().ConfigureAwait(false);
            var docs = await _api.GetDocumentsAsync().ConfigureAwait(false);
            var training = await _api.GetTrainingAsync().ConfigureAwait(false);

            if (!leads.IsSuccess || !jobs.IsSuccess || !docs.IsSuccess || !training.IsSuccess)
            {
                ErrorLabel.Text = leads.ErrorMessage
                    ?? jobs.ErrorMessage
                    ?? docs.ErrorMessage
                    ?? training.ErrorMessage
                    ?? "Unable to load your command center.";
                ErrorLabel.IsVisible = true;
                return;
            }

            LeadCountLabel.Text = leads.Value!.Count.ToString();
            JobCountLabel.Text = jobs.Value!.Count.ToString();
            DocCountLabel.Text = docs.Value!.Count.ToString();
            TrainingCountLabel.Text = (training.Value?.Catalog?.Programs?.Count ?? 0).ToString();
        });
    }

    async void OnRefreshing(object sender, EventArgs e)
    {
        await LoadAsync().ConfigureAwait(false);
        RefreshHost.IsRefreshing = false;
    }

    async void OnLeadsClicked(object sender, EventArgs e) => await Shell.Current.GoToAsync("//main/leads");
    async void OnPlanRoomClicked(object sender, EventArgs e) => await Shell.Current.GoToAsync("planroom");
    async void OnInvoicesClicked(object sender, EventArgs e) => await Shell.Current.GoToAsync("invoices");
    async void OnCommsClicked(object sender, EventArgs e) => await Shell.Current.GoToAsync("communications");
    async void OnSupportClicked(object sender, EventArgs e) => await Shell.Current.GoToAsync("support");
}
