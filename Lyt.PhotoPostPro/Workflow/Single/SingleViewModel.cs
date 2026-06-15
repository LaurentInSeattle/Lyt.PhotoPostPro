namespace Lyt.PhotoPostPro.Workflow.Single;

public sealed partial class SingleViewModel : ViewModel<SingleView>
{
    private readonly PhotoPostProModel model;

    private string imagePath;

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
                var image = ImageLoader.LoadImage(path, out string errorMessage);
                if (image is not null)
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
        if (string.IsNullOrWhiteSpace(this.imagePath))
        {
            this.Logger.Warning("No image to process.");
            return;
        }

        this.model.NewProject(
            name: System.IO.Path.GetFileNameWithoutExtension(this.imagePath),
            folderPath: this.imagePath,
            isSingleImage: true,
            out string errorMessage);
        if (!string.IsNullOrEmpty(errorMessage))
        {
            this.Logger.Warning("Failed to create project from dropped file: " + errorMessage);
            // TODO : Show error message to user
        }

        var postProcess = this.model.CurrentPostProcess;
        if (postProcess is not null)
        {

            ViewSelector<ActivatedView>.Enable(ActivatedView.Process);
            ApplicationMessagingExtensions.Select(ActivatedView.Process);
        }
        else
        {
            this.Logger.Warning("Failed to create project from dropped file: " + errorMessage);
            // TODO : Show error message to user
        }
    }
}
