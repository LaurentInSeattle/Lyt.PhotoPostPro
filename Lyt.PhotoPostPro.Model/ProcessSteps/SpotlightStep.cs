namespace Lyt.PhotoPostPro.Model.ProcessSteps;

public class SpotlightStep(ProcessWorkflow processWorkflow) :
    ProcessStep(processWorkflow, ProcessStep.SpotlightStepName)
{
    public int CenterX { get; set; }

    public int CenterY { get; set; }

    public int AxisX { get; set; }

    public int AxisY { get; set; }

    public float RotationAngle { get; set; }

    public float Lightness { get; set; }

    internal override void Initialize(Image<RgbaHalf> _) => this.Clear();

    protected override void SetIdentity()
        => base.IsIdentity = Math.Abs(this.Lightness) < 0.001;

    internal override Frame? Reset()
    {
        this.Clear();
        return base.Reset();
    }

    internal override void PerformStep(ProcessParameters ppp) 
        => this.Spotlight(
                ppp.SpotlightCenterX, ppp.SpotlightCenterY,
                ppp.SpotlightAxisX, ppp.SpotlightAxisY,
                ppp.SpotlightRotationAngle,  ppp.SpotlightLightness,
                withFrame: false);

    internal override Frame? Transform(bool withFrame = true)
        => base.DoTransform((clone) =>
        {
            clone.Spotlight(this.CenterX, this.CenterY, this.AxisX, this.AxisY, this.RotationAngle, this.Lightness);
        }, withFrame);

    internal Frame? Spotlight(
        int centerX, int centerY, int axisX, int axisY, float rotationAngle, float lightness, bool withFrame = true)
    {
        this.CenterX = centerX;
        this.CenterY = centerY;
        this.AxisX = axisX;
        this.AxisY = axisY;
        this.RotationAngle = rotationAngle;
        this.Lightness = lightness;
        this.SetIdentity();
        return this.Transform(withFrame);
    }

    private void Clear()
    {
        this.CenterX = 0;
        this.CenterY = 0;
        this.AxisX = 0;
        this.AxisY = 0;
        this.RotationAngle = 0.0f;
        this.Lightness = 0.0f;
        this.SetIdentity();
    }
}