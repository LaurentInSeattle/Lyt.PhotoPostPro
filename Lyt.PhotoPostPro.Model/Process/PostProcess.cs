namespace Lyt.PhotoPostPro.Model.Process;

public sealed class PostProcess
{
    public PostProcess(
        PhotoPostProModel model, 
        Metadata metadata, 
        Image<RgbaHalf> originalImage,
        bool isNew, 
        string fileUidString,
        PostProcessParameters postProcessParameters)
    {
        this.MaybeModel = model;
        this.Metadata = metadata;
        this.MaybeOriginalImage = originalImage;
        this.IsNew = isNew;
        this.FileUidString = fileUidString;
        this.PostProcessParameters = postProcessParameters;
        this.Workflow = new PostProcessWorkflow(this); 
    }

    public PhotoPostProModel? MaybeModel { get; set; }

    public PhotoPostProModel Model
        =>  this.MaybeModel ??
            throw new InvalidOperationException("Model must be set before accessing it.");

    public Image<RgbaHalf>? MaybeOriginalImage { get; set; }

    public Image<RgbaHalf> OriginalImage
        =>  this.MaybeOriginalImage ??
            throw new InvalidOperationException("Source image must be loaded before accessing it.");

    public PostProcessWorkflow Workflow { get; set; }

    public Metadata Metadata { get; set; }

    public bool IsNew { get; private set; }

    public string FileUidString { get; private set; } = string.Empty;

    public PostProcessParameters PostProcessParameters { get; private set; }

    public string SourceFilePath => this.Metadata.FullPath;

    public void Begin()
    {
        if (this.Workflow is not null)
        {
            // Auto gives a first star when beginning editing
            if ( this.Metadata.Rating == 0)
            {
                ++this.Metadata.Rating;
            }

            this.Metadata.LastEditedUTC = DateTime.UtcNow;
            this.Model.LibraryManager.SaveMetadata(this.Metadata);

            this.Workflow.Begin(this.OriginalImage);
        }
        else
        {
            Debug.WriteLine("Workflow is null");
        }
    } 
    
    public void Finish()
    {
        if (this.Workflow is not null)
        {
            this.Workflow.Finish();
        }
        else
        {
            Debug.WriteLine("Workflow is null");
        }
    }
}
