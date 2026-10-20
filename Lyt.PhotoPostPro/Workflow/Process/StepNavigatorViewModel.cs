namespace Lyt.PhotoPostPro.Workflow.Process;

public sealed partial class StepNavigatorViewModel :
    ViewModel<StepNavigatorView>,
    IRecipient<WorkflowUpdateMessage>,
    IRecipient<WorkflowProgressMessage>,
    IRecipient<LanguageChangedMessage>
{
    private readonly ProcessViewModel parent;
    private readonly PhotoPostProModel photoPostProModel;

    private ProcessWorkflow? workflow;
    private bool isProgrammatic;
    private string selectedStepName;

    public StepNavigatorViewModel(ProcessViewModel parent, PhotoPostProModel photoPostProModel)
    {
        this.parent = parent;
        this.photoPostProModel = photoPostProModel;

        this.BuildSteps();
        this.selectedStepName = string.Empty;
        this.Subscribe<WorkflowUpdateMessage>();
        this.Subscribe<WorkflowProgressMessage>();
        this.Subscribe<LanguageChangedMessage>();
    }

    [ObservableProperty]
    public partial List<SelectorButtonViewModel> Steps { get; set; } = [];

    public void BuildSteps()
    {
        int width = 104;
        int height = 36;
        double fontSize = 16;
        List<SelectorButtonViewModel> steps = [];
        int stepCount = ProcessStep.WorkflowOrderedStepNames.Count;
        for (int i = 0; i < stepCount; ++i)
        {
            string stepName = ProcessStep.WorkflowOrderedStepNames[i];
            string stepShortName =
                this.Localize(ProcessStep.StepShortNameLocalizationStrings[stepName]);
            bool isFirst = i == 0;
            bool isLast = i == stepCount - 1;
            bool isConnectedLeft = !isFirst;
            bool isConnectedRight = !isLast;
            steps.Add(
                new SelectorButtonViewModel(
                    stepShortName,
                    width, height,
                    this.OnStepSelected, stepName,
                    fontSize, isConnectedLeft, isConnectedRight));
        }

        this.Steps = steps;
    }

    public void Initialize(ProcessWorkflow workflow)
        => this.workflow = workflow;

    public void Receive(LanguageChangedMessage message)
        => this.BuildSteps();

    public void Receive(WorkflowProgressMessage message)
        => Dispatch.OnUiThread(() => this.ReceiveOnUiThread(message), DispatcherPriority.Background);

    // Process workflow progress messages on the UI thread,
    // ONLY for Export step when performing a Replay 
    public void ReceiveOnUiThread(WorkflowProgressMessage message)
    {
        if (this.workflow is null)
        {
            this.workflow = this.photoPostProModel.CurrentWorkflow;
            return;
        }

        if ((this.workflow is null) || !this.workflow.IsReplayMode)
        {
            return;
        }

        string stepName = message.StepName;
        Debug.WriteLine("Programmatic: " + stepName);
        if (stepName != ProcessStep.ExportStepName)
        {
            return;
        }

        // Waiting if needed
        Schedule.OnUiThread(
            120, // About one frame
            () =>
            {
                With.Flag(ref this.isProgrammatic, () =>
                {
                    SelectorButtonViewModel step = this.ViewModelFromStepName(stepName);
                    step.Select();
                    this.selectedStepName = stepName;

                    // Replay ends when we process the Export Notification 
                    this.workflow.EndReplay();
                });
            }, DispatcherPriority.Background);
    }

    public void Receive(WorkflowUpdateMessage message)
        => Dispatch.OnUiThread(() => this.ReceiveOnUiThread(message), DispatcherPriority.Background);

    // Process workflow progress messages on the UI thread,
    // When clicking Previous or Next or using the Navigator 
    // Excludes the Replay mode 
    public void ReceiveOnUiThread(WorkflowUpdateMessage message)
    {
        if (message.Kind == WorkflowUpdateKind.Abort)
        {
            return;
        }

        if ((this.workflow is null) || this.workflow.IsReplayMode)
        {
            return;
        }

        if (message.CurrentStep is null)
        {
            return;
        }

        string stepName = message.CurrentStep.Name;

        void SelectStep()
        {
            With.Flag(ref this.isProgrammatic, () =>
            {
                Debug.WriteLine("Programmatic: " + stepName);
                var step = this.ViewModelFromStepName(stepName);
                step.Select();
                this.selectedStepName = stepName;
            });
        }

        // Waiting is not necessary here except for the first one 
        if (stepName == ProcessStep.OrientationStepName)
        {
            // About one frame
            Schedule.OnUiThread(120, SelectStep, DispatcherPriority.Background);
        }
        else
        {
            SelectStep();
        }
    }

    void OnStepSelected(object? tag)
    {
        string previouslySelectedStepName = this.selectedStepName;

        // ! All steps have a tag
        this.selectedStepName = (string)tag!;

        if (this.isProgrammatic)
        {
            return;
        }

        if (tag is string stepName && this.workflow is not null)
        {
            Debug.WriteLine("User action: " + stepName);
            ProcessStep? step = this.workflow.GetStepByName(stepName);
            if (step is null)
            {
                Debug.WriteLine("Step not found: " + stepName);
                return;
            }

            // First check if this is a back or next selection 
            int previousIndex = this.StepIndexFromStepName(previouslySelectedStepName);
            int newIndex = this.StepIndexFromStepName(stepName);
            ToolboxHostViewModel toolboxHost = this.parent.ToolboxHostViewModel;
            if (newIndex == previousIndex + 1)
            {
                toolboxHost.OnNext();
            }
            else if (newIndex == previousIndex - 1)
            {
                toolboxHost.OnBack();
            }
            else
            {
                int delta = newIndex - previousIndex;
                int count = Math.Abs(delta);
                if (delta > 0)
                {
                    toolboxHost.OnJumpNextTo(stepName);
                }
                else
                {
                    toolboxHost.OnJumpBackTo(stepName);
                }
            }
        }
    }

    private SelectorButtonViewModel ViewModelFromStepName(string stepName)
    {
        foreach (var step in this.Steps)
        {
            if (step.Tag is string name && name == stepName)
            {
                return step;
            }
        }

        throw new InvalidOperationException($"Step '{stepName}' not found.");
    }

    private int StepIndexFromStepName(string stepName)
    {
        int index = 0;
        foreach (var step in this.Steps)
        {
            if (step.Tag is string name && name == stepName)
            {
                return index;
            }

            ++index;
        }

        throw new InvalidOperationException($"Step '{stepName}' not found.");
    }
}
