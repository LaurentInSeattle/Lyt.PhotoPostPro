namespace Lyt.PhotoPostPro.Model.ProcessSteps;

public class RecoveryStep(ProcessWorkflow processWorkflow) :
    ProcessStep(processWorkflow, ProcessStep.RecoveryStepName)
{
    public float ShadowAmount { get; set; }

    public float HighlightAmount { get; set; }

    public override void Initialize(Image<RgbaHalf> _) => this.Clear();

    protected override void SetIdentity()
    {
        base.IsIdentity =
            MathF.Abs(this.ShadowAmount) < 0.001 &&
            MathF.Abs(this.HighlightAmount) < 0.001f;
    }

    public override void PerformStep(ProcessParameters ppp)
        => this.HighlightsShadows(ppp.RecoveryHighlightAmount, ppp.RecoveryShadowAmount, withFrame: false);

    public override Frame? Reset()
    {
        this.Clear();
        return base.Reset();
    }

    internal override Frame? Transform(bool withFrame = true)
        => base.DoTransform((clone) =>
        {
            clone.HighlightsShadows(this.HighlightAmount, this.ShadowAmount);
        }, withFrame);

    internal Frame? HighlightsShadows(float highlightAmount, float shadowAmount, bool withFrame = true)
    {
        this.HighlightAmount = highlightAmount;
        this.ShadowAmount = shadowAmount;
        this.SetIdentity();
        return this.Transform(withFrame);
    }

    private void Clear()
    {
        this.ShadowAmount = 0.0f;
        this.HighlightAmount = 0.0f;
        this.SetIdentity();
    }
}