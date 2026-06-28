using Fca.Mobile.Models;

namespace Fca.Mobile.Pages;

public partial class PlansPage : ContentPage
{
    public PlansPage()
    {
        InitializeComponent();
        PlanCards.ItemsSource = PricingCatalog.Tiers;
    }

    async void OnGetStartedClicked(object sender, EventArgs e) =>
        await Shell.Current.GoToAsync("getstarted");
}
