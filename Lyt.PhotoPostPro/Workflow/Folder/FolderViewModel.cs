namespace Lyt.PhotoPostPro.Workflow.Folder;

public sealed partial class FolderViewModel : ViewModel<FolderView>
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

    internal void OnDropPath(string path, bool isDirectory)
    {
    }
}
