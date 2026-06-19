namespace Lyt.PhotoPostPro.Model.PostProcessors;

public sealed class WhiteBalanceStep() : PostProcessStep(PostProcessStep.WhiteBalanceStepName)
{
    public enum WhiteBalanceAlgorithm
    {
        FilteredGrayWorldAWB, 
    }

    public float SaturationThreshold { get ; set; }

    public WhiteBalanceAlgorithm Algorithm { get; set; }

    public override void Initialize(Image<Rgb48> _) => this.Clear();

    public override Frame? Transform(bool withFrame = true)
    {
        if (this.SourceImage is null)
        {
            return null;
        }

        var clone = this.SourceImage.Clone();

        clone.FilteredGrayWorldAWB(this.SaturationThreshold); 
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
        this.Algorithm = WhiteBalanceAlgorithm.FilteredGrayWorldAWB;
        this.SaturationThreshold = saturationThreshold;
        return this.Transform(withFrame: true);
    }

    private void Clear()
    {
        this.Algorithm = WhiteBalanceAlgorithm.FilteredGrayWorldAWB;
        this.SaturationThreshold = 0.4f;
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
}
