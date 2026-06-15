namespace Lyt.PhotoPostPro.Workflow.Process.Exposure;

public sealed partial class ExposureViewModel : StepViewModel<ExposureView>
{
    public ExposureViewModel() { }

    [ObservableProperty]
    public partial bool IsPortrait { get; set; }

    protected override void OnImageReceived(WriteableBitmap bitmap)
    {
        var size = bitmap.PixelSize; 
        this.IsPortrait = size.Height >= size.Width;
        this.SourceImageIsVisible = true;
    } 
}
