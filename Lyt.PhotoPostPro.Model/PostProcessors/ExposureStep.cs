namespace Lyt.PhotoPostPro.Model.PostProcessors;

internal class ExposureStep() : PostProcessStep(PostProcessStep.ExposureStepName)
{
    double gamma;
    double gain;
    int shift;

    public override void Initialize()
    {
        this.gamma = 1.0;
        this.gain = 1.0;
        this.shift = 0;
    }

    public override Frame? Transform(bool withFrame = true)
    {
        if (this.SourceImage is null)
        {
            return null;
        }

        var clone = this.SourceImage.Clone();
        bool isChanged =
            Math.Abs(1 - this.gamma) > 0.001 ||
            Math.Abs(1 - this.gain) > 0.001 ||
            this.shift != 0;
        if (isChanged)
        {
            ushort[] lut = clone.Gamma(this.gamma, this.gain, this.shift);
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
        this.gamma = gamma;
        this.gain = gain;
        this.shift = shift;
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
