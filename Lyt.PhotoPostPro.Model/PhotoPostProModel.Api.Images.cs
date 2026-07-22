namespace Lyt.PhotoPostPro.Model;

using Lyt.PhotoPostPro.Model.ExportModels;

public sealed partial class PhotoPostProModel : ModelBase
{
    public bool Rotate(bool isClockwise) =>
        this.ApiAction(() =>
        {
            if (this.Workflow.CurrentStep is OrientationStep orientationStep)
            {
                this.LastResultFrame = orientationStep.Rotate(isClockwise);
                return true;
            }

            return false;
        });

    public bool Flip(bool isMirror) =>
        this.ApiAction(() =>
        {
            if (this.Workflow.CurrentStep is OrientationStep orientationStep)
            {
                this.LastResultFrame = orientationStep.Flip(isMirror);
                return true;
            }

            return false;
        });

    public bool Rotate(bool isClockwise, float angle) =>
        this.ApiAction(() =>
        {
            if ((angle < -45.0) || (angle > 45.0))
            {
                return false;
            }

            if (this.Workflow.CurrentStep is StraightenStep straightenStep)
            {
                this.LastResultFrame = straightenStep.Rotate(isClockwise, angle);
                return true;
            }

            return false;
        });

    public bool Crop(int x, int y, int dx, int dy) =>
        this.ApiAction(() =>
        {
            if ((x < 0) || (y < 0) || (dx <= 0) || (dy <= 0))
            {
                return false;
            }

            var cropRectangle = new Rectangle(x, y, dx, dy);
            if (this.Workflow.CurrentStep is CompositionStep compositionStep)
            {
                var image = compositionStep.SourceImage;
                if (image is null)
                {
                    return false;
                }

                var imageRectangle = new Rectangle(0, 0, image.Width, image.Height);
                if (!imageRectangle.Contains(cropRectangle))
                {
                    return false;
                }

                this.LastResultFrame = compositionStep.Crop(x, y, dx, dy);
                return true;
            }

            return false;
        });

    public void AdjustExposure(float gamma, float gain, float shift) =>
        this.ApiAction(() =>
        {
            if (this.Workflow.CurrentStep is ExposureStep exposureStep)
            {
                this.LastResultFrame = exposureStep.AdjustExposure(gamma, gain, shift);
                return true;
            }

            return false;
        });

    public void HighlightsShadows(float highlights, float shadows) =>
        this.ApiAction(() =>
        {
            // All in -1 , +1 range 
            if ((highlights < -1.0) || (highlights > 1.0))
            {
                return false;
            }

            if ((shadows < -1.0) || (shadows > 1.0))
            {
                return false;
            }

            if (this.Workflow.CurrentStep is RecoveryStep recoveryStep)
            {
                this.LastResultFrame = recoveryStep.HighlightsShadows(highlights, shadows);
                return true;
            }

            return false;
        });

    public void FilteredGrayWorldAWB(float saturationThreshold) =>
        this.ApiAction(() =>
        {
            if ((saturationThreshold < 0.0) || (saturationThreshold > 100.0))
            {
                return false;
            }

            if (this.Workflow.CurrentStep is WhiteBalanceStep whiteBalanceStep)
            {
                this.LastResultFrame = whiteBalanceStep.FilteredGrayWorldAWB(saturationThreshold);
                return true;
            }

            return false;
        });

    public void TannerHellandWhiteBalance(float kelvin) =>
        this.ApiAction(() =>
        {
            if ((kelvin < 1000.0) || (kelvin > 40000.0))
            {
                return false;
            }

            if (this.Workflow.CurrentStep is WhiteBalanceStep whiteBalanceStep)
            {
                this.LastResultFrame = whiteBalanceStep.TannerHellandWhiteBalance(kelvin);
                return true;
            }

            return false;
        });

    public void ColorMatrixWhiteBalance(float temperature) =>
        this.ApiAction(() =>
        {
            if ((temperature < -100.0) || (temperature > 100.0))
            {
                return false;
            }

            if (this.Workflow.CurrentStep is WhiteBalanceStep whiteBalanceStep)
            {
                this.LastResultFrame = whiteBalanceStep.ColorMatrixWhiteBalance(temperature);
                return true;
            }

            return false;
        });

    public void WhitePatchWhiteBalance(float r, float g, float b) =>
        this.ApiAction(() =>
        {
            if (this.Workflow.CurrentStep is WhiteBalanceStep whiteBalanceStep)
            {
                this.LastResultFrame = whiteBalanceStep.WhitePatchWhiteBalance(r, g, b);
                return true;
            }

            return false;
        });


    public void GlobalContrast(float contrastAmount, float blurAmount, float brightnessAmount) =>
        this.ApiAction(() =>
        {
            // contrastAmount == from 1.0 to 2.5  -- 1.0 -> No Change 
            if ((contrastAmount < 1.0) || (contrastAmount > 3.0))
            {
                return false;
            }

            // blurAmount == sigma from 0.0 to 1.5 - 0.0 -> No blur 
            if ((blurAmount < 0.0) || (blurAmount > 1.5))
            {
                return false;
            }

            // brightnessAmount == from 0.0 to 0.5 - 0.0 -> No change 
            if ((brightnessAmount < 0.0) || (brightnessAmount > 0.5))
            {
                return false;
            }

            if (this.Workflow.CurrentStep is ContrastStep contrastStep)
            {
                this.LastResultFrame = contrastStep.GlobalContrast(contrastAmount, blurAmount, brightnessAmount);
                return true;
            }

            return false;
        });

