namespace Lyt.PhotoPostPro.Model.PostProcessors;

public class RecoveryStep(PostProcessWorkflow postProcessWorkflow) :
    PostProcessStep(postProcessWorkflow, PostProcessStep.RecoveryStepName)
{
    public float ShadowAmount { get; set; }

    public float HighlightAmount { get; set; }

    public override void Initialize(Image<RgbaVector> _) => this.Clear();

    public override void PerformStep(PostProcessParameters ppp)
    {
        bool changed =
            MathF.Abs(ppp.RecoveryHighlightAmount) > 0.000_1f ||
            MathF.Abs(ppp.RecoveryShadowAmount) > 0.000_1f;
        if (changed)
        {
            this.HighlightsShadows(ppp.RecoveryHighlightAmount, ppp.RecoveryShadowAmount);
        }
    }

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
            Math.Abs(this.ShadowAmount) > 0.001 ||
            Math.Abs(this.HighlightAmount) > 0.001;
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