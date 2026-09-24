namespace Lyt.PhotoPostPro.Workflow.Import;

// Do not add those ImageSharp namespaces to global using as some class definitions conflict
// with the ones from Avalonia. (Point, Rectangle, etc.) 
//using SixLabors.ImageSharp;
//using SixLabors.ImageSharp.PixelFormats;

public sealed partial class ImportViewModel : ViewModel<ImportView>, IDropPathHandler
{
    private const long ImageFileMaxLength = 120L * 1024L * 1024L;

    private readonly PhotoPostProModel model;
    private readonly IToaster toaster;

    [ObservableProperty]
    public partial DropViewModel DropViewModel { get; set; }

    [ObservableProperty]
    public partial FileImportViewModel FileImportViewModel { get; set; }

    [ObservableProperty]
    public partial FolderImportViewModel FolderImportViewModel { get; set; }

    public ImportViewModel(PhotoPostProModel model, IToaster toaster)
    {
        this.model = model;
        this.toaster = toaster;
        this.DropViewModel =
            new DropViewModel(this, "Single.DropZoneHelp")
            {
                IsVisible = true,
                Height = 600,
                Width = 820,
            };
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

    public void SetInitialState()
    {
        this.FileImportViewModel.IsFileMode = false;
        this.FolderImportViewModel.IsFolderMode = false;
        this.DropViewModel.IsVisible = true;
    }

    public void OnDropPath(string path, bool isDirectory)
    {
        if (isDirectory)
        {
            this.FolderImportViewModel.OnFolderDrop(path);
        }
        else
        {
            if (this.ValidatePath(path))
            {
                this.FileImportViewModel.OnSingleFileDrop(path);
            } 
            else
            {  
                // Returns without changing the state of the UI 
                // Inavalid path => We stay in Drop mode
                return; 
            }
        }

        this.DropViewModel.IsVisible = false;
        this.FileImportViewModel.IsFileMode = !isDirectory;
        this.FolderImportViewModel.IsFolderMode = isDirectory;
    }

    private bool ValidatePath(string path)
    {
        void LogAndMessageUser(string logMessage, string userMessage)
        {
            Debug.WriteLine(logMessage);
            this.Logger.Warning(logMessage);
            Dispatch.OnUiThread(() =>
            {
                // Localize and toast userMessage 
                string errorMessage = this.Localize("Toast.Error");
                string displayedMessage = this.Localize(userMessage);
                this.toaster.Show(errorMessage, displayedMessage, 3_500, InformationLevel.Warning);
            }, DispatcherPriority.Background);
        }

        try
        {
            FileInfo fileInfo = new(path);
            if (!fileInfo.Exists)
            {
                LogAndMessageUser($"File does not exist: {path}", "File.NotExist");
                return false;
            }

            long length = fileInfo.Length;
            if ((length == 0) || (length < 256L))
            {
                LogAndMessageUser($"File length is too small: {path}", "File.TooSmall");
                return false;
            }

            if (length > ImageFileMaxLength)
            {
                LogAndMessageUser($"File length is too large: {path}", "File.TooBig");
                return false;
            }

            bool notSupported =
                ImageLoader.HasExcludedExtension(path) || ImageLoader.HasMovieExtension(path);
            if (notSupported)
            {
                LogAndMessageUser($"Image File format not supported: {path}", "File.ImageNotSupported");
                return false;
            }

            if (!FileSystemExtensions.IsReadable(path))
            {
                LogAndMessageUser($"File cannot be read: {path}", "File.CantRead");
                return false;
            }

            return true;
        }
        catch (Exception ex)
        {
            LogAndMessageUser($"Exception thrown while processing: {ex}", "Error while processing image file.");
            return false;
        }
    }
}
