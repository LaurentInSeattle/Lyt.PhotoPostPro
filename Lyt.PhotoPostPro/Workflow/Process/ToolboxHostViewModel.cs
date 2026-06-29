namespace Lyt.PhotoPostPro.Workflow.Process;

public sealed partial class ToolboxHostViewModel : 
    ViewModel<ToolboxHostView>, 
    IRecipient<WorkflowUpdateMessage>
{
    private readonly PhotoPostProModel model;

    public ToolboxHostViewModel()
    {
        this.model = App.GetRequiredService<PhotoPostProModel>();
        this.Subscribe<WorkflowUpdateMessage>(); 
    } 

    [ObservableProperty]
    public partial string Title { get; set; } = " - ? ? ? -";

    [ObservableProperty]
    public partial bool BackIsDisabled { get; set; }

    [ObservableProperty]
    public partial bool ResetIsDisabled { get; set; }

    [ObservableProperty]
    public partial bool NextIsDisabled { get; set; }

    public void Receive(WorkflowUpdateMessage message)
    {
        if (message.Kind == WorkflowUpdateKind.Reset)
        {
            return;
        } 

        Dispatch.OnUiThread(() => 
        {
            this.BackIsDisabled = !this.model.Workflow.CanGoBack;
            this.NextIsDisabled = !this.model.Workflow.CanMoveNext;

            // Cannot do reset on the last step. 
            this.ResetIsDisabled = this.NextIsDisabled;
        }, DispatcherPriority.Background);
    }

    public IToolboxViewModel? ActiveToolboxViewModel { get; set;  }

    [RelayCommand]
    public void OnBack() 
    {
        this.ActiveToolboxViewModel?.OnBeforeBack();
        this.model.Back();
    }

    [RelayCommand]
    public void OnReset()
    {
        this.ActiveToolboxViewModel?.OnBeforeReset();
        this.model.Reset();
    } 

    [RelayCommand]
    public void OnNext()
    {
        this.ActiveToolboxViewModel?.OnBeforeNext();
        this.model.Next();
    }
}
