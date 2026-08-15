namespace Lyt.PhotoPostPro.Workflow.Import;

// Do not add those ImageSharp namespaces to global using as some class definitions conflict
// with the ones from Avalonia. (Point, Rectangle, etc.) 
//using SixLabors.ImageSharp;
//using SixLabors.ImageSharp.PixelFormats;

public sealed partial class ImportViewModel : ViewModel<ImportView>, IDropPathHandler
{
    private readonly PhotoPostProModel model;
    private readonly IToaster toaster;

    private LoadedImage? loadedImage;

    [ObservableProperty]
    public partial WriteableBitmap? SourceImage { get; set; }

    [ObservableProperty]
    public partial DropViewModel DropViewModel { get; set; }

    [ObservableProperty]
    public partial SpinViewModel SpinViewModel { get; set; }

    [ObservableProperty]
    public partial bool ProcessIsDisabled { get; set; } = true;

    [ObservableProperty]
    public partial MetadataViewModel MetadataViewModel { get; set; }

    public ImportViewModel(PhotoPostProModel model, IToaster toaster)
    {
        this.model = model;
        this.toaster = toaster;
        this.MetadataViewModel = new();
        this.SpinViewModel = new SpinViewModel()
        {
            IsVisible = false,
            IsActive = false,
        };

        this.DropViewModel = new DropViewModel(this, "Single.DropZoneHelp") { IsVisible = true };
    }

#pragma warning disable CA1822 // Mark members as static
    // RelayCommand's cannot be static 

    [RelayCommand]
    public void OnProcess()
    {
        var mainWindow = App.MainWindow;
        if (mainWindow.CanMaximize)
        {
            mainWindow.WindowState = WindowState.Maximized;
        }

        var viewModel = App.GetRequiredService<ImportViewModel>();
        viewModel.ProcessCurrentImage();
    }

#pragma warning restore CA1822
    public void OnDropPath(string path, bool isDirectory)
    {
        if (isDirectory)
        {
            this.Logger.Warning("Dropped path is a directory, expected a file.");
            return;
        }
        else
        {
            // Always launch a spinner for big or small files 
            SpinWait(start: true);
            Task.Run(() => { this.TryLoadImage(path); });
        }
    }

    private void TryLoadImage(string path)
    {
        string error = string.Empty;
        try
        {
            this.loadedImage = ImageLoader.LoadImage(path);
            loadedImage.CreateThumbnail();
            if (loadedImage.IsSuccess && loadedImage.IsFullyLoadedWithThumbnail)
            {
                // ! Verified by loadedImage.IsFullyLoaded
                var imageFrame = ImagingUtilities.ToFrame(this.loadedImage.Image!);
                if (imageFrame is not null)
                {
                    Dispatch.OnUiThread(() =>
                    {
                        this.OnImageLoaded(imageFrame);
                        // ! Verified by loadedImage.IsFullyLoaded
                        new MetadataGeneratedMessage(this.loadedImage.Metadata!).Publish();
                    });
                }
                else
                {

                    error = "Failed to load image frame.";
                }
            }
            else
            {
                error = "Failed to load image file: " + loadedImage.ErrorMessage;
            }
        }
        catch (Exception ex)
        {
            error = "Error loading image file, Exception thrown: " + ex.Message;
        }
        finally
        {
            // Error or not: stop the spinner 
            Dispatch.OnUiThread(() => { this.SpinWait(start: false); });

            if (!string.IsNullOrWhiteSpace(error))
            {
                this.Logger.Error(error);
                Dispatch.OnUiThread(() => { this.OnImageFailed(error); });
            }
        }
    }

    private void OnImageFailed(string _)
    {
        this.SourceImage = null;
        this.loadedImage = null;

        // Show error message to user
        this.toaster.Host = this.View.ToasterHost;
        this.toaster.Show(
            this.Localize("Single.LoadImageFailTitle"),
            this.Localize("Single.LoadImageFailMessage"),
            8_000,
            InformationLevel.Error);
    }

    private void OnImageLoaded(Frame frame)
    {
        this.SourceImage = frame.ToWriteableBitmap();
        frame.Dispose();
        Dispatch.OnUiThread(this.View.ZoomableImage.ZoomToFit );
    }

    private void SpinWait(bool start = true)
    {
        this.SpinViewModel.IsVisible = start;
        this.SpinViewModel.IsActive = start;
        this.DropViewModel.IsVisible = !start;
        this.ProcessIsDisabled = start;
    }

    internal void ProcessCurrentImage()
    {
        if (this.loadedImage is null)
        {
            this.Logger.Warning("No image to process.");
            return;
        }

        var libraryManager = this.model.LibraryManager; 
        libraryManager.AddDroppedFile(this.loadedImage);
        if (this.loadedImage.Metadata is Metadata metadata)
        {
            libraryManager.UpdateEditedFile(metadata);
        }
        else
        {
            this.Logger.Warning("Image has no metadata: " + this.loadedImage.LoadedFrom);
            // No need to show error message to user
        }

        this.model.ProcessLoadedImage(this.loadedImage);
        var postProcess = this.model.CurrentPostProcess;
        if (postProcess is not null)
        {
            var shell = App.GetRequiredService<ShellViewModel>();
            shell.EnableAndSelect(ActivatedView.Process);
        }
        else
        {
            this.Logger.Warning("Failed to create post process from dropped file: " + this.loadedImage.LoadedFrom);
            // TODO : Show error message to user
        }
    }
}
