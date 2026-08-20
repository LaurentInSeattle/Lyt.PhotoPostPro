namespace Lyt.PhotoPostPro.Model.PostProcessors;

using System.Security.Cryptography;

public sealed class ContrastStep(PostProcessWorkflow postProcessWorkflow) :
    PostProcessStep(postProcessWorkflow, PostProcessStep.ContrastStepName)
{
    public const float SCurveDefault = 4.5f;

    public enum ContrastAlgorithm
    {
        Global,
        SCurves,
    }

    public ContrastAlgorithm Algorithm { get; set; }

    public float ContrastAmount { get; set; }

    public float BlurAmount { get; set; }

    public float BrightnessAmount { get; set; }

    public float RedAmount { get; set; }

    public float GreenAmount { get; set; }

    public float BlueAmount { get; set; }

    public override void Initialize(Image<RgbaHalf> _) => this.Clear();

    protected override void SetIdentity()
    {
        if (this.Algorithm == ContrastAlgorithm.Global)
        {
            base.IsIdentity =
                MathF.Abs(1.0f - this.ContrastAmount) < 0.001f &&
                MathF.Abs(this.BlurAmount) < 0.001f &&
                MathF.Abs(this.BrightnessAmount) < 0.001f;
        }
        else // if (Algorithm != ContrastAlgorithm.SCurves)
        {
            base.IsIdentity =
                MathF.Abs(SCurveDefault - this.RedAmount) < 0.001f &&
                MathF.Abs(SCurveDefault - this.GreenAmount) < 0.001f &&
                MathF.Abs(SCurveDefault - this.BlueAmount) < 0.001f;
        }
    }

    public override Frame? Reset()
    {
        this.Clear();
        return base.Reset();
    }

    public override void PerformStep(PostProcessParameters ppp)
    {
        switch (ppp.ContrastAlgorithm)
        {
            default:
                break;

            case ContrastAlgorithm.Global:
                this.GlobalContrast(
                    ppp.ContrastContrastAmount, ppp.ContrastBlurAmount, ppp.ContrastBrightnessAmount, withFrame: false);
                break;

            case ContrastAlgorithm.SCurves:
                this.SCurvesContrast(
                    ppp.ContrastRedAmount, ppp.ContrastGreenAmount, ppp.ContrastBlueAmount, withFrame: false);
                break;
        }
    }

    internal override Frame? Transform(bool withFrame = true)
        => base.DoTransform((clone) =>
        {
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
        }, withFrame);

    internal Frame? GlobalContrast(float contrastAmount, float blurAmount, float brightnessAmount, bool withFrame = true)
    {
        this.Algorithm = ContrastAlgorithm.Global;
        this.ContrastAmount = contrastAmount;
        this.BlurAmount = blurAmount;
        this.BrightnessAmount = brightnessAmount;
        this.SetIdentity();
        return this.Transform(withFrame);
    }

    internal Frame? SCurvesContrast(float redAmount, float greenAmount, float blueAmount, bool withFrame = true)
    {
        this.Algorithm = ContrastAlgorithm.SCurves;
        this.RedAmount = redAmount;
        this.GreenAmount = greenAmount;
        this.BlueAmount = blueAmount;
        this.SetIdentity();
        return this.Transform(withFrame);
    }

    private void Clear()
    {
        this.Algorithm = ContrastAlgorithm.Global;
        this.ContrastAmount = 1.0f;
        this.BlurAmount = 0.0f;
        this.BrightnessAmount = 0.0f;

        // Clear all properties so that the UI sliders are also reset to zero on Reset 
        this.RedAmount = 4.5f;
        this.GreenAmount = 4.5f;
        this.BlueAmount = 4.5f;

        this.SetIdentity();
    }
}
