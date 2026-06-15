namespace Lyt.PhotoPostPro.Panes.HistogramPane;

using static Controls.HistogramImageControl;

public sealed partial class HistogramViewModel : 
    ViewModel<HistogramView>, 
    IRecipient<HistogramsGeneratedMessage>
{
    public HistogramViewModel() => this.Subscribe<HistogramsGeneratedMessage>();

    public void Receive(HistogramsGeneratedMessage message)
    {
        Dispatch.OnUiThread(() =>
        {
            var histograms = message.Histograms;
            this.View.HistogramImageControlRed.Load(histograms.Red, BrushColor.Red);
            this.View.HistogramImageControlGreen.Load(histograms.Green, BrushColor.Green);
            this.View.HistogramImageControlBlue.Load(histograms.Blue, BrushColor.Blue);
            this.View.HistogramImageControlLuminosity.Load(histograms.Luminosity, BrushColor.Luminosity);
        }, DispatcherPriority.ApplicationIdle);
    }
}
