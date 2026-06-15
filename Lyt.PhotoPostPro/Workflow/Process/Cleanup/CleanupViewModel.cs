namespace Lyt.PhotoPostPro.Workflow.Process.Cleanup;

public sealed partial class CleanupViewModel : StepViewModel<CleanupView>
{
    private bool isInitializing; 

    public CleanupViewModel() 
    {
        this.isInitializing = true;
        this.isInitializing = false;
    }

    public override void Activate(object? activationParameters)
    {
        base.Activate(activationParameters);
    }
}
