namespace Lyt.PhotoPostPro.Workflow.Process.WhiteBalance;

public sealed partial class WhiteBalanceViewModel : StepViewModel<WhiteBalanceView>
{
    public WhiteBalanceViewModel() { }

    [ObservableProperty]
    public partial bool IsPortrait { get; set; }

    protected override void OnSourceImageReceived(WriteableBitmap bitmap)
    {
        var size = bitmap.PixelSize; 
        this.IsPortrait = size.Height >= size.Width;
        this.SourceImageIsVisible = true;
    } 
}
