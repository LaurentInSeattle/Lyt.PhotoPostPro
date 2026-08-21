namespace Lyt.PhotoPostPro.Panes.HistogramPane;

using static Controls.HistogramImageControl;

public sealed partial class HistogramViewModel : 
    ViewModel<HistogramView>, 
    IRecipient<HistogramsGeneratedMessage>
{
    [ObservableProperty]
    public partial bool HistogramIsVisible { get; set; }

    public HistogramViewModel()
    {
        this.HistogramIsVisible = true; 
        this.Subscribe<HistogramsGeneratedMessage>();
    } 

    public void Receive(HistogramsGeneratedMessage message)
    {
        Dispatch.OnUiThread(() =>
        {
            var histograms = message.Histograms;
            this.View.HistogramImageControlRed.Load(histograms.Red, BrushColor.Red, this);
            this.View.HistogramImageControlGreen.Load(histograms.Green, BrushColor.Green, this);
            this.View.HistogramImageControlBlue.Load(histograms.Blue, BrushColor.Blue, this);
            this.View.HistogramImageControlLuminosity.Load(histograms.Luminosity, BrushColor.Luminosity, this);
        }, DispatcherPriority.ApplicationIdle);
    }

    internal void OnClick() => this.HistogramIsVisible = ! this.HistogramIsVisible; 
}
