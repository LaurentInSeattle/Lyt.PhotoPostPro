namespace Lyt.PhotoPostPro.Workflow.Process.Export;

public sealed partial class ExportToolboxViewModel : ToolboxViewModel<ExportToolboxView, ExportStep> 
{
#pragma warning disable CA1822 // Mark members as static
    // RelayCommand's cannot be static 

    [RelayCommand]
    public void OnNext()
    {
        var model = App.GetRequiredService<PhotoPostProModel>();
        model.ClearFirstRun();
    }

#pragma warning restore CA1822

    public override void OnBeforeReset()
    {
        this.model.Export(new ExportStep.ExportParameters());  
    }
}
