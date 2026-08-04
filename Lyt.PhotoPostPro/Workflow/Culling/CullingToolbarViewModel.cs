namespace Lyt.PhotoPostPro.Workflow.Culling;

public sealed partial class CullingToolbarViewModel : ViewModel<CullingToolbarView>
{
#pragma warning disable CA1822 // Mark members as static
    // RelayCommand's cannot be static 

    [RelayCommand]
    public void OnFullscreen() =>
        new ToolbarCommandMessage(ToolbarCommandMessage.ToolbarCommand.GoFullscreen).Publish();

#pragma warning restore CA1822
}
