namespace Fca.Mobile.Services;

public sealed class MauiNetworkStatus : INetworkStatus
{
    public bool IsOffline() => Connectivity.Current.NetworkAccess == NetworkAccess.None;
}
