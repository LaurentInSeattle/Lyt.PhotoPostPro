namespace Lyt.PhotoPostPro.Model.ProcessModels;

public class PostProcessStep(PostProcessWorkflow postProcessWorkflow, string name)
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
    public const string ColorStepName = "Color";

    public const string ExportStepName = "Export";

    public PostProcessWorkflow PostProcessWorkflow { get; private set; } = postProcessWorkflow; 
    
    public string Name { get; private set; } = name;

    [JsonIgnore]
    public PostProcessStep? PreviousStep { get; set; }

    [JsonIgnore]
    public PostProcessStep? NextStep { get; set; } 

    public bool IsFirstStep => this.PreviousStep is null;

    public bool IsLastStep => this.NextStep is null;

    [JsonIgnore]
    public bool IsReset { get; set; }

    [JsonIgnore]
    public bool IsCurrent { get; set; }

    [JsonIgnore]
    public Image<Rgb48>? SourceImage { get; set; }

    [JsonIgnore]
    public Image<Rgb48>? ResultImage { get; set; }

    // Default implementation does nothing. Override in derived classes if needed.
    public virtual void Initialize(Image<Rgb48> originalImage) { } 

    // Default implementation does nothing. Override in derived classes if needed.
    public virtual void Finish() { }

    // Default implementation restore original into result 
    public virtual Frame? Reset()
    {
        if (this.SourceImage is null)
        {
            return null;
        }

        this.IsReset = true;
        this.ResultImage = this.SourceImage;
        return this.SourceImage.ToFrame();
    }

    // Default implementations do nothing. Override in derived classes if needed.
    public virtual void Activate(WorkflowUpdateKind workflowUpdateKind) { }

    public virtual void Deactivate(WorkflowUpdateKind workflowUpdateKind) { }

    // Default implementation does nothing. Override in derived classes if needed.
    public virtual Frame? Transform(bool withFrame = true) => null;

    public static void RecalculateHistograms(Image<Rgb48> image)
    {
        Task.Run(() =>
        {
            Histograms histograms = new(image); 
            new HistogramsGeneratedMessage(histograms).Publish();
        });
    }

}
