namespace Lyt.PhotoPostPro.Model.PostProcessors;

public sealed class ContrastStep(PostProcessWorkflow postProcessWorkflow) : 
    PostProcessStep(postProcessWorkflow, PostProcessStep.ContrastStepName)
{
    public enum ContrastAlgorithm
    {
        Global, 
        SCurves, 
    }

    public float ContrastAmount { get; set; }

    public float BlurAmount { get; set; }

    public float BrightnessAmount { get; set; }


    public float RedAmount { get; set; }

    public float GreenAmount { get; set; }
    
    public float BlueAmount { get; set; }

    public ContrastAlgorithm Algorithm { get; set; }

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
            case ContrastAlgorithm.Global:
                clone.ApplyGlobalContrast(this.ContrastAmount, this.BlurAmount, this.BrightnessAmount);
                break;

            case ContrastAlgorithm.SCurves:
                clone.ApplySCurveContrast(this.RedAmount, this.GreenAmount, this.BlueAmount);
                break;

            default:
                throw new NotImplementedException("No such Contrast algorithm");
        }

        PostProcessStep.RecalculateHistograms(clone);
        this.ResultImage = isChanged ? clone : this.SourceImage;
        return withFrame ? clone.ToFrame() : null;
    }

    internal Frame? GlobalContrast(float contrastAmount, float blurAmount, float brightnessAmount)
    {
        this.Algorithm = ContrastAlgorithm.Global;
        this.ContrastAmount = contrastAmount;
        this.BlurAmount = blurAmount;
        this.BrightnessAmount = brightnessAmount;
        return this.Transform(withFrame: true);
    }

    internal Frame? SCurvesContrast(float redAmount, float greenAmount, float blueAmount)
    {
        this.Algorithm = ContrastAlgorithm.SCurves;
        this.RedAmount = redAmount;
        this.GreenAmount = greenAmount; 
        this.BlueAmount = blueAmount; 
        return this.Transform(withFrame: true);
    }

    private void Clear()
    {
        this.Algorithm = ContrastAlgorithm.Global;
        this.ContrastAmount = 1.0f;
        this.BlurAmount = 0.0f;
        this.BrightnessAmount = 0.0f;
        this.RedAmount = 4.5f;
        this.GreenAmount = 4.5f;
        this.BlueAmount = 4.5f;
    }
}
