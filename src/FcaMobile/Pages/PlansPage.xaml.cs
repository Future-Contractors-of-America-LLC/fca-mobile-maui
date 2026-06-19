namespace Fca.Mobile.Pages;

public partial class PlansPage : ContentPage
{
    public PlansPage() => InitializeComponent();

    async void OnGetStartedClicked(object sender, EventArgs e)
    {
        try
        {
            await Shell.Current.GoToAsync("getstarted");
        }
        catch (Exception)
        {
        }
    }
}
