namespace Lyt.PhotoPostPro.Workflow.Import.File;

// Do not add those ImageSharp namespaces to global using as some class definitions conflict
// with the ones from Avalonia. (Point, Rectangle, etc.) 
//using SixLabors.ImageSharp;
//using SixLabors.ImageSharp.PixelFormats;

public sealed partial class FileImportViewModel : ViewModel<FileImportView>
{
    private readonly PhotoPostProModel model;
    private readonly IToaster toaster;

    private LoadedImage? loadedImage;

    [ObservableProperty]
    public partial bool IsFileMode { get; set; } = true;

    [ObservableProperty]
    public partial bool IsNewAddition { get; set; } = true;

    [ObservableProperty]
    public partial string Message { get; set; } = string.Empty;

    [ObservableProperty]
    public partial WriteableBitmap? SourceImage { get; set; }

    [ObservableProperty]
    public partial SpinViewModel SpinViewModel { get; set; }

    [ObservableProperty]
    public partial bool ProcessIsDisabled { get; set; } = true;

    [ObservableProperty]
    public partial MetadataViewModel MetadataViewModel { get; set; }

    public FileImportViewModel(PhotoPostProModel model, IToaster toaster)
    {
        this.model = model;
        this.toaster = toaster;
        this.MetadataViewModel = new();
        this.IsFileMode = false;
        this.SpinViewModel = new SpinViewModel()
        {
            IsVisible = false,
            IsActive = false,
        };
    }

    public void OnSingleFileDrop(string path)
    {
        // Clear the previous image, if any, to avoid a blink 
        this.SourceImage = null;

        // Always launch a spinner for big or small files 
        this.Message = string.Empty;
        this.SpinWait(start: true);
        Task.Run(() => { this.TryLoadImage(path); });
    }

    [RelayCommand]
    public void OnProcess()
    {
        var mainWindow = App.MainWindow;
        if (mainWindow.CanMaximize)
        {
            mainWindow.WindowState = WindowState.Maximized;
        }

        this.ProcessCurrentImage();
    }

    [RelayCommand]
    public void OnAdd()
    {
        this.AddImageToLibrary();
        this.GoBack();
    }

    [RelayCommand]
    public void OnBack() => this.GoBack(); 

    private void GoBack () 
    {
        // Go back to import drop screen 
        var importVm = App.GetRequiredService<ImportViewModel>();
        importVm.SetInitialState();
    }

    private void AddImageToLibrary ()
    {
        if (this.loadedImage is null)
        {
            this.Logger.Warning("No image to process.");
            return;
        }

        this.model.LibraryManager.AddDroppedFile(this.loadedImage);
    }

    private void TryLoadImage(string path)
    {
        string error = string.Empty;
        try
        {
            this.loadedImage = ImageLoader.LoadImage(path);
            this.loadedImage.CreateThumbnail();
            if (loadedImage.IsSuccess && loadedImage.IsFullyLoadedWithThumbnail)
            {
                bool isAlreadyInLibray = false;
                if ( loadedImage.Metadata is Metadata metadata)
                {
                    if (this.model.LibraryManager.IsAlreadyInLibrary(metadata) )
                    {
                        isAlreadyInLibray = true;
                    }
                }

                Dispatch.OnUiThread(() =>
                {
                    if (isAlreadyInLibray)
                    {
                        // Hide 'Add' button: Show message instead 
                        this.Message = this.Localize("Import.File.AlreadyIn");
                        this.IsNewAddition = false;
                    }
                    else
                    {
                        this.Message = string.Empty;
                        this.IsNewAddition = true;
                    }
                }, DispatcherPriority.Background);

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
            Dispatch.OnUiThread(() => { this.SpinWait(start: false); }, DispatcherPriority.Background);

            if (!string.IsNullOrWhiteSpace(error))
            {
                this.Logger.Error(error);
                Dispatch.OnUiThread(() => { this.OnImageFailed(error); }, DispatcherPriority.Background);
            }
        }
    }

    private void OnImageFailed(string _)
    {
        this.SourceImage = null;
        this.loadedImage = null;

        // Show error message to user
        this.Message = this.Localize("Single.LoadImageFailMessage");
        this.IsNewAddition = false; 

        //this.toaster.Host = this.View.ToasterHost;
        //this.toaster.Show(
        //    this.Localize("Single.LoadImageFailTitle"),
        //    this.Localize("Single.LoadImageFailMessage"),
        //    8_000,
        //    InformationLevel.Error);
    }

    private void OnImageLoaded(Frame frame)
    {
        this.SourceImage = frame.ToWriteableBitmap();
        frame.Dispose();
        Dispatch.OnUiThread(this.View.ZoomableImage.ZoomToFit);
    }

    private void SpinWait(bool start = true)
    {
        this.SpinViewModel.IsVisible = start;
        this.SpinViewModel.IsActive = start;
        this.ProcessIsDisabled = start;
    }

    internal void ProcessCurrentImage()
    {
        if (this.loadedImage is null)
        {
            this.Logger.Warning("No image to process.");
            return;
        }

        this.AddImageToLibrary();

        if (this.loadedImage.Metadata is Metadata metadata)
        {
            this.model.LibraryManager.UpdateEditedFile(metadata);
        }
        else
        {
            this.Logger.Warning("Image has no metadata: " + this.loadedImage.LoadedFrom);
            // No need to show error message to user
        }

        this.model.ProcessLoadedImage(this.loadedImage);
        var workflow = this.model.CurrentWorkflow;
        if (workflow is not null)
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
