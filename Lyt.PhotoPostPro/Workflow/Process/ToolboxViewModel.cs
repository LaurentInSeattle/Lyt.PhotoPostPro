namespace Lyt.PhotoPostPro.Workflow.Process;

public partial class ToolboxViewModel<T> : ViewModel<T> where T : View, new()
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
}
