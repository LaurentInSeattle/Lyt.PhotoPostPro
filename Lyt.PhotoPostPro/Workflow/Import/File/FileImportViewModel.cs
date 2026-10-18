namespace Lyt.PhotoPostPro.Workflow.Import.File;

// Do not add those ImageSharp namespaces to global using as some class definitions conflict
// with the ones from Avalonia. (Point, Rectangle, etc.) 
//using SixLabors.ImageSharp;
//using SixLabors.ImageSharp.PixelFormats;

public sealed partial class FileImportViewModel : ViewModel<FileImportView>
{
    private const long ImageFileMaxLength = 140L * 1024L * 1024L;
    
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
        GoBack();
    }

#pragma warning disable CA1822 // Mark members as static
    // Relay commands cannot be static 

    [RelayCommand]
    public void OnBack() => GoBack();

#pragma warning restore CA1822 // Mark members as static

    private static void GoBack () 
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

                throw new Exception(logMessage); 
            }

            FileInfo fileInfo = new(path);
            if (!fileInfo.Exists)
            {
                LogAndMessageUser($"File does not exist: {path}", "File.NotExist");
            }

            long length = fileInfo.Length;
            if ((length == 0) || (length < 256L))
            {
                LogAndMessageUser($"File length is too small: {path}", "File.TooSmall");
            }

            if (length > ImageFileMaxLength)
            {
                LogAndMessageUser($"File length is too large: {path}", "File.TooBig");
            }

            if (ImageLoader.IsNotSupported(path))
            {
                LogAndMessageUser($"Image File format not supported: {path}", "File.ImageNotSupported");
            }

            if (!FileSystemExtensions.IsReadable(path))
            {
                LogAndMessageUser($"File cannot be read: {path}", "File.CantRead");
            }

            this.loadedImage = ImageLoader.LoadImage(path);
            this.loadedImage.CreateThumbnail();
            if (this.loadedImage.IsSuccess && this.loadedImage.IsFullyLoadedWithThumbnail)
            {
                bool isAlreadyInLibray = false;
                if (this.loadedImage.Metadata is Metadata metadata)
                {
                    if (this.model.LibraryManager.IsAlreadyInLibrary(metadata, out Metadata? existingMetadata) )
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

                        // Toast with longer message
                        this.toaster.Show(
                            this.Message, this.Localize("Import.File.AlreadyIn.Long"), 3_000, InformationLevel.Warning);
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
                    this.Logger.Error(error);
                    LogAndMessageUser($"Image file cannot be decoded: {path}", "File.ImageNotDecoded");
                }
            }
            else
            {
                error = "Failed to load image file: " + this.loadedImage.ErrorMessage;
                this.Logger.Error(error);
                LogAndMessageUser($"Image file cannot be decoded: {path}", "File.ImageNotDecoded");
            }
        }
        catch (Exception ex)
        {
            string exceptionMessage = "Error loading image file, Exception thrown: " + ex.Message;
            this.Logger.Error(exceptionMessage);
        }
        finally
        {
            // Error or not: stop the spinner 
            Dispatch.OnUiThread(() => { this.SpinWait(start: false); }, DispatcherPriority.Background);

            if (!string.IsNullOrWhiteSpace(error))
            {
                Dispatch.OnUiThread(() => { this.OnImageFailed(error); }, DispatcherPriority.Background);
            }
        }
    }

    private void OnImageFailed(string _)
    {
        this.SourceImage = null;
        this.loadedImage = null;

        // Show error message to user on the main view 
        this.Message = this.Localize("Single.LoadImageFailMessage");
        this.IsNewAddition = false; 
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

        if (this.loadedImage.Metadata is not Metadata metadata)
        {
            this.Logger.Warning("Image has no metadata: " + this.loadedImage.LoadedFrom);
            return; 
        }

        if (this.model.LibraryManager.IsAlreadyInLibrary(metadata, out Metadata? existingMetadata))
        {
            if ( existingMetadata is not null)
            {
                var libraryViewModel = App.GetRequiredService<LibraryViewModel>();
                libraryViewModel.ProcessLibraryImage(existingMetadata);
            }
        }
        else
        {
            this.AddImageToLibrary();
            this.model.LibraryManager.UpdateEditedFile(metadata);
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
            }
        }
    }
}
