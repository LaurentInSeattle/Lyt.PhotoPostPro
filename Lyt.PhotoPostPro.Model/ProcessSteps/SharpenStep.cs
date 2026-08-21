namespace Lyt.PhotoPostPro.Model.ProcessSteps;

public sealed class SharpenStep(ProcessWorkflow processWorkflow) :
    ProcessStep(processWorkflow, ProcessStep.SharpenStepName)
{
    public enum SharpenAlgorithm
    {
        Sharpen,
        EdgesMask,
    }

    public float SharpenAmount { get; set; }

    public SharpenAlgorithm Algorithm { get; set; }

    public override void Initialize(Image<RgbaHalf> _) => this.Clear();

    protected override void SetIdentity()
    {
        if (this.Algorithm == SharpenAlgorithm.Sharpen)
        {
            base.IsIdentity = MathF.Abs(this.SharpenAmount) < 0.001f;
        }
        else // if (Algorithm != SharpenAlgorithm.EdgesMask)
        {
            base.IsIdentity = true;
        }
    }

    public override Frame? Reset()
    {
        this.Clear();
        return base.Reset();
    }

    public override void PerformStep(ProcessParameters ppp)
        // Ignore Edge Mask for now 
        => this.Sharpen(ppp.SharpenSharpenAmount, withFrame: false);

    internal override Frame? Transform(bool withFrame = true)
        => base.DoTransform((clone) =>
        {
            switch (this.Algorithm)
            {
                case SharpenAlgorithm.Sharpen:
                    clone.ApplyGlobalSharpen(this.SharpenAmount);
                    break;

                case SharpenAlgorithm.EdgesMask:
                    // clone.ApplySCurveContrast(this.RedAmount, this.GreenAmount, this.BlueAmount);
                    break;

                default:
                    throw new NotImplementedException("No such Color algorithm");
            }
        }, withFrame);

    internal Frame? Sharpen(float sharpenAmount, bool withFrame = true)
    {
        this.Algorithm = SharpenAlgorithm.Sharpen;
        this.SharpenAmount = sharpenAmount;
        this.SetIdentity();
        return this.Transform(withFrame);
    }

    internal Frame? EdgesMask(bool withFrame = true)
    {
        this.Algorithm = SharpenAlgorithm.EdgesMask;
        this.SetIdentity();
        return this.Transform(withFrame);
    }

    private void Clear()
    {
        this.Algorithm = SharpenAlgorithm.Sharpen;
        this.SharpenAmount = 0.0f;
        this.SetIdentity();
    }
}