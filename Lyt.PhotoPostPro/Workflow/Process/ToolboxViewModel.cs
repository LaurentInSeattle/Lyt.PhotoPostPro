namespace Lyt.PhotoPostPro.Workflow.Process;

public interface IToolboxViewModel
{
    void OnBeforeBack();
    void OnBeforeReset();
    void OnBeforeNext();
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

    protected virtual string Title => " *** ? ***";

    // Interface implementation must be public, and same below twice
    public virtual void OnBeforeBack() { }

    public virtual void OnBeforeReset() { }

    public virtual void OnBeforeNext() { }
}
