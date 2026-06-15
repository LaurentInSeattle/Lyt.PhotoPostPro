namespace Lyt.PhotoPostPro.Workflow.Process.TouchUp;

public sealed partial class TouchUpViewModel : StepViewModel<TouchUpView>
{
    private bool isInitializing; 

    public TouchUpViewModel() 
    {
        this.isInitializing = true;
        this.isInitializing = false;
    }

    public override void Activate(object? activationParameters)
    {
        base.Activate(activationParameters);
    }
}
