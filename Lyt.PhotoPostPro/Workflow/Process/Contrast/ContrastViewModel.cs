namespace Lyt.PhotoPostPro.Workflow.Process.Contrast;

public sealed partial class ContrastViewModel : StepViewModel<ContrastView>
{
    private bool isInitializing; 

    public ContrastViewModel() 
    {
        this.isInitializing = true;
        this.isInitializing = false;
    }

    public override void Activate(object? activationParameters)
    {
        base.Activate(activationParameters);
    }
}
