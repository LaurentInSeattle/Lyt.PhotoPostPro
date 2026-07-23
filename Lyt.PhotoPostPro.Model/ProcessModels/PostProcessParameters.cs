namespace Lyt.PhotoPostPro.Model.ProcessModels;

using static Lyt.PhotoPostPro.Model.PostProcessors.ColorStep;
using static Lyt.PhotoPostPro.Model.PostProcessors.ContrastStep;
using static Lyt.PhotoPostPro.Model.PostProcessors.FiltersStep;
using static Lyt.PhotoPostPro.Model.PostProcessors.SharpenStep;
using static Lyt.PhotoPostPro.Model.PostProcessors.WhiteBalanceStep;

public sealed class PostProcessParameters
{
    public PostProcessParameters(/* required for deserialization */ ) { }

    public DateTime Created { get; set; }

    public DateTime Updated { get; set; }

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

    // LUT 
    public string LutFriendlyName { get; set; } = string.Empty;

    public string LutPath { get; set; } = string.Empty;
        
    public bool LutIsEmbedded { get; set; }

    // Color 
    public ColorAlgorithm ColorAlgorithm { get; set; }

    public float ColorSaturationAmount { get; set; }

    public float ColorRedAmount { get; set; }

    public float ColorGreenAmount { get; set; }

    public float ColorBlueAmount { get; set; }

    public SharpenAlgorithm SharpenAlgorithm { get; set; }

    public float SharpenSharpenAmount { get; set; }

    public Filter FilterSelectedFilter { get; set; }

    public float FilterAmount { get; set; }

    public PostProcessParameters (PostProcessWorkflow workflow)
    {
        this.Created = DateTime.Now;
        this.Updated = DateTime.Now;
        this.Update(workflow); 
    }

    public void Update(PostProcessWorkflow workflow)
    {
        this.Updated = DateTime.Now;
        var orientationStep = workflow.Get<OrientationStep>();
        this.OrientationRotationAngle = orientationStep.RotationAngle;
        this.OrientationIsMirrored = orientationStep.IsMirrored;

        var straightenStep = workflow.Get<StraightenStep>();
        this.StraightenRotationAngle = straightenStep.RotationAngle;

        var compositionStep = workflow.Get<CompositionStep>();
        this.CompositionX = compositionStep.X;
        this.CompositionY = compositionStep.Y;
        this.CompositionDx = compositionStep.Dx;
        this.CompositionDy = compositionStep.Dy;
        this.CompositionOriginalDx = compositionStep.OriginalDx;
        this.CompositionOriginalDy = compositionStep.OriginalDy;

        var exposureStep = workflow.Get<ExposureStep>();
        this.ExposureGain = exposureStep.Gain;
        this.ExposureGamma = exposureStep.Gamma;
        this.ExposureShift = exposureStep.Shift;

        var recoveryStep = workflow.Get<RecoveryStep>();
        this.RecoveryHighlightAmount = recoveryStep.HighlightAmount;
        this.RecoveryShadowAmount = recoveryStep.ShadowAmount;

        var vignetteStep = workflow.Get<VignetteStep>();
        this.VignetteTop = vignetteStep.Top;
        this.VignetteBottom = vignetteStep.Bottom;
        this.VignetteLeft = vignetteStep.Left;
        this.VignetteRight = vignetteStep.Right;
        this.VignetteLightness = vignetteStep.Lightness;

        var whiteBalanceStep = workflow.Get<WhiteBalanceStep>();
        this.WhiteBalanceAlgorithm = whiteBalanceStep.Algorithm;
        this.WhiteBalanceSaturationThreshold = whiteBalanceStep.SaturationThreshold;
        this.WhiteBalanceKelvin = whiteBalanceStep.Kelvin;
        this.WhiteBalanceTemperature = whiteBalanceStep.Temperature;
        this.WhiteBalanceRed = whiteBalanceStep.Red;
        this.WhiteBalanceGreen = whiteBalanceStep.Green;
        this.WhiteBalanceBlue = whiteBalanceStep.Blue;

        var contrastStep = workflow.Get<ContrastStep>();
        this.ContrastAlgorithm = contrastStep.Algorithm;
        this.ContrastContrastAmount = contrastStep.ContrastAmount;
        this.ContrastBlurAmount = contrastStep.BlurAmount;
        this.ContrastBrightnessAmount = contrastStep.BrightnessAmount;
        this.ContrastRedAmount = contrastStep.RedAmount;
        this.ContrastGreenAmount = contrastStep.GreenAmount;
        this.ContrastBlueAmount = contrastStep.BlueAmount;

        var lutStep = workflow.Get<LutStep>();
        this.LutFriendlyName = lutStep.LutMetadata.FriendlyName;
        this.LutPath = lutStep.LutMetadata.Path;
        this.LutIsEmbedded = lutStep.LutMetadata.IsEmbedded;

        var colorStep = workflow.Get<ColorStep>();
        this.ColorAlgorithm = colorStep.Algorithm;
        this.ColorRedAmount = colorStep.RedAmount;
        this.ColorGreenAmount = colorStep.GreenAmount;
        this.ColorBlueAmount = colorStep.BlueAmount;
        this.ColorSaturationAmount = colorStep.SaturationAmount;

        var sharpenStep = workflow.Get<SharpenStep>();
        this.SharpenAlgorithm = sharpenStep.Algorithm;
        this.SharpenSharpenAmount = sharpenStep.SharpenAmount;

        var filtersStep = workflow.Get<FiltersStep>();
        this.FilterSelectedFilter = filtersStep.SelectedFilter;
        this.FilterAmount = filtersStep.Amount;
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

        var lutStep = workflow.Get<LutStep>();
        LutMetadata lutMetadata = new(this.LutFriendlyName, this.LutPath, LutFormat.Unknown, this.LutIsEmbedded);
        lutStep.LutMetadata = lutMetadata;

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
