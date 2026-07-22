namespace Lyt.PhotoPostPro.Model.ProcessModels;

using static Lyt.PhotoPostPro.Model.PostProcessors.ColorStep;
using static Lyt.PhotoPostPro.Model.PostProcessors.ContrastStep;
using static Lyt.PhotoPostPro.Model.PostProcessors.FiltersStep;
using static Lyt.PhotoPostPro.Model.PostProcessors.SharpenStep;
using static Lyt.PhotoPostPro.Model.PostProcessors.WhiteBalanceStep;

public sealed class PostProcessParameters
{
    public int OrientationRotationAngle { get; set; } // Degrees

    public bool OrientationIsMirrored { get; set; }

    public float StraightenRotationAngle { get; set; } // Degrees

    public int CompositionX { get; set; }

    public int CompositionY { get; set; }

    public int CompositionDx { get; set; }

    public int CompositionDy { get; set; }

    public int CompositionOriginalDx { get; set; }

    public int CompositionOriginalDy { get; set; }

    public float ExposureGamma { get; set; }

    public float ExposureGain { get; set; }

    public float ExposureShift { get; set; }

    public float RecoveryShadowAmount { get; set; }

    public float RecoveryHighlightAmount { get; set; }

    public float VignetteTop { get; set; }

    public float VignetteBottom { get; set; }

    public float VignetteLeft { get; set; }

    public float VignetteRight { get; set; }

    public float VignetteLightness { get; set; }

    public WhiteBalanceAlgorithm WhiteBalanceAlgorithm { get; set; }

    public float WhiteBalanceSaturationThreshold { get; set; }

    public float WhiteBalanceTemperature { get; set; }

    public float WhiteBalanceKelvin { get; set; }

    public float WhiteBalanceRed { get; set; }

    public float WhiteBalanceGreen { get; set; }

    public float WhiteBalanceBlue { get; set; }

    public ContrastAlgorithm ContrastAlgorithm { get; set; }

    public float ContrastContrastAmount { get; set; }

    public float ContrastBlurAmount { get; set; }

    public float ContrastBrightnessAmount { get; set; }

    public float ContrastRedAmount { get; set; }

    public float ContrastGreenAmount { get; set; }

    public float ContrastBlueAmount { get; set; }

    // LUT ??? 

    public ColorAlgorithm ColorAlgorithm { get; set; }

    public float ColorSaturationAmount { get; set; }

    public float ColorRedAmount { get; set; }

    public float ColorGreenAmount { get; set; }

    public float ColorBlueAmount { get; set; }

    public SharpenAlgorithm SharpenAlgorithm { get; set; }

    public float SharpenSharpenAmount { get; set; }

    public Filter FilterSelectedFilter { get; set; }

    public float FilterAmount { get; set; }

    public static PostProcessParameters FromPostProcessWorkflow (PostProcessWorkflow workflow)
    {
        var p = new PostProcessParameters ();
        var orientationStep = workflow.Get<OrientationStep>();
        p.OrientationRotationAngle = orientationStep.RotationAngle;
        p.OrientationIsMirrored = orientationStep.IsMirrored;

        var straightenStep = workflow.Get<StraightenStep> ();
        p.StraightenRotationAngle = straightenStep.RotationAngle;

        var compositionStep = workflow.Get<CompositionStep> ();
        p.CompositionX = compositionStep.X;
        p.CompositionY = compositionStep.Y;
        p.CompositionDx = compositionStep.Dx;
        p.CompositionDy = compositionStep.Dy;
        p.CompositionOriginalDx = compositionStep.OriginalDx;
        p.CompositionOriginalDy = compositionStep.OriginalDy;

        var exposureStep = workflow.Get<ExposureStep> ();
        p.ExposureGain = exposureStep.Gain;
        p.ExposureGamma = exposureStep.Gamma;
        p.ExposureShift = exposureStep.Shift;

        var recoveryStep = workflow.Get<RecoveryStep> ();
        p.RecoveryHighlightAmount = recoveryStep.HighlightAmount;
        p.RecoveryShadowAmount = recoveryStep.ShadowAmount;

        var vignetteStep = workflow.Get<VignetteStep>();
        p.VignetteTop = vignetteStep.Top;
        p.VignetteBottom = vignetteStep.Bottom;
        p.VignetteLeft = vignetteStep.Left;
        p.VignetteRight = vignetteStep.Right;
        p.VignetteLightness = vignetteStep.Lightness;

        var whiteBalanceStep = workflow.Get<WhiteBalanceStep> ();
        p.WhiteBalanceAlgorithm = whiteBalanceStep.Algorithm;
        p.WhiteBalanceSaturationThreshold = whiteBalanceStep.SaturationThreshold;
        p.WhiteBalanceKelvin = whiteBalanceStep.Kelvin;
        p.WhiteBalanceTemperature = whiteBalanceStep.Temperature;
        p.WhiteBalanceRed = whiteBalanceStep.Red;
        p.WhiteBalanceGreen = whiteBalanceStep.Green;
        p.WhiteBalanceBlue = whiteBalanceStep.Blue;

        var contrastStep = workflow.Get<ContrastStep> ();
        p.ContrastAlgorithm = contrastStep.Algorithm;
        p.ContrastContrastAmount = contrastStep.ContrastAmount;
        p.ContrastBlurAmount = contrastStep.BlurAmount;
        p.ContrastBrightnessAmount = contrastStep.BrightnessAmount;
        p.ContrastRedAmount = contrastStep.RedAmount;
        p.ContrastGreenAmount = contrastStep.GreenAmount;
        p.ContrastBlueAmount = contrastStep.BlueAmount;

        // ?
        // var lutStep = workflow.Get<LutStep> ();

        var colorStep = workflow.Get<ColorStep> ();
        p.ColorAlgorithm = colorStep.Algorithm;
        p.ColorRedAmount = colorStep.RedAmount;
        p.ColorGreenAmount = colorStep.GreenAmount;
        p.ColorBlueAmount = colorStep.BlueAmount;
        p.ColorSaturationAmount = colorStep.SaturationAmount;

        var sharpenStep = workflow.Get<SharpenStep> ();
        p.SharpenAlgorithm = sharpenStep.Algorithm;
        p.SharpenSharpenAmount = sharpenStep.SharpenAmount;

        var filtersStep = workflow.Get<FiltersStep> ();
        p.FilterSelectedFilter = filtersStep.SelectedFilter; 
        p.FilterAmount= filtersStep.Amount;

        return p;
    }

