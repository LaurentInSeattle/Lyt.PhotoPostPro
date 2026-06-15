namespace Lyt.PhotoPostPro.Workflow.Process.Color;

public sealed partial class ColorViewModel : StepViewModel<ColorView>
{
    private bool isInitializing; 

    public ColorViewModel() 
    {
        this.isInitializing = true;
        this.isInitializing = false;
    }

    public override void Activate(object? activationParameters)
    {
        base.Activate(activationParameters);
    }
}
