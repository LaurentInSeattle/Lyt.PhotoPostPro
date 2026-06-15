namespace Lyt.PhotoPostPro.Model.PostProcessors;

public sealed class WhiteBalanceStep() : PostProcessStep(PostProcessStep.WhiteBalanceStepName)
{
    public enum Algorithm
    {
        FilteredGrayWorldAWB, 
    }

    private float saturationThreshold;
    private Algorithm algorithm;

    public override void Initialize()
    {
        this.algorithm = Algorithm.FilteredGrayWorldAWB;
        this.saturationThreshold = 0.4f;
    }

    public override Frame? Transform(bool withFrame = true)
    {
        if (this.SourceImage is null)
        {
            return null;
        }

        var clone = this.SourceImage.Clone();

        clone.FilteredGrayWorldAWB(this.saturationThreshold); 
        base.RecalculateHistograms(clone);

        bool isChanged = true ; 
        //        Math.Abs(1 - this.gamma) > 0.001 ||
        //        Math.Abs(1 - this.gain) > 0.001 ||
        //        this.shift != 0;
        //    if (isChanged)
        //    {
        //        ushort[] lut = clone.Gamma(this.gamma, this.gain, this.shift);
        //Curve curve = new(lut);
        //new GammaLutGeneratedMessage(curve).Publish();
        //    }

        this.ResultImage = isChanged ? clone : this.SourceImage;
        //    if (isChanged)
        //    {
        //    base.RecalculateHistograms(clone);
        //}

        return withFrame ? clone.ToFrame() : null;
    }

    internal Frame? FilteredGrayWorldAWB(float saturationThreshold) 
    {
        this.algorithm = Algorithm.FilteredGrayWorldAWB;
        this.saturationThreshold = saturationThreshold;
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
