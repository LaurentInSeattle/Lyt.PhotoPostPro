namespace Lyt.PhotoPostPro.Workflow.Single;

// Do not add those ImageSharp namespaces to global using as some class definitions conflict
// with the ones from Avalonia. (Point, Rectangle, etc.) 
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

public sealed partial class SingleViewModel : ViewModel<SingleView>
{
    private readonly PhotoPostProModel model;

    private string imagePath;
    private Image<Rgb48>? image;

    [ObservableProperty]
    public partial WriteableBitmap? SourceImage { get; set; }

    public SingleViewModel(PhotoPostProModel model)
    {
        this.model = model;
        this.imagePath = string.Empty;
    }

    internal void OnDropPath(string path, bool isDirectory)
    {
        if (isDirectory)
        {
            this.Logger.Warning("Dropped path is a directory, expected a file.");
            return;
        }
        else
        {
            try
            {
                // TODO: Launch a spinner for big files 
                this.image = ImageLoader.LoadImage(path, out string errorMessage);
                if (this.image is not null)
                {
                    var imageFrame = Model.Utilities.ImagingUtilities.ToFrame(image);
                    if (imageFrame is not null)
                    {
                        this.SourceImage = imageFrame.ToWriteableBitmap();
                        this.imagePath = path;
                    }
                    else
                    {
                        this.Logger.Warning("Failed to load image file.");
                        // TODO : Show error message to user
                    }
                }
                else
                {
                    this.Logger.Warning("Failed to load image file: " + errorMessage);
                    // TODO : Show error message to user
                }
            }
            catch (Exception ex)
            {
                this.Logger.Error("Error loading image file: " + ex.Message);
                // TODO : Show error message to user
            }
        }
    }

    internal void ProcessCurrentImage()
    {
        if (string.IsNullOrWhiteSpace(this.imagePath) || this.image is null)
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
