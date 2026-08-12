namespace Lyt.PhotoPostPro.Workflow.Process.Lut;

public sealed partial class LutViewModel : StepViewModel<LutView> 
{
    [ObservableProperty]
    public partial bool IsExplorerMode {  get ; set; }

    [ObservableProperty]
    public partial LutExplorerViewModel LutExplorerViewModel { get; set; }

    public LutViewModel()
    {
        this.LutExplorerViewModel = new(); 
    }

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
        this.IsExplorerMode = false;
        this.LutExplorerViewModel.Hide();
    }
}
