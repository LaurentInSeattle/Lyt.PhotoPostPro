namespace Lyt.PhotoPostPro.Workflow.Process.Denoise;

public sealed partial class DenoiseViewModel : StepViewModel<DenoiseView>
{
    private bool isInitializing; 

    public DenoiseViewModel() 
    {
        this.isInitializing = true;
        this.isInitializing = false;
    }

    public override void Activate(object? activationParameters)
    {
        base.Activate(activationParameters);
    }
}