    public void ToPostProcessWorkflow(PostProcessWorkflow workflow)
    {
        var orientationStep = workflow.Get<OrientationStep>();
        orientationStep.RotationAngle = this.OrientationRotationAngle;
        orientationStep.IsMirrored = this.OrientationIsMirrored;

        var straightenStep = workflow.Get<StraightenStep>();
        straightenStep.RotationAngle = this.StraightenRotationAngle;

        var compositionStep = workflow.Get<CompositionStep>();
        compositionStep.X = this.CompositionX;
        compositionStep.Y = this.CompositionY;
        compositionStep.Dx = this.CompositionDx;
        compositionStep.Dy = this.CompositionDy;
        compositionStep.OriginalDx = this.CompositionOriginalDx;
        compositionStep.OriginalDy = this.CompositionOriginalDy;

        var exposureStep = workflow.Get<ExposureStep>();
        exposureStep.Gain = this.ExposureGain;
        exposureStep.Gamma = this.ExposureGamma;
        exposureStep.Shift = this.ExposureShift;

        var recoveryStep = workflow.Get<RecoveryStep>();
        recoveryStep.HighlightAmount = this.RecoveryHighlightAmount;
        recoveryStep.ShadowAmount = this.RecoveryShadowAmount;

        var vignetteStep = workflow.Get<VignetteStep>();
        vignetteStep.Top = this.VignetteTop;
        vignetteStep.Bottom = this.VignetteBottom;
        vignetteStep.Left = this.VignetteLeft;
        vignetteStep.Right = this.VignetteRight;
        vignetteStep.Lightness = this.VignetteLightness;

        var whiteBalanceStep = workflow.Get<WhiteBalanceStep>();
        whiteBalanceStep.Algorithm = this.WhiteBalanceAlgorithm;
        whiteBalanceStep.SaturationThreshold = this.WhiteBalanceSaturationThreshold;
        whiteBalanceStep.Kelvin = this.WhiteBalanceKelvin;
        whiteBalanceStep.Temperature = this.WhiteBalanceTemperature;
        whiteBalanceStep.Red = this.WhiteBalanceRed;
        whiteBalanceStep.Green = this.WhiteBalanceGreen;
        whiteBalanceStep.Blue = this.WhiteBalanceBlue;

        var contrastStep = workflow.Get<ContrastStep>();
        contrastStep.Algorithm = this.ContrastAlgorithm;
        contrastStep.ContrastAmount = this.ContrastContrastAmount;
        contrastStep.BlurAmount = this.ContrastBlurAmount;
        contrastStep.BrightnessAmount = this.ContrastBrightnessAmount;
        contrastStep.RedAmount = this.ContrastRedAmount;
        contrastStep.GreenAmount = this.ContrastGreenAmount;
        contrastStep.BlueAmount = this.ContrastBlueAmount;

        // ?
        // var lutStep = workflow.Get<LutStep> ();

        var colorStep = workflow.Get<ColorStep>();
        colorStep.Algorithm = this.ColorAlgorithm;
        colorStep.RedAmount = this.ColorRedAmount;
        colorStep.GreenAmount = this.ColorGreenAmount;
        colorStep.BlueAmount = this.ColorBlueAmount;
        colorStep.SaturationAmount = this.ColorSaturationAmount;

        var sharpenStep = workflow.Get<SharpenStep>();
        sharpenStep.Algorithm = this.SharpenAlgorithm;
        sharpenStep.SharpenAmount = this.SharpenSharpenAmount;

        var filtersStep = workflow.Get<FiltersStep>();
        filtersStep.SelectedFilter = this.FilterSelectedFilter;
        filtersStep.Amount = this.FilterAmount;
    }
}