    public void SCurvesContrast(float redAmount, float greenAmount, float blueAmount) =>
        this.ApiAction(() =>
        {
            if ((redAmount < 2.5) || (redAmount > 7.0))
            {
                return false;
            }

            if ((greenAmount < 2.5) || (greenAmount > 7.0))
            {
                return false;
            }

            if ((blueAmount < 2.5) || (blueAmount > 7.0))
            {
                return false;
            }

            if (this.Workflow.CurrentStep is ContrastStep contrastStep)
            {
                this.LastResultFrame = contrastStep.SCurvesContrast(redAmount, greenAmount, blueAmount);
                return true;
            }
            return false;
        });


    public void GlobalSaturation(float saturationAmount) =>
        this.ApiAction(() =>
        {
            // saturationAmount == from 0.0 to 2.5  -- 1.0 -> No Change 
            if ((saturationAmount < 0.0) || (saturationAmount > 2.5))
            {
                return false;
            }

            if (this.Workflow.CurrentStep is ColorStep colorStep)
            {
                this.LastResultFrame = colorStep.Saturation(saturationAmount);
                return true;
            }

            return false;
        });

    public void Vibrance(float redAmount, float greenAmount, float blueAmount) =>
        this.ApiAction(() =>
        {
            // All in -1 , +1 range 
            if ((redAmount < -1.0) || (redAmount > 1.0))
            {
                return false;
            }

            if ((greenAmount < -1.0) || (greenAmount > 1.0))
            {
                return false;
            }

            if ((blueAmount < -1.0) || (blueAmount > 1.0))
            {
                return false;
            }

            if (this.Workflow.CurrentStep is ColorStep colorStep)
            {
                this.LastResultFrame = colorStep.Vibrance(redAmount, greenAmount, blueAmount);
                return true;
            }

            return false;
        });

    public void Lut(LutMetadata lutMetadata) =>
        this.ApiAction(() =>
        {
            if (this.Workflow.CurrentStep is LutStep lutStep)
            {
                this.LastResultFrame = lutStep.Lut(lutMetadata);
                return true;
            }

            return false;
        });

    public void GlobalSharpen(float sharpenAmount) =>
        this.ApiAction(() =>
        {
            // blurAmount == sigma from 0.0 to 4.0 - 0.0 -> No sharpening 
            if ((sharpenAmount < 0.0) || (sharpenAmount > 4.0))
            {
                return false;
            }

            if (this.Workflow.CurrentStep is SharpenStep sharpenStep)
            {
                this.LastResultFrame = sharpenStep.Sharpen(sharpenAmount);
                return true;
            }

            return false;
        });

    public void Vignette(float top, float bottom, float left, float right, float lightness) =>
        this.ApiAction(() =>
        {
            if ((top < 0.0f) || (top > 0.45f))
            {
                return false;
            }

            if ((bottom < 0.0f) || (bottom > 0.45f))
            {
                return false;
            }

            if ((left < 0.0f) || (left > 0.45f))
            {
                return false;
            }

            if ((right < 0.0f) || (right > 0.45f))
            {
                return false;
            }

            if ((lightness < -0.75f) || (lightness > 0.75f))
            {
                return false;
            }

            if (this.Workflow.CurrentStep is VignetteStep vignetteStep)
            {
                this.LastResultFrame = vignetteStep.Vignette(top, bottom, left, right, lightness);
                return true;
            }

            return false;
        });

    public void Grayscale(float grayscaleAmount) =>
        this.ApiAction(() =>
        {
            // grayscaleAmount == from 0.0 to 1.0  -- 0.0 -> No Change 
            if ((grayscaleAmount < 0.0) || (grayscaleAmount > 1.0))
            {
                return false;
            }
            
            if (this.Workflow.CurrentStep is FiltersStep filtersStep)
            {
                this.LastResultFrame = filtersStep.Grayscale(grayscaleAmount);
                return true;
            }

            return false;
        });

    public void Sepia(float sepiaAmount) =>
        this.ApiAction(() =>
        {
            // sepiaAmount == from 0.0 to 1.0  -- 0.0 -> No Change 
            if ((sepiaAmount < 0.0) || (sepiaAmount > 1.0))
            {
                return false;
            }

            if (this.Workflow.CurrentStep is FiltersStep filtersStep)
            {
                this.LastResultFrame = filtersStep.Sepia(sepiaAmount);
                return true;
            }

            return false;
        });

    public void Vignette() =>
        this.ApiAction(() =>
        {
            if (this.Workflow.CurrentStep is FiltersStep filtersStep)
            {
                this.LastResultFrame = filtersStep.Vignette();
                return true;
            }

            return false;
        });

    public void BlackWhite() =>
        this.ApiAction(() =>
        {
            if (this.Workflow.CurrentStep is FiltersStep filtersStep)
            {
                this.LastResultFrame = filtersStep.BlackWhite();
                return true;
            }

            return false;
        });

    public void Kodachrome() =>
        this.ApiAction(() =>
        {
            if (this.Workflow.CurrentStep is FiltersStep filtersStep)
            {
                this.LastResultFrame = filtersStep.Kodachrome();
                return true;
            }

            return false;
        });

    public void Lomograph() =>
        this.ApiAction(() =>
        {
            if (this.Workflow.CurrentStep is FiltersStep filtersStep)
            {
                this.LastResultFrame = filtersStep.Lomograph();
                return true;
            }

            return false;
        });

    public void Polaroid() =>
        this.ApiAction(() =>
        {
            if (this.Workflow.CurrentStep is FiltersStep filtersStep)
            {
                this.LastResultFrame = filtersStep.Polaroid();
                return true;
            }

            return false;
        });

    public void Export(ExportParameters exportParameters) =>
        this.ApiAction(() =>
        {
            if (this.Workflow.CurrentStep is ExportStep exportStep)
            {
                this.LastResultFrame = exportStep.Export(exportParameters);
                return true;
            }

            return false;
        });
}
