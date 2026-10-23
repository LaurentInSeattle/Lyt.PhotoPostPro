namespace Lyt.PhotoPostPro.Workflow.Process.Spotlight;

public sealed partial class SpotlightViewModel : StepViewModel<SpotlightView> 
{
    [ObservableProperty]
    public partial double ImageWidth { get; set; }

    [ObservableProperty]
    public partial double ImageHeight { get; set; }

    public override void OnViewLoaded()
    {
        base.OnViewLoaded();
    }

    public override void Activate(object? activationParameters)
    {
        base.Activate(activationParameters);
    }

    public override void Deactivate()
    {
        base.Deactivate();
    }

    protected override void OnSourceImageReceived(WriteableBitmap bitmap)
    {
        var imageSize = bitmap.Size;
        this.ImageWidth = imageSize.Width;
        this.ImageHeight = imageSize.Height;
    }
}