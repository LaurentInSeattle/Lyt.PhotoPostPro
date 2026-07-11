namespace Lyt.PhotoPostPro.Workflow.Single;

using Lyt.PhotoPostPro.Model.Loader;

// Do not add those ImageSharp namespaces to global using as some class definitions conflict
// with the ones from Avalonia. (Point, Rectangle, etc.) 
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

public sealed partial class SingleViewModel : ViewModel<SingleView>, IDropPathHandler
{
    private readonly PhotoPostProModel model;
    private readonly IToaster toaster; 

    private string imagePath;
    private Image<Rgb48>? image;
    private Metadata? metadata; 

    [ObservableProperty]
    public partial WriteableBitmap? SourceImage { get; set; }

    public SingleViewModel(PhotoPostProModel model, IToaster toaster)
    {
        this.model = model;
        this.toaster = toaster;
        this.imagePath = string.Empty;
    }

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
            this.SpinWait(start: true); 
            Task.Run(() => { this.TryLoadImage(path); } );
        }
    } 

    private void TryLoadImage(string path) 
    {
        string error = string.Empty;
        try
        {
            LoadedImage loadedImage = ImageLoader.LoadImage(path);
            if (loadedImage.IsSuccess && loadedImage.IsFullyLoaded)
            {
                this.image = loadedImage.Image;
                this.metadata = loadedImage.Metadata;
                // ! Verified by loadedImage.IsFullyLoaded
                var imageFrame = ImagingUtilities.ToFrame(this.image!);
                if (imageFrame is not null)
                {
                    Dispatch.OnUiThread(()=>
                    { 
                        this.OnImageLoaded (imageFrame, path);
                        // ! Verified by loadedImage.IsFullyLoaded
                        new MetadataGeneratedMessage(this.metadata!).Publish();
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

    private void OnImageFailed(string error)
    {
        this.SourceImage = null;
        this.imagePath = string.Empty;
        this.image = null; 
        this.metadata = null;
        
        // Show error message to user
        this.toaster.Host = this.View.ToasterHost;
        this.toaster.Show(
            this.Localize("Single.LoadImageFailTitle"), 
            this.Localize("Single.LoadImageFailMessage"),
            8_000, 
            InformationLevel.Error);
    }

    private void OnImageLoaded (Frame frame, string path )
    {
        this.SourceImage = frame.ToWriteableBitmap();
        this.imagePath = path;
    }

    private void SpinWait ( bool start = true )
    {
        var toolboxViewModel = App.GetRequiredService<SingleToolboxViewModel>();
        toolboxViewModel.SpinViewModel.IsVisible = start;
        toolboxViewModel.SpinViewModel.IsActive = start;
        toolboxViewModel.DropViewModel.IsVisible = ! start;
        toolboxViewModel.ProcessIsDisabled = start;
    }

    internal void ProcessCurrentImage()
    {
        if (string.IsNullOrWhiteSpace(this.imagePath) || this.image is null || this.metadata is null)
        {
            this.Logger.Warning("No image to process.");
            return;
        }

        var project = this.model.CurrentProject; 
        if (project is not null)
        {
            this.model.CloseProject(out string errorMessageClose);
            if (!string.IsNullOrEmpty(errorMessageClose))
            {
                this.Logger.Warning("Failed to close project: " + errorMessageClose);
                // TODO : Show error message to user
                return; 
            }
        }

        this.model.NewProject(
            name: System.IO.Path.GetFileNameWithoutExtension(this.imagePath),
            folderPath: this.imagePath,
            isSingleImage: true,
            this.image, 
            this.metadata,
            out string errorMessage);
        if (!string.IsNullOrEmpty(errorMessage))
        {
            this.Logger.Warning("Failed to create project from dropped file: " + errorMessage);
            // TODO : Show error message to user
        }

        var postProcess = this.model.CurrentPostProcess;
        if (postProcess is not null)
        {
            var shell = App.GetRequiredService<ShellViewModel>();
            shell.EnableAndSelect(ActivatedView.Process);  
        }
        else
        {
            this.Logger.Warning("Failed to create project from dropped file: " + errorMessage);
            // TODO : Show error message to user
        }
    }
}
