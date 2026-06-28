using Android.App;
using Android.Runtime;

namespace Fca.Mobile;

[Application]
public class MainApplication : MauiApplication
{
    public MainApplication(IntPtr handle, JniHandleOwnership ownership)
        : base(handle, ownership)
    {
        AndroidEnvironment.UnhandledExceptionRaiser += (_, args) =>
        {
            System.Diagnostics.Debug.WriteLine($"Android unhandled: {args.Exception}");
        };
    }

    protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();
}
