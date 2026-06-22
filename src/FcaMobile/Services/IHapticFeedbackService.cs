namespace Fca.Mobile.Services;

public interface IHapticFeedbackService
{
    void Click();

    void Success();
}

public sealed class MauiHapticFeedbackService : IHapticFeedbackService
{
    public void Click() =>
        HapticFeedback.Default.Perform(HapticFeedbackType.Click);

    public void Success() =>
        HapticFeedback.Default.Perform(HapticFeedbackType.LongPress);
}
