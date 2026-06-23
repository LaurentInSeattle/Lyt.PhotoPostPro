namespace Lyt.PhotoPostPro.Model;

using static Lyt.PhotoPostPro.Model.PostProcessors.ContrastStep;

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

    public void AdjustExposure(double gamma, double gain, int shift) =>
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

    public void GlobalContrast(float contrastAmount, float blurAmount) =>
        this.ApiAction(() =>
        {
            // contrastAmount == from 1.0 to 2.5  -- 1.0 -> No Change 
            // blurAmount == sigma from 0.0 to 1.5 - 0.0 -> No blur 
            if ((contrastAmount < 1.0) || (contrastAmount > 3.0))
            {
                return false;
            }

            if ((blurAmount < 0.0) || (blurAmount > 1.5))
            {
                return false;
            }

            if (this.Workflow.CurrentStep is ContrastStep contrastStep)
            {
                this.LastResultFrame = contrastStep.GlobalContrast(contrastAmount, blurAmount);
                return true;
            }

            return false;
        });

    public void SCurvesContrast(float redAmount, float greenAmount, float blueAmount) =>
        this.ApiAction(() =>
        {
            // TODO : Figure out the actual limits 
            if ((redAmount < 1.0) || (redAmount > 3.0))
            {
                return false;
            }

            if ((greenAmount < 1.0) || (greenAmount > 3.0))
            {
                return false;
            }

            if ((blueAmount < 0.0) || (blueAmount > 1.5))
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

    public void Export(ExportStep.ExportParameters exportParameters) =>
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
