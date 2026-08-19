namespace Lyt.PhotoPostPro.Model.PostProcessors;

public class ExposureStep(PostProcessWorkflow postProcessWorkflow) :
    PostProcessStep(postProcessWorkflow, PostProcessStep.ExposureStepName)
{
    public float Gamma { get; set; }

    public float Gain { get; set; }

    public float Shift { get; set; }

    public override void Initialize(Image<RgbaHalf> _) => this.Clear();

    protected override void SetIdentity()
        => base.IsIdentity =
            MathF.Abs(1.0f - this.Gamma) < 0.001 &&
            MathF.Abs(1.0f - this.Gain) < 0.001 &&
            MathF.Abs(this.Shift) < 0.001f;

    public override Frame? Reset()
    {
        this.Clear();
        return base.Reset();
    }

    public override void PerformStep(PostProcessParameters ppp)
    {
        bool changed =
            MathF.Abs(ppp.ExposureGamma - 1.0f) > 0.000_1f ||
            MathF.Abs(ppp.ExposureGain - 1.0f) > 0.000_1f ||
            MathF.Abs(ppp.ExposureShift) > 0.000_1f;

        if (changed)
        {
            this.AdjustExposure(ppp.ExposureGamma, ppp.ExposureGain, ppp.ExposureShift);
        }
    }

    internal override Frame? Transform(bool withFrame = true)
    {
        if (this.SourceImage is null)
        {
            return null;
        }

        if (this.IsIdentity)
        {
            new GammaLutClearMessage().Publish();
            this.ResultImage = this.SourceImage;
        }
        else
        {
            var clone = this.SourceImage.Clone();
            Half[] lut = clone.Gamma(this.Gamma, this.Gain, this.Shift);
            Curve curve = new(lut);
            new GammaLutGeneratedMessage(curve).Publish();
            this.ResultImage = clone;
        }

        PostProcessStep.RecalculateHistograms(this.ResultImage);
        return withFrame ? this.ResultImage.ToFrame() : null;
    }

    internal Frame? AdjustExposure(float gamma, float gain, float shift)
    {
        this.Gamma = gamma;
        this.Gain = gain;
        this.Shift = shift;
        this.SetIdentity();
        return this.Transform(withFrame: true);
    }

    private void Clear()
    {
        this.Gamma = 1.0f;
        this.Gain = 1.0f;
        this.Shift = 0.0f;
        this.SetIdentity();
        new GammaLutClearMessage().Publish();
    }
}
