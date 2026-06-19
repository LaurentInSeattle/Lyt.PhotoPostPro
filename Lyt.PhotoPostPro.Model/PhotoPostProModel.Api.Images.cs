namespace Lyt.PhotoPostPro.Model;

public sealed partial class PhotoPostProModel : ModelBase
{
    public void BeginPostProcess()
    {
        this.ApiAction(() =>
        {
            this.CurrentPostProcess!.Begin();
            //bool success = this.GetSourceImage();
            //if (success)
            //{
            //    // HACK BEGIN 
            //    Task.Run(() =>
            //    {
            //        // NOT Final: ONLY for testing histograms atm
            //        var sourceImage = this.CurrentPostProcess.SourceImage;
            //        Histograms histograms = new(sourceImage);
            //        new HistogramsGeneratedMessage(histograms).Publish();
            //    });
            //    // HACK END 
            //}

            return true;
        });
    }

    public bool GetProcessSourceImage()
    {
        if ((this.CurrentProject is null) || (this.CurrentProjectMetadata is null) || (this.CurrentPostProcess is null))
        {
            return false;
        }

        try
        {
            var sourceImage = this.CurrentPostProcess.OriginalImage;
            this.LastSourceFrame = sourceImage.ToFrame();
            this.IsSourceImageUpdatePending = true;
            return true;
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex);
            return false;
        }
    }

    public bool GetStepSourceImage() =>
        this.ApiAction(() =>
        {
            if (this.Workflow.CurrentStep is PostProcessStep step)
            {
                if (step.SourceImage is not null)
                {
                    this.LastSourceFrame = step.SourceImage.ToFrame();
                    return true;
                }

                return false;
            }

            return false;
        });

    public bool GetStepResultImage() =>
        this.ApiAction(() =>
        {
            if (this.Workflow.CurrentStep is PostProcessStep step)
            {
                if (step.ResultImage is not null)
                {
                    this.LastResultFrame = step.ResultImage.ToFrame();
                    return true;
                }

                return false;
            }

            return false;
        });
    public bool Back() =>
        this.ApiAction(() =>
        {
            this.Workflow.Back();
            return false;
        });

    public bool Reset() => 
        this.ApiAction(() =>
        {
            var frame = this.Workflow.Reset();
            this.LastSourceFrame = frame;
            this.LastResultFrame = frame;
            return true;
        });


    public bool Next() =>
        this.ApiAction(() =>
        {
            this.Workflow.Next();
            return false;
        });

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
            if (this.Workflow.CurrentStep is CompositionStep compositionStep)
            {
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
            if (this.Workflow.CurrentStep is WhiteBalanceStep whiteBalanceStep)
            {
                this.LastResultFrame = whiteBalanceStep.FilteredGrayWorldAWB(saturationThreshold);
                return true;
            }

            return false;
        });


    private bool ApiAction(Func<bool> action, bool notify = true)
    {
        if (!this.timeoutTimer.IsRunning)
        {
            this.timeoutTimer.Start();
        }

        if (notify)
        {
            this.timeoutTimer.ResetTimeout();
        }

        if ((this.CurrentProject is null) ||
            (this.CurrentProjectMetadata is null) ||
            (this.CurrentPostProcess is null))
        {
            string errorMessage = "No project is currently open.";
            Debug.WriteLine(errorMessage);
            return false;
        }

        if (this.Workflow is null)
        {
            string errorMessage = "Workflow is not setup.";
            Debug.WriteLine(errorMessage);
            return false;
        }

        bool success = action();
        if (success)
        {
            new ModelStepUpdatedMessage(Step: this.Workflow.CurrentStep).Publish(); 
        }

        return success;
    }
}
