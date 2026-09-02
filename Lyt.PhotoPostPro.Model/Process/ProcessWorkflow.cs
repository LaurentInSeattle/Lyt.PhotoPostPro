namespace Lyt.PhotoPostPro.Model.Process;

public sealed class ProcessWorkflow
{
    public ProcessWorkflow(
        PhotoPostProModel model,
        Metadata metadata,
        Image<RgbaHalf> originalImage,
        bool isNew,
        string fileUidString,
        ProcessParameters postProcessParameters)
    {

        this.MaybeModel = model;
        this.Metadata = metadata;
        this.MaybeOriginalImage = originalImage;
        this.IsNew = isNew;
        this.FileUidString = fileUidString;
        this.PostProcessParameters = postProcessParameters;

        this.IsReplayMode = false;

        var orientationStep = new OrientationStep(this);
        var straightenStep = new StraightenStep(this);
        var compositionStep = new CompositionStep(this);
        var denoiseStep = new DenoiseStep(this);
        var exposureStep = new ExposureStep(this);
        var recoveryStep = new RecoveryStep(this);
        var vignetteStep = new VignetteStep(this);
        var whiteBalanceStep = new WhiteBalanceStep(this);
        var contrastStep = new ContrastStep(this);
        var lutStep = new LutStep(this);
        var colorStep = new ColorStep(this);
        var sharpenStep = new SharpenStep(this);
        var filtersStep = new FiltersStep(this);
        var exportStep = new ExportStep(this);

        this.Steps =
        [
            // Geometry 
            orientationStep, straightenStep, compositionStep, 
            
            // Denoise 
            denoiseStep, 

            // Exposure 
            exposureStep, recoveryStep, vignetteStep, 

            // Constrast and Color 
            whiteBalanceStep, contrastStep, lutStep, colorStep, sharpenStep, 

            // Final Filters
            filtersStep,

            // Export
            exportStep,
        ];

        int stepsCount = this.Steps.Count;
        int stepsCountMinusOne = stepsCount - 1;
        for (int i = 0; i < stepsCount; ++i)
        {
            var step = this.Steps[i];
            step.PreviousStep = i == 0 ? null : this.Steps[i - 1];
            step.NextStep = i < stepsCountMinusOne ? this.Steps[i + 1] : null;
        }
    }

    public PhotoPostProModel? MaybeModel { get; set; }

    public PhotoPostProModel Model
        => this.MaybeModel ??
            throw new InvalidOperationException("Model must be set before accessing it.");

    public Image<RgbaHalf>? MaybeOriginalImage { get; set; }

    public Image<RgbaHalf> OriginalImage
        => this.MaybeOriginalImage ??
            throw new InvalidOperationException("Source image must be loaded before accessing it.");

    public Metadata Metadata { get; set; }

    public bool IsReplayMode { get; private set; }

    public bool IsNew { get; private set; }

    public string FileUidString { get; private set; } = string.Empty;

    public ProcessParameters PostProcessParameters { get; private set; }

    public string SourceFilePath => this.Metadata.FullPath;


    /// <summary> Steps of the process, should be the only property we need to serialize.  </summary>
    public List<ProcessStep> Steps { get; private set; }

    public bool IsComplete { get; private set; }

    public int CurrentStepIndex { get; set; }

    public ProcessStep CurrentStep => this.Steps[this.CurrentStepIndex];

    public bool CanGoBack => this.CurrentStepIndex > 0;

    public bool CanMoveNext => this.CurrentStepIndex < this.Steps.Count - 1;

    public T Get<T>() where T : ProcessStep
    {
        var step =
            (from stp in this.Steps where stp is T stepOfT select (T)stp)
            .FirstOrDefault();
        return step is null ? throw new Exception("Invalid step type") : step;
    }

    public void Begin()
    {
        // Auto gives a first star when beginning editing
        if (this.Metadata.Rating == 0)
        {
            ++this.Metadata.Rating;
        }

        this.Metadata.LastEditedUTC = DateTime.UtcNow;
        this.Model.LibraryManager.SaveMetadata(this.Metadata);

        this.Begin(this.OriginalImage);
    }

