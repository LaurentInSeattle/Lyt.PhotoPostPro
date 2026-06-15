namespace Lyt.PhotoPostPro.Workflow.Process.Export;

public sealed partial class ExportViewModel : StepViewModel<ExportView>
{
    private bool isInitializing; 

    public ExportViewModel() 
    {
        this.isInitializing = true;
        this.isInitializing = false;
    }

    public override void Activate(object? activationParameters)
    {
        base.Activate(activationParameters);
    }
}
