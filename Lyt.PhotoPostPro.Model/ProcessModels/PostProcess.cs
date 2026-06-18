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
    public Image<Rgb48>? MaybeSourceImage { get; set; }

    [JsonIgnore]
    public Image<Rgb48> SourceImage
        =>  this.MaybeSourceImage ??
            throw new InvalidOperationException("Source image must be loaded before accessing it.");

    public bool IsInvalid
        =>
            string.IsNullOrWhiteSpace(this.Name) ||
            string.IsNullOrWhiteSpace(this.SourceFilePath);

    public PostProcessWorkflow? Workflow { get; private set; }

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

    public bool LoadSourceImage(Image<Rgb48>? image, out string errorMessage)
    {
        errorMessage = string.Empty;
        if (image is null)
        {
            try
            {
                image = ImageLoader.LoadImage(this.SourceFilePath, out errorMessage);
                bool loaded = image is not null;
                this.MaybeSourceImage = image;
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
            this.MaybeSourceImage = image;
            return true;
        }
    }

    public void Initialize() => this.Workflow = new();

    public void Begin()
    {
        if (this.Workflow is not null)
        {
            this.Workflow.Begin(this.SourceImage);
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
