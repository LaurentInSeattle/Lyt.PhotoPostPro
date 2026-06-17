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

    public ToolboxViewModel() 
    {
        this.model = App.GetRequiredService<PhotoPostProModel>();
        this.Subscribe<ModelStepUpdatedMessage>();
    } 

    public required ToolboxHostViewModel ToolboxHostViewModel { get; set; }

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

    public void Receive(ModelStepUpdatedMessage message)
    {
        if (message.Step is not TStep step)
        {
            return; 
        }

        this.OnModelStepUpdated(step);
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
    public virtual void OnModelStepUpdated (TStep step) { }

    public virtual void OnBeforeBack() { }

    public virtual void OnBeforeReset() { }

    public virtual void OnBeforeNext() { }

    public virtual void OnAfterBack() { }

    public virtual void OnAfterReset() { }

    public virtual void OnAfterNext() { }

}
