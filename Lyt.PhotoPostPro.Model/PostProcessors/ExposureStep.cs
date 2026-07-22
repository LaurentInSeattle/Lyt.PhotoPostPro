namespace Lyt.PhotoPostPro.Model.PostProcessors;

public class ExposureStep(PostProcessWorkflow postProcessWorkflow) : 
    PostProcessStep(postProcessWorkflow, PostProcessStep.ExposureStepName)
{
    public float Gamma { get; set; }
    
    public float Gain { get; set; }

    public float Shift { get; set; }

    public override void Initialize(Image<RgbaVector> _) => this.Clear();

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
        bool isChanged =
            Math.Abs(1.0 - this.Gamma) > 0.001 ||
            Math.Abs(1.0 - this.Gain) > 0.001 ||
            this.Shift != 0;
        if (isChanged)
        {
            float[] lut = clone.Gamma(this.Gamma, this.Gain, this.Shift);
            Curve curve = new(lut);
            new GammaLutGeneratedMessage(curve).Publish();
            PostProcessStep.RecalculateHistograms(clone);
        }

        this.ResultImage = isChanged ? clone : this.SourceImage;
        return withFrame ? clone.ToFrame() : null;
    }

    internal Frame? AdjustExposure(float gamma, float gain, float shift)
    {
        this.Gamma = gamma;
        this.Gain = gain;
        this.Shift = shift;
        return this.Transform(withFrame: true);
    }

    private void Clear ()
    {
        this.Gamma = 1.0f;
        this.Gain = 1.0f;
        this.Shift = 0.0f;
        new GammaLutClearMessage().Publish();
    }
}
