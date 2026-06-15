namespace Lyt.PhotoPostPro.Workflow.Process.Sharpen;

public sealed partial class SharpenViewModel : StepViewModel<SharpenView>
{
    private bool isInitializing; 

    public SharpenViewModel() 
    {
        this.isInitializing = true;
        this.isInitializing = false;
    }

    public override void Activate(object? activationParameters)
    {
        base.Activate(activationParameters);
    }
}
