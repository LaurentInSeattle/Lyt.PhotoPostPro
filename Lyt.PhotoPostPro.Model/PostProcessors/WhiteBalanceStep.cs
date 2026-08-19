namespace Lyt.PhotoPostPro.Model.PostProcessors;

public sealed class WhiteBalanceStep(PostProcessWorkflow postProcessWorkflow) :
    PostProcessStep(postProcessWorkflow, PostProcessStep.WhiteBalanceStepName)
{
    public enum WhiteBalanceAlgorithm
    {
        None,
        FilteredGrayWorldAWB,
        ColorMatrix,
        WhitePatch,
    }

    public float SaturationThreshold { get; set; }

    public float Temperature { get; set; }

    public float Red { get; set; }

    public float Green { get; set; }

    public float Blue { get; set; }

    public WhiteBalanceAlgorithm Algorithm { get; set; }

    public override void Initialize(Image<RgbaHalf> _) => this.Clear();

    protected override void SetIdentity()
        => this.IsIdentity = this.Algorithm == WhiteBalanceAlgorithm.None; 

    public override Frame? Reset()
    {
        this.Clear();
        return base.Reset();
    }

    public override void PerformStep(PostProcessParameters ppp)
    {
        switch (ppp.WhiteBalanceAlgorithm)
        {
            default:
            case WhiteBalanceAlgorithm.None:
                break;

            case WhiteBalanceAlgorithm.ColorMatrix:
                this.ColorMatrixWhiteBalance(ppp.WhiteBalanceTemperature);
                break;

            case WhiteBalanceAlgorithm.FilteredGrayWorldAWB:
                this.FilteredGrayWorldAWB(ppp.WhiteBalanceSaturationThreshold);
                break;

            case WhiteBalanceAlgorithm.WhitePatch:
                this.WhitePatchWhiteBalance(ppp.WhiteBalanceRed, ppp.WhiteBalanceGreen, ppp.WhiteBalanceBlue);
                break;
        }
    }

    internal override Frame? Transform(bool withFrame = true)
    {
        if (this.SourceImage is null)
        {
            return null;
        }

        Image<RgbaHalf> clone;
        if (this.Algorithm == WhiteBalanceAlgorithm.None)
        {
            clone = this.SourceImage;
        }
        else
        {
            clone = this.SourceImage.Clone();
            switch (this.Algorithm)
            {
                case WhiteBalanceAlgorithm.ColorMatrix:
                    clone.ApplyColorTemperature(this.Temperature);
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
        }

        PostProcessStep.RecalculateHistograms(clone);
        this.ResultImage = clone;
        return withFrame ? clone.ToFrame() : null;
    }

    internal Frame? ColorMatrixWhiteBalance(float temperature)
    {
        this.Algorithm = WhiteBalanceAlgorithm.ColorMatrix;
        this.Temperature = temperature;
        this.SetIdentity(); 
        return this.Transform(withFrame: true);
    }

    internal Frame? FilteredGrayWorldAWB(float saturationThreshold)
    {
        this.Algorithm = WhiteBalanceAlgorithm.FilteredGrayWorldAWB;
        this.SaturationThreshold = saturationThreshold;
        this.SetIdentity();
        return this.Transform(withFrame: true);
    }

    internal Frame? WhitePatchWhiteBalance(float r, float g, float b)
    {
        this.Algorithm = WhiteBalanceAlgorithm.WhitePatch;
        this.Red = r;
        this.Green = g;
        this.Blue = b;
        this.SetIdentity();
        return this.Transform(withFrame: true);
    }

    private void Clear()
    {
        this.Algorithm = WhiteBalanceAlgorithm.None;

        // Clear all properties so that the UI sliders are also reset to default on Reset 
        this.Temperature = 0.0f;
        this.SaturationThreshold = 0.4f;
        this.Red = 0.0f;
        this.Green = 0.0f;
        this.Blue = 0.0f;
    }
}
