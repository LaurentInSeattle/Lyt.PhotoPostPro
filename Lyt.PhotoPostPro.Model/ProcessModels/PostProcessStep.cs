namespace Lyt.PhotoPostPro.Model.ProcessModels;

public class PostProcessStep(string name)
{
    public const string StartStepName = "Start";
    public const string EndStepName = "End";

    public const string OrientationStepName = "Orientation";
    public const string StraightenStepName = "Straighten";
    public const string CompositionStepName = "Composition";
    public const string ExposureStepName = "Exposure";
    public const string RecoveryStepName = "Recovery";
    public const string WhiteBalanceStepName = "WhiteBalance";

    public string Name { get; set; } = name;

    [JsonIgnore]
    public PostProcessStep? PreviousStep { get; set; }

    [JsonIgnore]
    public PostProcessStep? NextStep { get; set; } 

    public bool IsFirstStep => this.PreviousStep is null;

    public bool IsLastStep => this.NextStep is null;

    [JsonIgnore]
    public bool IsCurrent { get; set; }

    [JsonIgnore]
    public bool IsSkipped { get; set; }

    [JsonIgnore]
    public Image<Rgb48>? SourceImage { get; set; }

    [JsonIgnore]
    public Image<Rgb48>? ResultImage { get; set; }

    // Default implementation does nothing. Override in derived classes if needed.
    public virtual void Initialize() { } 

    // Default implementation does nothing. Override in derived classes if needed.
    public virtual void Finish() { } 

    // Default implementation does nothing. Override in derived classes if needed.
    public virtual void Save() { }

    // Default implementation does nothing. Override in derived classes if needed.
    public virtual void Skip() { }

    // Default implementation does nothing. Override in derived classes if needed.
    public virtual Frame? Transform(bool withFrame = true) => null;

    protected void RecalculateHistograms(Image<Rgb48> image)
    {
        Task.Run(() =>
        {
            Histograms histograms = new(image); 
            new HistogramsGeneratedMessage(histograms).Publish();
        });
    }
}
