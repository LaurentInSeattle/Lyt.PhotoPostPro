namespace Lyt.PhotoPostPro.Workflow.Process.Export;

public sealed partial class ExportToolboxViewModel : ToolboxViewModel<ExportToolboxView, ExportStep> 
{
    protected override string Title => this.Localize("Workflow.Contrast.Title");

    public override void OnBeforeReset()
    {
        this.model.Export(new ExportStep.ExportParameters());  
    }
}
