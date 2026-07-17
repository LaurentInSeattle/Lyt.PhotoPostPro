namespace Lyt.PhotoPostPro.Model;

public sealed partial class PhotoPostProModel : ModelBase
{
    /// <summary> Start post processing with a LoadedImage </summary>
    public bool ProcessLoadedImage(LoadedImage loadedImage)
    {
        this.CurrentPostProcess = null;
        if (loadedImage.Metadata is null)
        {
            return false;
        }

        try
        {
            PostProcess? postProcess = null;
            if (loadedImage.IsFullyLoaded)
            {
                // ! because is Fully Loaded 
                postProcess = new PostProcess(this, loadedImage.Metadata, loadedImage.Image!);
            }
            else if (loadedImage.IsPreLoaded)
            {
                LoadedImage fullyLoadedImage = ImageLoader.LoadImage(loadedImage.Metadata.FullPath);
                if (loadedImage.IsFullyLoaded)
                {
                    // ! because is now Fully Loaded 
                    postProcess = new PostProcess(this, loadedImage.Metadata, loadedImage.Image!);
                }
            }

            if (postProcess is null)
            {
                return false;
            }

            this.CurrentPostProcess = postProcess;
            return true;
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex);
            return false;
        }
    }

    /// <summary> Start post processing with Metadata </summary>
    public bool ProcessImageFromMetadata(Metadata metadata)
    {
        this.CurrentPostProcess = null;

        try
        {
            PostProcess? postProcess = null;
            LoadedImage loadedImage = ImageLoader.LoadImage(metadata.FullPath);
            if (loadedImage.IsFullyLoaded)
            {
                // ! because is now Fully Loaded 
                postProcess = new PostProcess(this, metadata, loadedImage.Image!);
            }

            if (postProcess is null)
            {
                return false;
            }

            this.CurrentPostProcess = postProcess;
            return true;
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex);
            return false;
        }
    }

    public void BeginPostProcess()
    {
        this.ApiAction(() =>
        {
            // ! CurrentPostProcess is checked for being not null by ApiAction wrapper 
            this.CurrentPostProcess!.Begin();
            return true;
        });
    }

    public bool GetProcessOriginalImage()
    {
        // NOT an ApiAction wrapper : MUST check for nulls 
        if (this.CurrentPostProcess is null)
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
            if (frame is not null)
            {
                this.LastResultFrame = frame;
            }


            string desktop = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            var stepsParameters = PostProcessParameters.FromPostProcessWorkflow(this.Workflow); 
            string serialized = this.fileManager.Serialize<PostProcessParameters>(stepsParameters);
            string testPath = System.IO.Path.Combine(desktop, "test.json");
            File.WriteAllText(testPath, serialized);
            var desteps = this.fileManager.Deserialize<PostProcessParameters>(serialized);
            if ( desteps is PostProcessParameters parameters)
            {
                Debug.WriteLine(desteps);
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

        //if ((this.CurrentProject is null) ||
        //    (this.CurrentProjectMetadata is null) ||
        if (this.CurrentPostProcess is null)
        {
            string errorMessage = "No post process is currently active.";
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
