namespace Lyt.PhotoPostPro.Model.PostProcessors;

internal class RecoveryStep() : PostProcessStep(PostProcessStep.RecoveryStepName)
{
    private float shadowAmount;
    private float highlightAmount;

    public override void Initialize()
    {
        this.shadowAmount = 0.0f;
        this.highlightAmount = 0.0f;
    }

    public override Frame? Transform(bool withFrame = true)
    {
        if (this.SourceImage is null)
        {
            return null;
        }

        var clone = this.SourceImage.Clone();

        clone.HighlightsShadows(this.highlightAmount, this.shadowAmount); 

        base.RecalculateHistograms(clone);

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
        this.highlightAmount = highlightAmount;
        this.shadowAmount = shadowAmount;
        return this.Transform(withFrame: true);
    }

    internal Frame? Clear()
    {
        this.Initialize();

        if (this.SourceImage is null)
        {
            return null;
        }

        this.ResultImage = this.SourceImage;
        return this.SourceImage.ToFrame();
    }
}
