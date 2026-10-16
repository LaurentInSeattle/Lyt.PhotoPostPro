namespace Lyt.PhotoPostPro.Workflow.Process;

public sealed partial class StepNavigatorViewModel :
    ViewModel<StepNavigatorView>,
    IRecipient<WorkflowUpdateMessage>,
    IRecipient<WorkflowProgressMessage>
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

        this.selectedStepName = string.Empty;
        this.Subscribe<WorkflowUpdateMessage>();
        this.Subscribe<WorkflowProgressMessage>();
    }

    [ObservableProperty]
    public partial List<SelectorButtonViewModel> Steps { get; set; } = [];

    public void Initialize(ProcessWorkflow workflow)
    {
        int width = 104;
        int height = 36;
        double fontSize = 16;
        List<SelectorButtonViewModel> steps = [];
        foreach (var step in workflow.Steps)
        {
            string stepName = step.Name;
            string stepShortName =
                this.Localize(ProcessStep.StepShortNameLocalizationStrings[stepName]);
            steps.Add(
                new SelectorButtonViewModel(stepShortName, width, height, this.OnStepSelected, stepName, fontSize));
        }

        this.Steps = steps;
        this.workflow = workflow;
    }

    public void Receive(WorkflowProgressMessage message)
        => Dispatch.OnUiThread(() => this.ReceiveOnUiThread(message), DispatcherPriority.Background);

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

        SelectorButtonViewModel step = this.ViewModelFromStepName(stepName);
        Schedule.OnUiThread(
            120, // About two frames
            () =>
            {
                With.Flag(ref this.isProgrammatic, () =>
                {
                    step.Select();
                    this.selectedStepName = stepName;
                });
            }, DispatcherPriority.Background);
    }

    public void Receive(WorkflowUpdateMessage message)
        => Dispatch.OnUiThread(() => this.ReceiveOnUiThread(message), DispatcherPriority.Background);

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

        With.Flag(ref this.isProgrammatic, () =>
        {
            if (message.Kind == WorkflowUpdateKind.Begin)
            {
                // When starting a new edit, we need to wait a bit so that the Navigator UI has time to load
                Schedule.OnUiThread(
                    50, // About one frame
                    () => this.Steps[0].Select(), DispatcherPriority.Background);

                // ! All steps have a tag
                this.selectedStepName = (string)this.Steps[0].Tag!;
            }
            else
            {
                if (message.CurrentStep is not null)
                {
                    string stepName = message.CurrentStep.Name;
                    Debug.WriteLine("Programmatic: " + stepName);
                    var step = this.ViewModelFromStepName(stepName);
                    step.Select();
                    this.selectedStepName = stepName;
                }
            }
        });
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
                    for (int i = 0; i < count; i++)
                    {
                        toolboxHost.OnNext();
                    }
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
