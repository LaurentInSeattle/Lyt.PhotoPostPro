namespace Lyt.PhotoPostPro.Model.ProcessModels;

public sealed class PostProcess
{
    public PostProcess() { /* Required for serialization */ }

    public required string ProjectId { get; set; } = FilenamesMgr.NewShortId();

    public required string ProcessId { get; set; } = FilenamesMgr.NewShortId();

    public required string Name { get; set; } = string.Empty;

    public required DateTime Created { get; set; } = DateTime.Now;

    public required DateTime LastUpdated { get; set; } = DateTime.Now;

    public required string SourceFilePath { get; set; } = string.Empty;

    public void SetProject(Project project) => this.MaybeProject = project;

    [JsonIgnore]
    public Project? MaybeProject { get; set; }

    [JsonIgnore]
    public Project Project 
        =>  this.MaybeProject ?? 
            throw new InvalidOperationException("Project must be set before accessing it.");

    [JsonIgnore]
    public Image<Rgb48>? MaybeOriginalImage { get; set; }

    [JsonIgnore]
    public Image<Rgb48> OriginalImage
        =>  this.MaybeOriginalImage ??
            throw new InvalidOperationException("Source image must be loaded before accessing it.");

    [JsonIgnore]
    public PostProcessWorkflow? Workflow { get; private set; }

    [JsonIgnore]
    public ProcessMetadata? ProcessMetadata { get; private set; }

    public bool IsInvalid
        =>
            string.IsNullOrWhiteSpace(this.Name) ||
            string.IsNullOrWhiteSpace(this.SourceFilePath);

    public bool Validate(out string errorMessageKey)
    {
        errorMessageKey = string.Empty;
        if (string.IsNullOrWhiteSpace(this.Name))
        {
            errorMessageKey = "Model.Project.Name";
            return false;
        }

        if (string.IsNullOrWhiteSpace(this.SourceFilePath))
        {
            errorMessageKey = "Model.Project.SourceFile";
            return false;
        }

        FileInfo fileInfo = new(Path.Combine(this.Project.Metadata.SourceFolderPath, this.SourceFilePath));
        if (!fileInfo.Exists)
        {
            errorMessageKey = "Model.Project.SourceFile";
        }

        // CONSIDER ~ LATER
        // Try to write a dummy file in the target directory to ensure write access is granted
        return true;
    }

    public bool LoadSourceImage(Image<Rgb48>? image, ProcessMetadata? processMetadata, out string errorMessage)
    {
        errorMessage = string.Empty;
        if (image is null || processMetadata is null)
        {
            try
            {
                (image, processMetadata) = ImageLoader.LoadImage(this.SourceFilePath, out errorMessage);
                bool loaded = image is not null && processMetadata is not null ;
                if (loaded)
                {
                    this.MaybeOriginalImage = image;
                    this.ProcessMetadata = processMetadata;

                    // nullable " ! " : checked by loaded 
                    new MetadataGeneratedMessage(processMetadata!).Publish();
                }

                return loaded;
            }
            catch (Exception ex)
            {
                errorMessage = "An error occurred while loading the source image." + ex.Message;
                Debug.WriteLine(ex);
                return false;
            }
        } 
        else
        {
            this.MaybeOriginalImage = image;
            return true;
        }
    }

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
