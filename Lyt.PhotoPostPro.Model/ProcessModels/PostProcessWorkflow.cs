namespace Lyt.PhotoPostPro.Model.ProcessModels;

public sealed class PostProcessWorkflow
{
    public PostProcessWorkflow(PostProcess postProcess)
    {
        this.PostProcess = postProcess;

        var orientationStep = new OrientationStep(this);
        var straightenStep = new StraightenStep(this);
        var compositionStep = new CompositionStep(this);
        var exposureStep = new ExposureStep(this);
        var recoveryStep = new RecoveryStep(this);
        var whiteBalanceStep = new WhiteBalanceStep(this);
        var contrastStep = new ContrastStep(this);
        var lutStep = new LutStep(this);
        var colorStep = new ColorStep(this);
        var sharpenStep = new SharpenStep(this);
        var vignetteStep = new VignetteStep(this);
        var filtersStep = new FiltersStep(this);
        var exportStep = new ExportStep(this);

        this.Steps =
        [
            // Geometry 
            orientationStep, straightenStep, compositionStep, 
            
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

    /// <summary> Steps of the process, should be the only property we need to serialize.  </summary>
    public List<PostProcessStep> Steps { get; private set; }

    public PostProcess PostProcess { get; private set; }

    public bool IsComplete { get; private set; }

    public int CurrentStepIndex { get; set; }

    public PostProcessStep CurrentStep => this.Steps[this.CurrentStepIndex];

    public bool CanGoBack => this.CurrentStepIndex > 0;

    public bool CanMoveNext => this.CurrentStepIndex < this.Steps.Count - 1;

    public T Get<T>() where T : PostProcessStep
    {
        var step = 
            (from stp in this.Steps where stp is T stepOfT select (T) stp )
            .FirstOrDefault();
        if ( step is null)
        {
            throw new Exception("Invalid step type");
        }

        return step; 
    }

    public bool Begin(Image<HalfVector4> originalImage)
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
        this.Notify(null, WorkflowUpdateKind.Begin);
        PostProcessStep.RecalculateHistograms(originalImage); 
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
            var nextSourceImage = this.CurrentStep.ResultImage;
            if (nextSourceImage is null)
            {
                // User just clicked 'Next' without doing anything 
                nextSourceImage = this.CurrentStep.SourceImage;
            } 

            this.CurrentStep.Deactivate(WorkflowUpdateKind.Next);

            // next
            this.CurrentStepIndex++;

            // new step
            this.CurrentStep.SourceImage = nextSourceImage;
            this.CurrentStep.Activate(WorkflowUpdateKind.Next);

            // Notify to change view 
            this.Notify(this.Steps[this.CurrentStepIndex - 1], WorkflowUpdateKind.Next);

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
            PostProcessStep.RecalculateHistograms(sourceImage);
        } 

        this.Notify(this.CurrentStep, WorkflowUpdateKind.Reset);
        return frame;
    }

    private void Notify(PostProcessStep? previousStep, WorkflowUpdateKind kind)
        => new WorkflowUpdateMessage(previousStep, this.CurrentStep, kind).Publish();
}
