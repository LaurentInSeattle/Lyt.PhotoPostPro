namespace Lyt.PhotoPostPro.Model.PostProcessors;

public class RecoveryStep(PostProcessWorkflow postProcessWorkflow) : 
    PostProcessStep(postProcessWorkflow, PostProcessStep.RecoveryStepName)
{
    public float ShadowAmount { get; set; }

    public float HighlightAmount { get; set; }

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

        bool isChanged =
            Math.Abs(1.0 - this.ShadowAmount) > 0.001 ||
            Math.Abs(1.0 - this.HighlightAmount) > 0.001 ;
        var clone = this.SourceImage.Clone();
        if (isChanged)
        {
            clone.HighlightsShadows(this.HighlightAmount, this.ShadowAmount);
            PostProcessStep.RecalculateHistograms(clone);
        }

        this.ResultImage = isChanged ? clone : this.SourceImage;
        return withFrame ? clone.ToFrame() : null;
    }

    internal Frame? HighlightsShadows(float highlightAmount, float shadowAmount)
    {
        this.HighlightAmount = highlightAmount;
        this.ShadowAmount = shadowAmount;
        return this.Transform(withFrame: true);
    }

    private void Clear()
    {
        this.ShadowAmount = 0.0f;
        this.HighlightAmount = 0.0f;
    }
}