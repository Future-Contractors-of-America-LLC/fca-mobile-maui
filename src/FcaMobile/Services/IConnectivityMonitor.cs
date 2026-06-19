namespace Fca.Mobile.Services;

public interface IConnectivityMonitor
{
    bool IsOffline { get; }

    event EventHandler? Changed;
}

public sealed class MauiConnectivityMonitor : IConnectivityMonitor
{
    public MauiConnectivityMonitor()
    {
        Connectivity.ConnectivityChanged += (_, _) => Changed?.Invoke(this, EventArgs.Empty);
    }

    public bool IsOffline => Connectivity.Current.NetworkAccess == NetworkAccess.None;

    public event EventHandler? Changed;
}
