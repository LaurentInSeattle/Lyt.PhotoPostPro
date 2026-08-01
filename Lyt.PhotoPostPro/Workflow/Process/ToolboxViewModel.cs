namespace Lyt.PhotoPostPro.Workflow.Process;

public interface IToolboxViewModel
{
    void OnBeforeBack();
    void OnBeforeReset();
    void OnBeforeNext();

    void OnAfterBack();
    void OnAfterReset();
    void OnAfterNext();
}

public partial class ToolboxViewModel<TView, TStep> :
    ViewModel<TView>,
    IToolboxViewModel,
    IRecipient<ModelStepUpdatedMessage>
    where TView : View, new()
    where TStep : PostProcessStep
{
    protected readonly PhotoPostProModel model;
    protected readonly ShellViewModel shell;

    // When we return from fullscreen, the view will be loaded again, and the sliders will be reset to their default values.
    // We need to ignore these load events to avoid reinitializing the UI.  
    protected bool isFirstLoad;

    // the action we need to throttle when moving the sliders. May be null!
    private Action? pendingAction;

    public ToolboxViewModel()
    {
        this.model = App.GetRequiredService<PhotoPostProModel>();
        this.shell = App.GetRequiredService<ShellViewModel>();
        this.isFirstLoad = true;
        this.Subscribe<ModelStepUpdatedMessage>();
    }

    public required ToolboxHostViewModel ToolboxHostViewModel { get; set; }

    public bool IsLeftButtonPressed => this.shell.MouseMonitor.IsLeftButtonPressed;

    public bool IsRightButtonPressed => this.shell.MouseMonitor.IsRightButtonPressed;

    public override void Activate(object? activationParameters)
    {
        base.Activate(activationParameters);
        Dispatch.OnUiThread(() =>
        {
            this.ToolboxHostViewModel.Title = this.Title;
            var step = this.ModelStep<TStep>();
            this.OnModelStepUpdated(step);
        }, DispatcherPriority.Background);
    }

    // Derived view models do NOT need to call this base method 
    public override void Initialize()
    {
    }

    protected void ThrottleModelUpdate(Action action)
    {
        // Debug.WriteLine(" IsLeftButtonPressed : " + this.IsLeftButtonPressed);
        if (pendingAction is not null)
        {
            return;
        }

        if (this.IsLeftButtonPressed)
        {
            this.pendingAction = action;
            Schedule.OnUiThread(80, this.ModelUpdate, DispatcherPriority.Background);
        }
        else
        {
            action();
        }
    }

    protected void ModelUpdate()
    {
        // Debug.WriteLine(" IsLeftButtonPressed : " + this.IsLeftButtonPressed);
        if (this.IsLeftButtonPressed)
        {
            Schedule.OnUiThread(80, this.ModelUpdate, DispatcherPriority.Background);
        }
        else
        {
            this.pendingAction?.Invoke();
            this.pendingAction = null;
        }
    }

    public void Receive(ModelStepUpdatedMessage message)
    {
        if (message.Step is not TStep step)
        {
            return;
        }

        Dispatch.OnUiThread(() =>
        {
            this.OnModelStepUpdated(step);
        }, DispatcherPriority.Background);
    }

    protected TAnyStep ModelStep<TAnyStep>() where TAnyStep : PostProcessStep
    {
        var step = this.model.Workflow.CurrentStep;
        if (step is TAnyStep anyStep)
        {
            return anyStep;
        }

        throw new InvalidCastException("Current Step is not expected PostProcessStep type");
    }

    protected virtual string Title => " *** ? ***";

    // Interface implementations must be public, and same below 6 times 
    public virtual void OnModelStepUpdated(TStep step) { }

    public virtual void OnBeforeBack() { }

    public virtual void OnBeforeReset() { }

    public virtual void OnBeforeNext() { }

    public virtual void OnAfterBack() { }

    public virtual void OnAfterReset() { }

    public virtual void OnAfterNext() { }

}
