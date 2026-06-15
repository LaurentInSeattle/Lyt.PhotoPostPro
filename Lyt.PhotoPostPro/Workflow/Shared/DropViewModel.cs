namespace Lyt.PhotoPostPro.Workflow.Shared;

public sealed partial class DropViewModel : ViewModel<DropView>
{
    /// <summary> Returns true if the path is a valid directory path ora valid file. </summary>
    internal bool OnDrop(string path, bool isDirectory)
    {
        try
        {
            var folderViewModel = App.GetRequiredService<FolderViewModel>();            
            if (folderViewModel.IsActivated)
            {
                folderViewModel.OnDropPath(path, isDirectory);
                return true;
            }
 
            var singleViewModel = App.GetRequiredService<SingleViewModel>();
            if (singleViewModel.IsActivated)
            {
                singleViewModel.OnDropPath(path, isDirectory);
                return true;
            }

            return false;
        }
        catch (Exception ex)
        {
            this.Logger.Warning(ex.ToString());
            return false;
        }
    }
}
