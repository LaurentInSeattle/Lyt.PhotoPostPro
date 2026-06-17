namespace Lyt.PhotoPostPro.Model.PostProcessors;

internal class ExposureStep() : PostProcessStep(PostProcessStep.ExposureStepName)
{
    public double Gamma { get; set; }
    
    public double Gain { get; set; }

    public int Shift { get; set; }

    public override void Initialize()
    {
        this.Gamma = 1.0;
        this.Gain = 1.0;
        this.Shift = 0;
    }

    public override Frame? Transform(bool withFrame = true)
    {
        if (this.SourceImage is null)
        {
            return null;
        }

        var clone = this.SourceImage.Clone();
        bool isChanged =
            Math.Abs(1 - this.Gamma) > 0.001 ||
            Math.Abs(1 - this.Gain) > 0.001 ||
            this.Shift != 0;
        if (isChanged)
        {
            ushort[] lut = clone.Gamma(this.Gamma, this.Gain, this.Shift);
            Curve curve = new(lut);
            new GammaLutGeneratedMessage(curve).Publish();
        }

        this.ResultImage = isChanged ? clone : this.SourceImage;
        if (isChanged)
        {
            base.RecalculateHistograms(clone);
        }

        return withFrame ? clone.ToFrame() : null;
    }

    internal Frame? AdjustExposure(double gamma, double gain, int shift)
    {
        this.Gamma = gamma;
        this.Gain = gain;
        this.Shift = shift;
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
