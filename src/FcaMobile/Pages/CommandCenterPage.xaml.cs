using Fca.Mobile.Services;

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

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadAsync(showSpinner: true);
    }

    async Task LoadAsync(bool showSpinner)
    {
        var profile = _store.Load();
        GreetingLabel.Text = profile?.Company is { Length: > 0 } company
            ? $"{company} Command Center"
            : "Your Command Center";

        if (showSpinner)
            Busy.IsVisible = Busy.IsRunning = true;

        try
        {
            var leads = await _api.GetLeadsAsync();
            var jobs = await _api.GetJobsAsync();
            var docs = await _api.GetDocumentsAsync();
            var training = await _api.GetTrainingAsync();

            LeadCountLabel.Text = leads.Count.ToString();
            JobCountLabel.Text = jobs.Count.ToString();
            DocCountLabel.Text = docs.Count.ToString();
            TrainingCountLabel.Text = (training?.Catalog?.Programs?.Count ?? 0).ToString();
        }
        catch (Exception)
        {
            LeadCountLabel.Text = JobCountLabel.Text = DocCountLabel.Text = TrainingCountLabel.Text = "—";
        }
        finally
        {
            Busy.IsVisible = Busy.IsRunning = false;
        }
    }

    async void OnRefreshing(object sender, EventArgs e)
    {
        await LoadAsync(showSpinner: false);
        RefreshHost.IsRefreshing = false;
    }

    async void OnLeadsClicked(object sender, EventArgs e) => await NavigateAsync("//main/leads");
    async void OnPlanRoomClicked(object sender, EventArgs e) => await NavigateAsync("planroom");
    async void OnInvoicesClicked(object sender, EventArgs e) => await NavigateAsync("invoices");
    async void OnCommsClicked(object sender, EventArgs e) => await NavigateAsync("communications");
    async void OnSupportClicked(object sender, EventArgs e) => await NavigateAsync("support");

    static async Task NavigateAsync(string route)
    {
        try
        {
            await Shell.Current.GoToAsync(route);
        }
        catch (Exception)
        {
            // Navigation can fail if a route is already mid-transition; ignore.
        }
    }
}
