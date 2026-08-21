namespace Lyt.PhotoPostPro.Workflow.Library.Dialogs;

public sealed partial class ProcessingDialogModel :
    DialogViewModel<ProcessingDialog, object>,
    IRecipient<WorkflowUpdateMessage>,
    IRecipient<WorkflowAbortMessage>,
    IRecipient<WorkflowProgressMessage>
{
    [ObservableProperty]
    public partial string? Message { get; set; }

    [ObservableProperty]
    public partial string? Title { get; set; }

    [ObservableProperty]
    public partial string? Progress { get; set; }

    private readonly PostProcess postProcess;

    public ProcessingDialogModel(PostProcess postProcess)
    {
        this.postProcess = postProcess;
        this.CanEnter = false;
        this.CanEscape = false;
        this.Title = this.Localize("Imaging.InProgress");
        this.Message = this.Localize("Imaging.InProgressHelp");

        this.Subscribe<WorkflowUpdateMessage>();
        this.Subscribe<WorkflowAbortMessage>();
        this.Subscribe<WorkflowProgressMessage>();

        Schedule.OnUiThread(60,
            () =>
            {
                this.postProcess.Replay();
            },
            DispatcherPriority.ApplicationIdle);
    }

    public void Receive(WorkflowProgressMessage message)
        => Dispatch.OnUiThread(() =>
        {
            this.ReceiveOnUiThread(message);
        }, DispatcherPriority.ApplicationIdle);

    private void ReceiveOnUiThread(WorkflowProgressMessage message)
        => this.Progress =
            string.Concat(
                this.Localize("Imaging.NowProcessing"),
                " ",
                this.Localize(message.StepLocalizationName));

    public void Receive(WorkflowAbortMessage message)
        => Dispatch.OnUiThread(() =>
        {
            this.ReceiveOnUiThread(message);
        }, DispatcherPriority.ApplicationIdle);

    private void ReceiveOnUiThread(WorkflowAbortMessage _) => this.Cancel();

    public void Receive(WorkflowUpdateMessage message)
        => Dispatch.OnUiThread(() =>
            {
                this.ReceiveOnUiThread(message);
            }, DispatcherPriority.ApplicationIdle);

    private void ReceiveOnUiThread(WorkflowUpdateMessage message)
    {
        if (message.PreviousStep is null)
        {
            return;
        }

        string stepTitle = message.PreviousStep.Name;
        Debug.WriteLine(" Step: " + stepTitle);

        if (message.CurrentStep is ExportStep)
        {
            this.Cancel();
        }
    }
}
