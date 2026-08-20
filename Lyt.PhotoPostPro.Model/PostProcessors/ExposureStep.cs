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
        => this.AdjustExposure(ppp.ExposureGamma, ppp.ExposureGain, ppp.ExposureShift, withFrame: false);

    internal override Frame? Transform(bool withFrame = true)
    {
        if (this.IsIdentity)
        {
            new GammaLutClearMessage().Publish();
        }

        return base.DoTransform((clone) =>
        {
            Half[] lut = clone.Gamma(this.Gamma, this.Gain, this.Shift);
            Curve curve = new(lut);
            new GammaLutGeneratedMessage(curve).Publish();
        }, withFrame);
    } 

    internal Frame? AdjustExposure(float gamma, float gain, float shift, bool withFrame = true)
    {
        this.Gamma = gamma;
        this.Gain = gain;
        this.Shift = shift;
        this.SetIdentity();
        return this.Transform(withFrame);
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
