namespace Lyt.PhotoPostPro.Workflow.FirstRun;

public sealed partial class FirstRunViewModel : ViewModel<FirstRunView>
{
    private readonly PhotoPostProModel model;
    private readonly IToaster toaster;

    public FirstRunViewModel(PhotoPostProModel model, IToaster toaster)
    {
        this.model = model;
        this.toaster = toaster;
    }
}
