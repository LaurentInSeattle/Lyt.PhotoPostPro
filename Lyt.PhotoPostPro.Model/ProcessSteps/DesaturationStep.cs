namespace Lyt.PhotoPostPro.Model.ProcessSteps;

public sealed class DesaturationStep(ProcessWorkflow processWorkflow) :
    ProcessStep(processWorkflow, ProcessStep.DesaturationStepName)
{
    public bool Identity { get; set; }

    public float TargetHue { get; set; }

    public float Tolerance { get; set; }

    public float Feather { get; set; }

    public float SaturationBase { get; set; }

    public float SaturationBoost { get; set; }

    internal override void Initialize(Image<RgbaHalf> _) => this.Clear();

    protected override void SetIdentity() => base.IsIdentity = this.Identity;

    internal override Frame? Reset()
    {
        this.Clear();
        return base.Reset();
    }

    internal override void PerformStep(ProcessParameters ppp)
    {
        if (ppp.DesaturationIdentity)
        {
            this.Clear();
        }
        else
        {
            this.SelectiveDesaturation(
                ppp.DesaturationTargetHue, 
                ppp.DesaturationTolerance, 
                ppp.DesaturationFeather, 
                ppp.DesaturationSaturationBase, 
                ppp.DesaturationSaturationBoost,
                withFrame: false);
        }
    }

    internal override Frame? Transform(bool withFrame = true)
        => base.DoTransform((clone) =>
        {
            clone.SelectiveDesaturation(
                this.TargetHue, this.Tolerance, this.Feather, this.SaturationBase, this.SaturationBoost);
        }, withFrame);

    internal Frame? SelectiveDesaturation(
        float targetHue, float tolerance, float feather, float saturationBase, float saturationBoost,
        bool withFrame = true)
    {
        this.TargetHue = targetHue;
        this.Tolerance = tolerance;
        this.Feather = feather;
        this.SaturationBase = saturationBase;
        this.SaturationBoost = saturationBoost;
        this.Identity = false; 
        this.SetIdentity();
        return this.Transform(withFrame);
    }

    private void Clear()
    {
        this.Identity = true;

        // Clear all properties so that the UI sliders are also reset to zero on Reset 
        this.TargetHue = 0.0f;
        this.Tolerance = 20.0f;
        this.Feather = 5.0f;
        this.SaturationBase = 0.0f;
        this.SaturationBoost = 1.0f;
        this.SetIdentity();
    }
}
