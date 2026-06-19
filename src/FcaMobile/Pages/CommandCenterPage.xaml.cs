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
        await LoadAsync();
    }

    async Task LoadAsync()
    {
        try
        {
            var profile = _store.Load();
            GreetingLabel.Text = profile?.Company is { Length: > 0 } company
                ? $"{company} Command Center"
                : "Your Command Center";

            var leadsTask = _api.GetLeadsAsync();
            var jobsTask = _api.GetJobsAsync();
            var docsTask = _api.GetDocumentsAsync();
            var trainingTask = _api.GetTrainingAsync();
            await Task.WhenAll(leadsTask, jobsTask, docsTask, trainingTask);

            LeadCountLabel.Text = leadsTask.Result.Count.ToString();
            JobCountLabel.Text = jobsTask.Result.Count.ToString();
            DocCountLabel.Text = docsTask.Result.Count.ToString();
            TrainingCountLabel.Text = (trainingTask.Result?.Catalog?.Programs?.Count ?? 0).ToString();
        }
        finally
        {
            RefreshHost.IsRefreshing = false;
        }
    }

    async void OnRefreshing(object sender, EventArgs e) => await LoadAsync();

    async void OnLeadsClicked(object sender, EventArgs e) => await Shell.Current.GoToAsync("//main/leads");
    async void OnPlanRoomClicked(object sender, EventArgs e) => await Shell.Current.GoToAsync("planroom");
    async void OnInvoicesClicked(object sender, EventArgs e) => await Shell.Current.GoToAsync("invoices");
    async void OnCommsClicked(object sender, EventArgs e) => await Shell.Current.GoToAsync("communications");
    async void OnSupportClicked(object sender, EventArgs e) => await Shell.Current.GoToAsync("support");
}
