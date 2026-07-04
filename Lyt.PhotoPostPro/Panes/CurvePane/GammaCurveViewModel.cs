namespace Lyt.PhotoPostPro.Panes.CurvePane;

public sealed partial class GammaCurveViewModel : 
    ViewModel<GammaCurveView>,
    IRecipient<GammaLutGeneratedMessage>,
    IRecipient<GammaLutClearMessage>
{
    public GammaCurveViewModel()
    {
        this.Subscribe<GammaLutGeneratedMessage>();
        this.Subscribe<GammaLutClearMessage>();
    } 

    public void Receive(GammaLutGeneratedMessage message)
    {
        Dispatch.OnUiThread(() =>
        {
            this.View.GammaCurveImageControl.Load(message.Curve);
        }, DispatcherPriority.ApplicationIdle);
    }

    public void Receive(GammaLutClearMessage message)
    {
        Dispatch.OnUiThread(() =>
        {
            this.View.GammaCurveImageControl.Clear();
        }, DispatcherPriority.ApplicationIdle);
    }
}
