namespace Lyt.PhotoPostPro.Model.PostProcessors;

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
            case SharpenAlgorithm.Sharpen:
                clone.ApplyGlobalSharpen(this.SharpenAmount);
                break;

            case SharpenAlgorithm.EdgesMask:
                // clone.ApplySCurveContrast(this.RedAmount, this.GreenAmount, this.BlueAmount);
                break;

            default:
                throw new NotImplementedException("No such Color algorithm");
        }

        PostProcessStep.RecalculateHistograms(clone);
        this.ResultImage = isChanged ? clone : this.SourceImage;
        return withFrame ? clone.ToFrame() : null;
    }

    internal Frame? Sharpen(float sharpenAmount)
    {
        this.Algorithm = SharpenAlgorithm.Sharpen;
        this.SharpenAmount = sharpenAmount;
        return this.Transform(withFrame: true);
    }

    internal Frame? EdgesMask()
    {
        this.Algorithm = SharpenAlgorithm.EdgesMask;
        return this.Transform(withFrame: true);
    }

    private void Clear()
    {
        this.Algorithm = SharpenAlgorithm.Sharpen;
        this.SharpenAmount = 1.0f;
    }
}

