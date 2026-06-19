namespace Lyt.PhotoPostPro.Model.PostProcessors;

public class RecoveryStep() : PostProcessStep(PostProcessStep.RecoveryStepName)
{
    public float ShadowAmount { get; set; }

    public float HighlightAmount { get; set; }

    public override void Initialize(Image<Rgb48> _) => this.Clear();

    public override Frame? Transform(bool withFrame = true)
    {
        if (this.SourceImage is null)
        {
            return null;
        }

        var clone = this.SourceImage.Clone();

        clone.HighlightsShadows(this.HighlightAmount, this.ShadowAmount);

        PostProcessStep.RecalculateHistograms(clone);

        bool isChanged = true; 
        //        Math.Abs(1 - this.gamma) > 0.001 ||
        //        Math.Abs(1 - this.gain) > 0.001 ||
        //        this.shift != 0;
        //    if (isChanged)
        //    {
        //        ushort[] lut = clone.Gamma(this.gamma, this.gain, this.shift);
        //Curve curve = new(lut);
        //new GammaLutGeneratedMessage(curve).Publish();
        //    }

        //    this.ResultImage = isChanged ? clone : this.SourceImage;
        //    if (isChanged)
        //    {
        //    base.RecalculateHistograms(clone);
        //}

        this.ResultImage = isChanged ? clone : this.SourceImage;
        return withFrame ? clone.ToFrame() : null;
    }

    internal Frame? HighlightsShadows(float highlightAmount, float shadowAmount)
    {
        this.HighlightAmount = highlightAmount;
        this.ShadowAmount = shadowAmount;
        return this.Transform(withFrame: true);
    }

    //internal Frame? Clear()
    //{
    //    this.Initialize();

    //    if (this.SourceImage is null)
    //    {
    //        return null;
    //    }

    //    this.ResultImage = this.SourceImage;
    //    return this.SourceImage.ToFrame();
    //}

    private void Clear()
    {
        this.ShadowAmount = 0.0f;
        this.HighlightAmount = 0.0f;
    }
}
