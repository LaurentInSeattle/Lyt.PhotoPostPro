namespace Lyt.PhotoPostPro.Model.ProcessModels;

public sealed class PostProcess
{
    public PostProcess() { /* Required for serialization */ }

    public required Metadata Metadata { get; set; } 

    public required DateTime Created { get; set; } = DateTime.Now;

    public required DateTime LastUpdated { get; set; } = DateTime.Now;

    public void SetModel(PhotoPostProModel model)
    {
        this.MaybeModel = model;
    }

    [JsonIgnore]
    public PhotoPostProModel? MaybeModel { get; set; }

    [JsonIgnore]
    public PhotoPostProModel Model
        =>  this.MaybeModel ??
            throw new InvalidOperationException("Model must be set before accessing it.");

    //[JsonIgnore]
    //public Project? MaybeProject { get; set; }

    //[JsonIgnore]
    //public Project Project 
    //    =>  this.MaybeProject ?? 
    //        throw new InvalidOperationException("Project must be set before accessing it.");

    [JsonIgnore]
    public Image<Rgb48>? MaybeOriginalImage { get; set; }

    [JsonIgnore]
    public Image<Rgb48> OriginalImage
        =>  this.MaybeOriginalImage ??
            throw new InvalidOperationException("Source image must be loaded before accessing it.");

    [JsonIgnore]
    public PostProcessWorkflow? Workflow { get; private set; }

    public string SourceFilePath => this.Metadata.FullPath; 

    //public bool LoadSourceImage(Image<Rgb48>? image, Metadata? metadata, out string errorMessage)
    //{
    //    errorMessage = string.Empty;
    //    if (image is null || metadata is null)
    //    {
    //        try
    //        {
    //            LoadedImage loadedImage = ImageLoader.LoadImage(this.SourceFilePath);
    //            errorMessage = loadedImage.ErrorMessage; 
    //            bool loaded = image is not null && metadata is not null ;
    //            if (loaded)
    //            {
    //                this.MaybeOriginalImage = image;
    //                this.Metadata = metadata;

    //                // ! nullable : checked by loaded 
    //                new MetadataGeneratedMessage(metadata!).Publish();
    //            }

    //            return loaded;
    //        }
    //        catch (Exception ex)
    //        {
    //            errorMessage = "An error occurred while loading the source image." + ex.Message;
    //            Debug.WriteLine(ex);
    //            return false;
    //        }
    //    } 
    //    else
    //    {
    //        this.MaybeOriginalImage = image;
    //        return true;
    //    }
    //}

    public void Initialize() => this.Workflow = new(this);

    public void Begin()
    {
        if (this.Workflow is not null)
        {
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
            this.Workflow = null;
        }
        else
        {
            Debug.WriteLine("Workflow is null");
        }
    }
}
