namespace Lyt.PhotoPostPro.Panes.CurvePane;

public sealed partial class GammaCurveViewModel : 
    ViewModel<GammaCurveView>, 
    IRecipient<GammaLutGeneratedMessage>
{
    public GammaCurveViewModel() => this.Subscribe<GammaLutGeneratedMessage>();

    public void Receive(GammaLutGeneratedMessage message)
    {
        Dispatch.OnUiThread(() =>
        {
            this.View.GammaCurveImageControl.Load(message.Curve);
        }, DispatcherPriority.ApplicationIdle);
    }
}
