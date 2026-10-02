namespace Lyt.PhotoPostPro.Workflow.Settings;

public sealed partial class SettingsViewModel : ViewModel<SettingsView>
{
    private readonly PhotoPostProModel model;
    private readonly IToaster toaster;

    public SettingsViewModel(PhotoPostProModel model, IToaster toaster)
    {
        this.model = model;
        this.toaster = toaster;
    }
}
