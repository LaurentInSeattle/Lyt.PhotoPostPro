namespace Lyt.PhotoPostPro.Workflow.Process.Orient;

public sealed partial class OrientViewModel : StepViewModel<OrientView> 
{
    protected override void OnSourceImageReceived(WriteableBitmap _)
    {
        if (base.ResultImageIsVisible && base.ResultImage is not null)
        {
            base.SourceImageIsVisible = false;
            base.SourceImage = null;
        }
    }

    protected override void OnResultImageReceived (WriteableBitmap _) 
    {
        base.SourceImageIsVisible = false;
        base.SourceImage = null;
    }
}
