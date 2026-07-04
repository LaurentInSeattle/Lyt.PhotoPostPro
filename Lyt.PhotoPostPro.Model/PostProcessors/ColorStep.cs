namespace Lyt.PhotoPostPro.Model.PostProcessors;

public sealed class ColorStep(PostProcessWorkflow postProcessWorkflow) :
    PostProcessStep(postProcessWorkflow, PostProcessStep.ColorStepName)
{
    public enum ColorAlgorithm
    {
        Saturation,
        Vibrance,
    }

    public float SaturationAmount { get; set; }

    public float RedAmount { get; set; }

    public float GreenAmount { get; set; }

    public float BlueAmount { get; set; }

    public ColorAlgorithm Algorithm { get; set; }

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
            case ColorAlgorithm.Saturation:
                clone.ApplyGlobalSaturation(this.SaturationAmount);
                break;

            case ColorAlgorithm.Vibrance:
                clone.Vibrance(this.RedAmount, this.GreenAmount, this.BlueAmount);
                break;

            default:
                throw new NotImplementedException("No such Color algorithm");
        }

        PostProcessStep.RecalculateHistograms(clone);
        this.ResultImage = isChanged ? clone : this.SourceImage;
        return withFrame ? clone.ToFrame() : null;
    }

    internal Frame? Saturation(float saturationAmount)
    {
        this.Algorithm = ColorAlgorithm.Saturation;
        this.SaturationAmount = saturationAmount;
        return this.Transform(withFrame: true);
    }

    internal Frame? Vibrance(float redAmount, float greenAmount, float blueAmount)
    {
        this.Algorithm = ColorAlgorithm.Vibrance;
        this.RedAmount = redAmount;
        this.GreenAmount = greenAmount;
        this.BlueAmount = blueAmount;
        return this.Transform(withFrame: true);
    }

    private void Clear()
    {
        this.Algorithm = ColorAlgorithm.Saturation;
        this.SaturationAmount = 1.0f;

        // Clear all properties so that the UI sliders are also reset to zero on Reset 
        this.RedAmount = 0.0f;
        this.GreenAmount = 0.0f;
        this.BlueAmount = 0.0f;
    }
}

