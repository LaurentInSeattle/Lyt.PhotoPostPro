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

public partial class ToolboxViewModel<T> : ViewModel<T>,  IToolboxViewModel where T : View, new() 
{
    protected readonly PhotoPostProModel model;

    public ToolboxViewModel() => this.model = App.GetRequiredService<PhotoPostProModel>();

    public required ToolboxHostViewModel ToolboxHostViewModel { get; set; }

    public override void Activate(object? activationParameters)
    {
        base.Activate(activationParameters);
        this.ToolboxHostViewModel.Title = this.Title; 
    }

    protected TStep ModelStep<TStep>() where TStep : PostProcessStep
    {
        var step = this.model.Workflow.CurrentStep;
        if (step is TStep tstep)
        {
            return tstep; 
        } 

        throw new InvalidCastException("Current is not expected type");
    } 
    
    protected virtual string Title => " *** ? ***";

    // Interface implementation must be public, and same below 5 times 
    public virtual void OnBeforeBack() { }

    public virtual void OnBeforeReset() { }

    public virtual void OnBeforeNext() { }

    public virtual void OnAfterBack() { }

    public virtual void OnAfterReset() { }

    public virtual void OnAfterNext() { }
}
