namespace Lyt.PhotoPostPro.Workflow.Folder;

public sealed partial class FolderViewModel : ViewModel<FolderView>, IDropPathHandler
{
    private readonly PhotoPostProModel photoPostProModel;

    public FolderViewModel(PhotoPostProModel photoPostProModel) 
    {
        this.photoPostProModel = photoPostProModel;
    }

    public override void Activate(object? activationParameters)
    {
        base.Activate(activationParameters);
    }

    public void OnDropPath(string path, bool isDirectory)
    {
    }
}
