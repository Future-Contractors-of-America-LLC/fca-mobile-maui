namespace Fca.Mobile.Pages;

public partial class WelcomePage : ContentPage
{
    public WelcomePage() => InitializeComponent();

    async void OnPlansClicked(object sender, EventArgs e) => await NavigateAsync("plans");

    async void OnSignInClicked(object sender, EventArgs e) => await NavigateAsync("signin");

    async void OnGetStartedClicked(object sender, EventArgs e) => await NavigateAsync("getstarted");

    static async Task NavigateAsync(string route)
    {
        try
        {
            await Shell.Current.GoToAsync(route);
        }
        catch (Exception)
        {
        }
    }
}
