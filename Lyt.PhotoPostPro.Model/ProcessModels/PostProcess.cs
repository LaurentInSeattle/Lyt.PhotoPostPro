namespace Lyt.PhotoPostPro.Model.ProcessModels;

public sealed class PostProcess
{
    /// <summary> CTOR to be used when starting a new process from scratch  </summary>
    public PostProcess(PhotoPostProModel model, Metadata metadata, Image<Rgb48> originalImage)
    {
        this.MaybeModel = model;
        this.Metadata = metadata;
        this.MaybeOriginalImage = originalImage;
        this.Created = DateTime.Now;
        this.LastUpdated = DateTime.Now;
        this.Workflow = new PostProcessWorkflow(this); 
    }

    public PostProcessWorkflow Workflow { get; set; }

    public Metadata Metadata { get; set; } 

    public DateTime Created { get; set; } = DateTime.Now;

    public DateTime LastUpdated { get; set; } = DateTime.Now;

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

    [JsonIgnore]
    public Image<Rgb48>? MaybeOriginalImage { get; set; }

    [JsonIgnore]
    public Image<Rgb48> OriginalImage
        =>  this.MaybeOriginalImage ??
            throw new InvalidOperationException("Source image must be loaded before accessing it.");

    public string SourceFilePath => this.Metadata.FullPath; 

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
        }
        else
        {
            Debug.WriteLine("Workflow is null");
        }
    }
}
