using Fca.Mobile.Models;
using Fca.Mobile.Services;

namespace Fca.Mobile.Pages;

public partial class LeadPipelinePage : ContentPage
{
    private readonly FcaApiClient _api;

    public LeadPipelinePage(FcaApiClient api)
    {
        _api = api;
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
            LeadList.ItemsSource = await _api.GetLeadsAsync();
        }
        catch
        {
            await this.ShowLoadErrorAsync("leads");
        }
    }

    async void OnRefreshing(object sender, EventArgs e)
    {
        try
        {
            await LoadAsync();
        }
        finally
        {
            RefreshHost.IsRefreshing = false;
        }
    }

    async void OnQualifyClicked(object sender, EventArgs e)
    {
        if (sender is not BindableObject bindable || bindable.BindingContext is not BidRecord bid || string.IsNullOrWhiteSpace(bid.Id))
            return;

        try
        {
            await _api.QualifyLeadAsync(bid.Id);
            await LoadAsync();
            await DisplayAlert("Qualified", "Lead qualification recorded on Auricrux-Central.", "OK");
        }
        catch
        {
            await DisplayAlert("Unable to qualify", "Check your session and retry from the FCA portal if needed.", "OK");
        }
    }
}
