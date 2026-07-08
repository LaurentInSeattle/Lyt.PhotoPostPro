namespace Lyt.PhotoPostPro.Workflow.Shared;

public sealed partial class DropViewModel : ViewModel<DropView>
{
    private readonly IDropPathHandler dropPathHandler; 

    public DropViewModel(IDropPathHandler dropPathHandler) => this.dropPathHandler = dropPathHandler;

    [ObservableProperty]
    public partial bool IsVisible { get; set; }

    internal bool OnDrop(string path, bool isDirectory)
    {
        try
        {
            this.dropPathHandler.OnDropPath(path, isDirectory); 
            return true;
        }
        catch (Exception ex)
        {
            this.Logger.Warning(ex.ToString());
            return false;
        }
    }
}
