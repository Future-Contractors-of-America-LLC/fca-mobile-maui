namespace Fca.Mobile.Pages;

public partial class WelcomePage : ContentPage
{
    public WelcomePage() => InitializeComponent();

    async void OnPlansClicked(object sender, EventArgs e) =>
        await Shell.Current.GoToAsync("plans");

    async void OnSignInClicked(object sender, EventArgs e) =>
        await Shell.Current.GoToAsync("signin");

    async void OnGetStartedClicked(object sender, EventArgs e) =>
        await Shell.Current.GoToAsync("getstarted");
}
