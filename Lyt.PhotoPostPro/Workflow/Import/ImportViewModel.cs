namespace Lyt.PhotoPostPro.Workflow.Import;

// Do not add those ImageSharp namespaces to global using as some class definitions conflict
// with the ones from Avalonia. (Point, Rectangle, etc.) 
//using SixLabors.ImageSharp;
//using SixLabors.ImageSharp.PixelFormats;

public sealed partial class ImportViewModel : ViewModel<ImportView>, IDropPathHandler
{
    private readonly PhotoPostProModel model;

    [ObservableProperty]
    public partial DropViewModel DropViewModel { get; set; }

    [ObservableProperty]
    public partial FileImportViewModel FileImportViewModel { get; set; }

    [ObservableProperty]
    public partial FolderImportViewModel FolderImportViewModel { get; set; }

    public ImportViewModel(PhotoPostProModel model, IToaster toaster)
    {
        this.model = model;
        this.DropViewModel = new DropViewModel(this, "Single.DropZoneHelp") { IsVisible = true };
        this.FileImportViewModel = new FileImportViewModel(this.model, toaster);
        this.FolderImportViewModel = new FolderImportViewModel(this.model, toaster);
        this.SetInitialState();
    }

    public override void Activate(object? activationParameters)
    {
        base.Activate(activationParameters);

        // The animation of transitioning views is break that : 
        //this.FileImportViewModel.IsFileMode = false;
        //this.FolderImportViewModel.IsFolderMode = false;
        //this.DropViewModel.IsVisible = true;
        //
        // Therefore we do it on Deactivate so that we are ready for next round 

        // We are potentially about to launch heavy stuff, so clean up while we still can
        // We have about at least one second for Drag and drop to happen 
        this.Dispatcher.OnIdle(() => GC.Collect()); 
    }

    public override void Deactivate()
    { 
        base.Deactivate();
        this.SetInitialState();
    }

    public void SetInitialState ()
    {
        this.FileImportViewModel.IsFileMode = false;
        this.FolderImportViewModel.IsFolderMode = false;
        this.DropViewModel.IsVisible = true;
    }

    public void OnDropPath(string path, bool isDirectory)
    {
        this.DropViewModel.IsVisible = false; 
        this.FileImportViewModel.IsFileMode = !isDirectory;
        this.FolderImportViewModel.IsFolderMode = isDirectory;
        if (isDirectory)
        {
            this.FolderImportViewModel.OnFolderDrop(path);
        }
        else
        {
            this.FileImportViewModel.OnSingleFileDrop(path);
        }
    }
}
