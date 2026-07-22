namespace Lyt.PhotoPostPro.Model.PostProcessors;

public sealed class WhiteBalanceStep(PostProcessWorkflow postProcessWorkflow) : 
    PostProcessStep(postProcessWorkflow, PostProcessStep.WhiteBalanceStepName)
{
    public enum WhiteBalanceAlgorithm
    {
        FilteredGrayWorldAWB, 
        ColorMatrix,
        TannerHelland,
        WhitePatch,
    }

    public float SaturationThreshold { get; set; }

    public float Temperature { get; set; }

    public float Kelvin { get; set; }

    public float Red { get; set; }

    public float Green { get; set; }

    public float Blue { get; set; }

    public WhiteBalanceAlgorithm Algorithm { get; set; }

    public override void Initialize(Image<HalfVector4> _) => this.Clear();

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

            case WhiteBalanceAlgorithm.WhitePatch:
                clone.WhitePatchWhiteBalance(this.Red, this.Green, this.Blue);
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

    internal Frame? WhitePatchWhiteBalance(float r, float g, float b)
    {
        this.Algorithm = WhiteBalanceAlgorithm.WhitePatch;
        this.Red = r;
        this.Green = g;
        this.Blue = b;
        return this.Transform(withFrame: true);
    }

    private void Clear()
    {
        this.Algorithm = WhiteBalanceAlgorithm.FilteredGrayWorldAWB;
        this.SaturationThreshold = 0.4f;

        // Clear all properties so that the UI sliders are also reset to default on Reset 
        this.Temperature = 0.0f;
        this.Kelvin = 1000.0f; // This one hidden for now 
        this.Red = 0.0f;
        this.Green = 0.0f;
        this.Blue = 0.0f;
    }
}
