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

    private LoadedImage? loadedImage;

    [ObservableProperty]
    public partial WriteableBitmap? SourceImage { get; set; }

    public SingleViewModel(PhotoPostProModel model, IToaster toaster)
    {
        this.model = model;
        this.toaster = toaster;
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

    private void OnImageFailed(string error)
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
    }

    private void SpinWait(bool start = true)
    {
        var toolboxViewModel = App.GetRequiredService<SingleToolboxViewModel>();
        toolboxViewModel.SpinViewModel.IsVisible = start;
        toolboxViewModel.SpinViewModel.IsActive = start;
        toolboxViewModel.DropViewModel.IsVisible = !start;
        toolboxViewModel.ProcessIsDisabled = start;
    }

    internal void ProcessCurrentImage()
    {
        if (this.loadedImage is null)
        {
            this.Logger.Warning("No image to process.");
            return;
        }

        this.model.LibraryManager.AddDroppedFile(this.loadedImage);
        this.model.ProcessLoadedImage(this.loadedImage);
        var postProcess = this.model.CurrentPostProcess;
        if (postProcess is not null)
        {
            var shell = App.GetRequiredService<ShellViewModel>();
            shell.EnableAndSelect(ActivatedView.Process);
        }
        else
        {
            this.Logger.Warning("Failed to create post process from dropped file: ");
            // TODO : Show error message to user
        }
    }
}
