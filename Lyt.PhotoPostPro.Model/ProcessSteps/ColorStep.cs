namespace Lyt.PhotoPostPro.Model.ProcessSteps;

public sealed class ColorStep(ProcessWorkflow processWorkflow) :
    ProcessStep(processWorkflow, ProcessStep.ColorStepName)
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

    public override void Initialize(Image<RgbaHalf> _) => this.Clear();

    protected override void SetIdentity()
    {
        if (this.Algorithm == ColorAlgorithm.Saturation)
        {
            base.IsIdentity = MathF.Abs(1.0f - this.SaturationAmount) < 0.001f;
        }
        else // if (Algorithm != ColorAlgorithm.Vibrance)
        {
            base.IsIdentity =
                MathF.Abs(this.RedAmount) < 0.001f &&
                MathF.Abs(this.GreenAmount) < 0.001f &&
                MathF.Abs(this.BlueAmount) < 0.001f;
        }
    }

    public override Frame? Reset()
    {
        this.Clear();
        return base.Reset();
    }

    public override void PerformStep(ProcessParameters ppp)
    {
        switch (ppp.ColorAlgorithm)
        {
            default:
                break;

            case ColorAlgorithm.Saturation:
                this.Saturation(ppp.ColorSaturationAmount, withFrame: false);
                break;

            case ColorAlgorithm.Vibrance:
                this.Vibrance(ppp.ColorRedAmount, ppp.ColorGreenAmount, ppp.ColorBlueAmount, withFrame: false);
                break;
        }
    }

    internal override Frame? Transform(bool withFrame = true)
        => base.DoTransform((clone) =>
            {
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
            }, withFrame);

    internal Frame? Saturation(float saturationAmount, bool withFrame = true)
    {
        this.Algorithm = ColorAlgorithm.Saturation;
        this.SaturationAmount = saturationAmount;
        this.SetIdentity();
        return this.Transform(withFrame);
    }

    internal Frame? Vibrance(float redAmount, float greenAmount, float blueAmount, bool withFrame = true)
    {
        this.Algorithm = ColorAlgorithm.Vibrance;
        this.RedAmount = redAmount;
        this.GreenAmount = greenAmount;
        this.BlueAmount = blueAmount;
        this.SetIdentity();
        return this.Transform(withFrame);
    }

    private void Clear()
    {
        this.Algorithm = ColorAlgorithm.Saturation;
        this.SaturationAmount = 1.0f;

        // Clear all properties so that the UI sliders are also reset to zero on Reset 
        this.RedAmount = 0.0f;
        this.GreenAmount = 0.0f;
        this.BlueAmount = 0.0f;
        this.SetIdentity();
    }
}