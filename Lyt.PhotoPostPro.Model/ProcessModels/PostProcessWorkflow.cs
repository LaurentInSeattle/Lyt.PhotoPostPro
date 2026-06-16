namespace Lyt.PhotoPostPro.Model.ProcessModels;

public sealed class PostProcessWorkflow
{
    public PostProcessWorkflow()
    {
        var orientationStep = new OrientationStep();
        var straightenStep = new StraightenStep() ;
        var compositionStep = new CompositionStep() ;
        var exposureStep = new ExposureStep() ;
        var recoveryStep = new RecoveryStep();
        var whiteBalanceStep = new WhiteBalanceStep();

        this.Steps = 
        [
            // Geometry 
            orientationStep, straightenStep, compositionStep, 
            
            //// Exposure 
            exposureStep, recoveryStep, whiteBalanceStep,

            // Color 
        ];

        int stepsCount = this.Steps.Count;
        int stepsCountMinusOne = stepsCount - 1;
        for ( int i = 0; i < stepsCount; ++ i)
        {
            var step = this.Steps[i] ;
            step.PreviousStep = i == 0 ? null : this.Steps[i-1] ;
            step.NextStep = i < stepsCountMinusOne ? this.Steps[i + 1] : null ;
        }
    }

    public bool IsComplete { get; private set; }

    public List<PostProcessStep> Steps { get; private set; }

    public int CurrentStepIndex { get; set; }

    public PostProcessStep CurrentStep => this.Steps[this.CurrentStepIndex];

    public bool CanGoBack => this.CurrentStepIndex > 0;

    public bool CanMoveNext => this.CurrentStepIndex < this.Steps.Count - 1;

    public bool Begin(Image<Rgb48> sourceImage)
    {
        if (this.Steps.Count == 0)
        {
            return false;
        }

        foreach (var step in this.Steps)
        {
            step.Initialize();
            step.IsCurrent = false;
            step.IsSkipped = false;
        }

        this.CurrentStepIndex = 0;
        this.CurrentStep.IsCurrent = true;
        this.CurrentStep.IsSkipped = false;
        this.CurrentStep.SourceImage = sourceImage;
        this.CurrentStep.ResultImage = sourceImage;
        Notify(); 
        return true;
    }

    public bool Finish()
    {
        foreach (var step in this.Steps)
        {
            step.Finish();
        }

        this.IsComplete = true;
        Notify();
        return true;
    }

    public bool SaveAndNext()
    {
        if (this.CanMoveNext)
        {
            // old step 
            this.CurrentStep.IsCurrent = false;
            this.CurrentStep.IsSkipped = false;
            var nextSourceImage = this.CurrentStep.ResultImage;
            this.CurrentStep.Save();

            // next
            this.CurrentStepIndex++;

            // new step
            this.CurrentStep.SourceImage = nextSourceImage;
            this.CurrentStep.IsCurrent = true;
            this.CurrentStep.IsSkipped = false;

            // Will only create a ResultImage if able 
            // Frame will be created when the new view gets activated 
            _ = this.CurrentStep.Transform(withFrame:false);

            // Notify to change view 
            Notify();
            return true;
        }

        return false;
    }

    public bool GoBack()
    {
        if (this.CanGoBack)
        {
            // old step 
            this.CurrentStep.IsCurrent = false;
            this.CurrentStep.IsSkipped = false;

            // previous
            this.CurrentStepIndex--;

            // new step
            this.CurrentStep.IsCurrent = true;
            this.CurrentStep.IsSkipped = false;

            // Notify to change view 
            Notify();
            return true;
        }

        return false;
    }

    public bool Reset ()
    {
        this.CurrentStep.Reset () ;
        return true;
    }

    private static void Notify() => new WorkflowUpdateMessage().Publish();
}
