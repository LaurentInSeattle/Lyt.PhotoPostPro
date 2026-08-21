namespace Lyt.PhotoPostPro.Model.PostProcessors;

public class VignetteStep(ProcessWorkflow processWorkflow) :
    ProcessStep(processWorkflow, ProcessStep.VignetteStepName)
{
    public float Top { get; set; }

    public float Bottom { get; set; }

    public float Left { get; set; }

    public float Right { get; set; }

    public float Lightness { get; set; }

    public override void Initialize(Image<RgbaHalf> _) => this.Clear();

    protected override void SetIdentity()
        => base.IsIdentity = Math.Abs(this.Lightness) < 0.001;

    public override Frame? Reset()
    {
        this.Clear();
        return base.Reset();
    }

    public override void PerformStep(ProcessParameters ppp)
        => this.Vignette(
                ppp.VignetteTop, ppp.VignetteBottom, 
                ppp.VignetteLeft, ppp.VignetteRight, 
                ppp.VignetteLightness, 
                withFrame:false);

    internal override Frame? Transform(bool withFrame = true)
        => base.DoTransform((clone) =>
        {
            clone.Vignette(this.Top, this.Bottom, this.Left, this.Right, this.Lightness);
        }, withFrame);

    internal Frame? Vignette(float top, float bottom, float left, float right, float lightness, bool withFrame = true)
    {
        this.Top = top;
        this.Bottom = bottom;
        this.Left = left;
        this.Right = right;
        this.Lightness = lightness;
        this.SetIdentity();
        return this.Transform(withFrame);
    }

    private void Clear()
    {
        this.Top = 0.0f;
        this.Bottom = 0.0f;
        this.Left = 0.0f;
        this.Right = 0.0f;
        this.Lightness = 0.0f;
        this.SetIdentity();
    }
}