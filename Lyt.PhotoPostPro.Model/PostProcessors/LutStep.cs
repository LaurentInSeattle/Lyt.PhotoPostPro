namespace Lyt.PhotoPostPro.Model.PostProcessors;

public sealed class LutStep(PostProcessWorkflow postProcessWorkflow) :
    PostProcessStep(postProcessWorkflow, PostProcessStep.LutStepName)
{
    [JsonIgnore]
    public LutMetadata LutMetadata { get; set; } = LutMetadata.Empty;

    public override void Initialize(Image<Rgb48> _) => this.Clear();

    public override Frame? Reset()
    {
        this.Clear();
        return base.Reset();
    }

    public override Frame? Transform(bool withFrame = true)
    {
        if (this.SourceImage is null)
        {
            return null;
        }

        var clone = this.SourceImage.Clone();
        clone.Lut(this.LutMetadata);
        PostProcessStep.RecalculateHistograms(clone);
        this.ResultImage = clone;
        return withFrame ? clone.ToFrame() : null;
    }

    internal Frame? Lut(LutMetadata lutMetadata)
    {
        this.LutMetadata= lutMetadata;
        return this.Transform(withFrame: true);
    }

    private void Clear() => this.LutMetadata = LutMetadata.Empty;     
}