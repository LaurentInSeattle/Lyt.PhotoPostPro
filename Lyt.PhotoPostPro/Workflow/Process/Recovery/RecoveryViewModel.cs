namespace Lyt.PhotoPostPro.Workflow.Process.Recovery;

public sealed partial class RecoveryViewModel : StepViewModel<RecoveryView>
{
    public RecoveryViewModel() { }

    [ObservableProperty]
    public partial bool IsPortrait { get; set; }

    protected override void OnSourceImageReceived(WriteableBitmap bitmap)
    {
        var size = bitmap.PixelSize; 
        this.IsPortrait = size.Height >= size.Width;
        this.SourceImageIsVisible = true;
    } 
}
