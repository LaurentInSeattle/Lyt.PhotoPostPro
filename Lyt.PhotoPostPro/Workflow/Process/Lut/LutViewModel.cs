namespace Lyt.PhotoPostPro.Workflow.Process.Lut;

public sealed partial class LutViewModel : StepViewModel<LutView> 
{
    [ObservableProperty]
    public partial bool IsExplorerMode {  get ; set; }

    [ObservableProperty]
    public partial LutExplorerViewModel LutExplorerViewModel { get; set; }

    public LutViewModel() => this.LutExplorerViewModel = new();

    public void LaunchExplorer()
    {
        if ( this.model.CurrentWorkflow is null)
        {
            return; 
        }

        if (this.model.CurrentWorkflow.CurrentStep is not LutStep lutStep)
        {
            return;
        }

        this.IsExplorerMode = true;
        this.LutExplorerViewModel.Launch(this, lutStep); 
    }

    public void HideExplorer()
    {
        var toolbox = App.GetRequiredService<LutToolboxViewModel>();
        toolbox.IsExplorerMode = false; 
        this.IsExplorerMode = false;
        this.LutExplorerViewModel.Hide();
    }

    public void NoLut()
    {
        // Call on the model, and NOT the workflow 
        this.model.Reset();
        this.HideExplorer();
    }

    public void UseThisLut (LutMetadata lutMetadata)
    {
        // Call on the model, and NOT the workflow 
        this.model.Reset();
        this.model.Lut(lutMetadata);
        this.HideExplorer();
    }
}
