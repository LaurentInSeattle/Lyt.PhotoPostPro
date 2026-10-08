namespace Lyt.PhotoPostPro.Model.ProcessSteps;

public sealed class TouchUpStep(ProcessWorkflow processWorkflow) :
    ProcessStep(processWorkflow, ProcessStep.TouchUpStepName)
{
    [JsonConverter(typeof(JsonStringEnumConverter<TouchUpAlgorithm>))]
    public enum TouchUpAlgorithm
    {
        None,
        Patch,
    }

    public TouchUpAlgorithm Algorithm { get; set; }

    public int SpotPixelX { get; set; }

    public int SpotPixelY { get; set; }

    public int CleanPixelX { get; set; }

    public int CleanPixelY { get; set; }

    public int Radius { get; set; }

    internal override void Initialize(Image<RgbaHalf> _) => this.Clear();

    protected override void SetIdentity()
        => this.IsIdentity = this.Algorithm == TouchUpAlgorithm.None;

    internal override Frame? Reset()
    {
        this.Clear();
        return base.Reset();
    }

    internal override void PerformStep(ProcessParameters ppp)
    {
        switch (ppp.TouchUpAlgorithm)
        {
            default:
            case TouchUpAlgorithm.None:
                break;

            case TouchUpAlgorithm.Patch:
                this.SpotRemoval(
                    ppp.TouchUpSpotPixelX, ppp.TouchUpSpotPixelY,
                    ppp.TouchUpCleanPixelX, ppp.TouchUpCleanPixelY,
                    ppp.TouchUpRadius,
                    withFrame: false);
                break;
        }
    }

    internal override Frame? Transform(bool withFrame = true)
        => base.DoTransform((clone) =>
        {
            switch (this.Algorithm)
            {
                case TouchUpAlgorithm.Patch:
                    clone.SpotRemoval(
                        new Point(this.SpotPixelX, this.SpotPixelY),
                        new Point(this.CleanPixelX, this.CleanPixelY),
                        this.Radius);
                    break;

                default:
                    throw new NotImplementedException("No such Touch Up algorithm");
            }
        }, withFrame);

    internal Frame? SpotRemoval(
        int spotPixelX, int spotPixelY, int cleanPixelX, int cleanPixelY, int radius, bool withFrame = true)
    {
        this.Algorithm = TouchUpAlgorithm.Patch;
        this.SpotPixelX = spotPixelX;
        this.SpotPixelY = spotPixelY;
        this.CleanPixelX = cleanPixelX;
        this.CleanPixelY = cleanPixelY;
        this.Radius = radius;
        this.SetIdentity();
        return this.Transform(withFrame);
    }

    private void Clear()
    {
        this.Algorithm = TouchUpAlgorithm.None;

        // Clear all properties so that the UI sliders are also reset to default on Reset 
        this.SpotPixelX = 0;
        this.SpotPixelY = 0;
        this.CleanPixelX = 0;
        this.CleanPixelY = 0;
        this.Radius = 0;

        this.SetIdentity();
    }

}
