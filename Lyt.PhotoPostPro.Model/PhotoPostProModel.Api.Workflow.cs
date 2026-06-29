namespace Lyt.PhotoPostPro.Model;

public sealed partial class PhotoPostProModel : ModelBase
{
    public void BeginPostProcess()
    {
        this.ApiAction(() =>
        {
            // CurrentPostProcess checked not null by ApiAction wrapper 
            this.CurrentPostProcess!.Begin();
            return true;
        });
    }

    public bool GetProcessOriginalImage()
    {
        // NOT an ApiAction wrapper : MUST check for nulls 
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
            return true;
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
            var frame = this.Workflow.Next();
            if ( frame is not null)
            {
                this.LastResultFrame = frame;
            }

            return true;
        });

    public void Finish() =>
        this.ApiAction(() =>
        {
            this.Workflow.Finish();
            this.LastResultFrame = null;

            // No workflow notification
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
