namespace Lyt.PhotoPostPro.Model.PostProcessors;

using static Lyt.PhotoPostPro.Model.PostProcessors.ContrastStep;

public sealed class SharpenStep(PostProcessWorkflow postProcessWorkflow) :
    PostProcessStep(postProcessWorkflow, PostProcessStep.SharpenStepName)
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

    public override void PerformStep(PostProcessParameters ppp)
    {
        // Ignore Edge Mask for now 
        float amount = ppp.SharpenSharpenAmount;
        if (amount > 0.000_1f)
        {
            this.Sharpen(amount);
        }
    }

    internal override Frame? Transform(bool withFrame = true)
    {
        if (this.SourceImage is null)
        {
            return null;
        }

        if (this.IsIdentity)
        {
            this.ResultImage = this.SourceImage;
        }
        else
        {
            var clone = this.SourceImage.Clone();
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

            this.ResultImage = clone;
        }

        PostProcessStep.RecalculateHistograms(this.ResultImage);
        return withFrame ? this.ResultImage.ToFrame() : null;
    }

    internal Frame? Sharpen(float sharpenAmount)
    {
        this.Algorithm = SharpenAlgorithm.Sharpen;
        this.SharpenAmount = sharpenAmount;
        this.SetIdentity();
        return this.Transform(withFrame: true);
    }

    internal Frame? EdgesMask()
    {
        this.Algorithm = SharpenAlgorithm.EdgesMask;
        this.SetIdentity();
        return this.Transform(withFrame: true);
    }

    private void Clear()
    {
        this.Algorithm = SharpenAlgorithm.Sharpen;
        this.SharpenAmount = 0.0f;
        this.SetIdentity();
    }
}