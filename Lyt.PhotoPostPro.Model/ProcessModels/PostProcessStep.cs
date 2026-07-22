namespace Lyt.PhotoPostPro.Model.ProcessModels;

public class PostProcessStep
{
    public const string StartStepName = "Start";
    public const string EndStepName = "End";

    public const string OrientationStepName = "Orientation";
    public const string StraightenStepName = "Straighten";
    public const string CompositionStepName = "Composition";
    public const string ExposureStepName = "Exposure";
    public const string RecoveryStepName = "Recovery";
    public const string WhiteBalanceStepName = "WhiteBalance";
    public const string ContrastStepName = "Contrast";
    public const string LutStepName = "Lut";
    public const string ColorStepName = "Color";
    public const string SharpenStepName = "Sharpen";
    public const string VignetteStepName = "Vignette";
    public const string FiltersStepName = "Filters";
    public const string ExportStepName = "Export";

    public PostProcessStep(PostProcessWorkflow postProcessWorkflow, string name) 
    {
        this.PostProcessWorkflow = postProcessWorkflow;
        this.Name = name;
        this.IsFirstRun = true;
    }

    public string Name { get; set; }

    [JsonIgnore]
    public PostProcessStep? PreviousStep { get; set; }

    [JsonIgnore]
    public PostProcessStep? NextStep { get; set; }

    [JsonIgnore]
    public PostProcessWorkflow PostProcessWorkflow { get; private set; }

    [JsonIgnore]
    public bool IsFirstRun { get; set; }

    [JsonIgnore]
    public Image<RgbaVector>? SourceImage { get; set; }

    [JsonIgnore]
    public Image<RgbaVector>? ResultImage { get; set; }

    public bool IsFirstStep => this.PreviousStep is null;

    public bool IsLastStep => this.NextStep is null;

    // Default implementation does nothing. Override in derived classes if needed.
    public virtual void Initialize(Image<RgbaVector> originalImage) { } 

    // Default implementation does nothing. Override in derived classes if needed.
    public virtual void Finish() { }

    // Default implementation restore original into result 
    public virtual Frame? Reset()
    {
        if (this.SourceImage is null)
        {
            return null;
        }

        this.ResultImage = this.SourceImage;
        return this.SourceImage.ToFrame();
    }

    // Override in derived classes if needed, overrides must call the base class .
    public virtual void Activate(WorkflowUpdateKind workflowUpdateKind) 
    {
        Debug.WriteLine("Activating : " + this.Name + "  " + workflowUpdateKind);
        if (this.IsFirstRun)
        {
            Debug.WriteLine(this.Name + "  - First Run : " + workflowUpdateKind); 
            this.IsFirstRun = false;
            this.Reset() ;
        } 
        else
        {
            this.Transform(withFrame: false);
        }
    }

    // Default implementation does nothing. Override in derived classes if needed.
    public virtual void Deactivate(WorkflowUpdateKind workflowUpdateKind) { }

    // Default implementation does nothing. Override in derived classes is needed.
    public virtual Frame? Transform(bool withFrame = true) => null;

    public static void RecalculateHistograms(Image<RgbaVector> image)
    {
        Task.Run(() =>
        {
            Histograms histograms = new(image); 
            new HistogramsGeneratedMessage(histograms).Publish();
        });
    }
}
