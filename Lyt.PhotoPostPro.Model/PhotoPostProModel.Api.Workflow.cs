namespace Lyt.PhotoPostPro.Model;

public sealed partial class PhotoPostProModel : ModelBase
{
    public void NavigateToGallery()
        // Navigate to global folder, ignoring subdirectories if any 
        => NavigateTo(this.LibraryManager.GalleryFolderPath);

    public static void NavigateToImageFolder(Metadata metadata) =>
        // Navigate to subdirectory for specified image
        NavigateTo(System.IO.Path.GetDirectoryName(metadata.FullPath));

    private static void NavigateTo(string? directoryPath )
    {
        if (!string.IsNullOrWhiteSpace(directoryPath))
        {
            if (Directory.Exists(directoryPath))
            {
                directoryPath.OpenInExplorer();
            }
        }
    }

    /// <summary> Start post processing with a LoadedImage </summary>
    public bool ProcessLoadedImage(LoadedImage loadedImage)
    {
        if (loadedImage.Metadata is null)
        {
            return false;
        }

        try
        {
            this.CurrentWorkflow = null;
            LoadedImage? fullyLoadedImage = null;
            if (loadedImage.IsFullyLoaded)
            {
                fullyLoadedImage = loadedImage;
            }
            else if (loadedImage.IsPreLoaded)
            {
                LoadedImage reLoadedImage = ImageLoader.LoadImage(loadedImage.Metadata.FullPath);
                if (reLoadedImage.IsFullyLoaded)
                {
                    // ! because is now Fully Loaded 
                    fullyLoadedImage = reLoadedImage;
                }
            }

            if (fullyLoadedImage is null || !fullyLoadedImage.IsFullyLoaded)
            {
                return false;
            }

            // ! because fullyLoadedImage is Fully Loaded 
            ProcessWorkflow workflow =
                new(
                    this,
                    fullyLoadedImage.Metadata!,
                    fullyLoadedImage.Image!,
                    isNew: true,
                    this.FileUidString,
                    new ProcessParameters());
            this.CurrentWorkflow = workflow;
            return true;
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex);
            return false;
        }
    }

    /// <summary> Start post processing with Metadata </summary>
    public bool ProcessImageFromMetadata(
        Metadata metadata,
        bool isNew,
        string fileUidString,
        ProcessParameters postProcessParameters)
    {
        try
        {
            this.CurrentWorkflow = null;
            ProcessWorkflow? processWorkflow = null;
            LoadedImage loadedImage = ImageLoader.LoadImage(metadata.FullPath);
            if (loadedImage.IsFullyLoaded)
            {
                // ! because is now Fully Loaded 
                processWorkflow =
                    new ProcessWorkflow(
                        this, metadata, loadedImage.Image!, isNew, fileUidString, postProcessParameters);
            }

            if (processWorkflow is null)
            {
                return false;
            }

            this.CurrentWorkflow = processWorkflow;
            return true;
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex);
            return false;
        }
    }

    public void BeginProcessWorkflow()
    {
        this.ApiAction(() =>
        {
            // Workflow is checked for being not null by ApiAction wrapper 
            this.Workflow.Begin();
            this.dispatcher.OnIdle(() => GC.Collect());
            return true;
        });
    }

    public bool GetProcessOriginalImage()
    {
        // NOT an ApiAction wrapper : MUST check for nulls 
        if (this.CurrentWorkflow is null)
        {
            return false;
        }

        try
        {
            var sourceImage = this.Workflow.OriginalImage;
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
            if (this.Workflow.CurrentStep is ProcessStep step)
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
            if (this.Workflow.CurrentStep is ProcessStep step)
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

            // ! Verified by ApiAction
            string path = this.LibraryManager.SaveEditParameters(this.Workflow);
            return ! string.IsNullOrWhiteSpace(path);
        });

    public void Finish() =>
        this.ApiAction(() =>
        {
            this.Workflow.Finish();
            this.LastResultFrame?.Dispose();
            this.LastResultFrame = null;

            this.dispatcher.OnIdle(()=> GC.Collect());

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