    public void Replay()
    {
        this.IsReplayMode = true;

        Task.Run(() =>
        {
            bool error = false;
            try
            {
                this.Begin();

                // Throttle 
                Task.Delay(60).Wait();

                foreach (var step in this.Steps)
                {
                    if (step is ExportStep)
                    {
                        break;
                    }

                    new WorkflowProgressMessage(step.LocalizationName).Publish();
                    this.Next();

                    // Throttle 
                    Task.Delay(60).Wait();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
                error = true;
            }
            finally
            {
                if (error)
                {
                    this.IsReplayMode = false;
                    new WorkflowAbortMessage().Publish();
                }
            }
        });
    }

    private bool Begin(Image<RgbaHalf> originalImage)
    {
        if (this.Steps.Count == 0)
        {
            return false;
        }

        foreach (var step in this.Steps)
        {
            step.Initialize(originalImage);
        }

        this.CurrentStepIndex = 0;
        this.CurrentStep.SourceImage = originalImage;
        this.CurrentStep.ResultImage = originalImage;

        if (!this.IsNew)
        {
            // We have parameters to continue editing
            foreach (var step in this.Steps)
            {
                // this will trigger an automatic edit using the PostProcess parameters...
                step.InitialRunNeeded = true;
            }

            // Do it here for the very first step
            this.CurrentStep.PerformStep(this.PostProcessParameters);
            this.CurrentStep.InitialRunNeeded = false;
        }

        this.Notify(null, WorkflowUpdateKind.Begin);
        ProcessStep.RecalculateHistograms(originalImage);
        return true;
    }

    public bool Finish()
    {
        foreach (var step in this.Steps)
        {
            step.Finish();
        }

        this.IsComplete = true;
        this.Notify(null, WorkflowUpdateKind.Finish);
        return true;
    }

    public Frame? Next()
    {
        if (this.CanMoveNext)
        {
            // old step 
            // User just clicked 'Next' without doing anything 
            var nextSourceImage = this.CurrentStep.ResultImage ?? this.CurrentStep.SourceImage;
            this.CurrentStep.Deactivate(WorkflowUpdateKind.Next);

            // next
            this.CurrentStepIndex++;

            // new step
            this.CurrentStep.SourceImage = nextSourceImage;
            this.CurrentStep.Activate(WorkflowUpdateKind.Next);

            bool hasPerformedStep = false;
            if (this.CurrentStep.InitialRunNeeded)
            {
                this.CurrentStep.PerformStep(this.PostProcessParameters);
                this.CurrentStep.InitialRunNeeded = false;
                hasPerformedStep = true;
            }

            // Notify to change view 
            this.Notify(this.Steps[this.CurrentStepIndex - 1], WorkflowUpdateKind.Next);

            // Notify to tell the UI to update 
            if (hasPerformedStep)
            {
                new ModelStepUpdatedMessage(Step: this.CurrentStep).Publish();
            }

            // Return current result image 
            var resultImage = this.CurrentStep.ResultImage;
            return resultImage?.ToFrame();
        }

        return null;
    }

    public bool Back()
    {
        if (this.CanGoBack)
        {
            // old step 
            this.CurrentStep.Deactivate(WorkflowUpdateKind.Back);

            // previous
            this.CurrentStepIndex--;

            // new step
            this.CurrentStep.Activate(WorkflowUpdateKind.Back);

            // Notify to change view 
            this.Notify(this.Steps[this.CurrentStepIndex + 1], WorkflowUpdateKind.Back);
            return true;
        }

        return false;
    }

    public Frame? Reset()
    {
        var frame = this.CurrentStep.Reset();
        var sourceImage = this.CurrentStep.SourceImage;
        if (sourceImage is not null)
        {
            ProcessStep.RecalculateHistograms(sourceImage);
        }

        this.Notify(this.CurrentStep, WorkflowUpdateKind.Reset);
        return frame;
    }

    private void Notify(ProcessStep? previousStep, WorkflowUpdateKind kind)
        => new WorkflowUpdateMessage(previousStep, this.CurrentStep, kind).Publish();
}
