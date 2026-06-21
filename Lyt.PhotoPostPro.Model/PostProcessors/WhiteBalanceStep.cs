namespace Lyt.PhotoPostPro.Model.PostProcessors;

public sealed class WhiteBalanceStep() : PostProcessStep(PostProcessStep.WhiteBalanceStepName)
{
    public enum WhiteBalanceAlgorithm
    {
        FilteredGrayWorldAWB, 
        ColorMatrix, 
        TannerHelland, 
    }

    public float SaturationThreshold { get; set; }

    public float Temperature { get; set; }

    public float Kelvin { get; set; }

    public WhiteBalanceAlgorithm Algorithm { get; set; }

    public override void Initialize(Image<Rgb48> _) => this.Clear();

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
        bool isChanged = true; // For now 
        switch (this.Algorithm)
        {
            case WhiteBalanceAlgorithm.ColorMatrix:
                clone.ApplyColorTemperature(this.Temperature);
                break;

            case WhiteBalanceAlgorithm.TannerHelland:
                clone.AdjustColorTemperature(this.Kelvin);
                break;

            case WhiteBalanceAlgorithm.FilteredGrayWorldAWB:
                clone.FilteredGrayWorldAWB(this.SaturationThreshold);
                break;

            default:
                throw new NotImplementedException("No such White Balance algorithm");
        }

        PostProcessStep.RecalculateHistograms(clone);
        this.ResultImage = isChanged ? clone : this.SourceImage;
        return withFrame ? clone.ToFrame() : null;
    }

    internal Frame? TannerHellandWhiteBalance(float kelvin)
    {
        this.Algorithm = WhiteBalanceAlgorithm.TannerHelland;
        this.Kelvin = kelvin;
        return this.Transform(withFrame: true);
    }

    internal Frame? ColorMatrixWhiteBalance(float temperature)
    {
        this.Algorithm = WhiteBalanceAlgorithm.ColorMatrix;
        this.Temperature = temperature;
        return this.Transform(withFrame: true);
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
}
