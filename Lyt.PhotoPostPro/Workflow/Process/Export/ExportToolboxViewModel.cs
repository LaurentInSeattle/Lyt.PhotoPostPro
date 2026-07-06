namespace Lyt.PhotoPostPro.Workflow.Process.Export;

public sealed partial class ExportToolboxViewModel : ToolboxViewModel<ExportToolboxView, ExportStep> 
{
    protected override string Title => this.Localize("Workflow.Export.Title");

    public override void OnBeforeReset()
    {
    }

    [RelayCommand]
    public void OnExport()
    {
        // TODO: Collect parameters 
        this.model.Export(new ExportParameters());
    }

    [RelayCommand]
    public void OnFinish()
    {
        // TODO: Warn if nothing exported 
        new ToolbarCommandMessage(ToolbarCommandMessage.ToolbarCommand.BackToWindowed).Publish();
        this.model.Finish();
    }
}
