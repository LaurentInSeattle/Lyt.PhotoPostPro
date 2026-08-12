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
        this.IsExplorerMode = true;
        this.LutExplorerViewModel.Launch(this); 
    }

    public void HideExplorer()
    {
        this.IsExplorerMode = false;
    }
}
