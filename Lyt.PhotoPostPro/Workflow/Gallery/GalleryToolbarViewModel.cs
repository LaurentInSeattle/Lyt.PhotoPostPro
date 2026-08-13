namespace Lyt.PhotoPostPro.Workflow.Gallery;

public sealed partial class GalleryToolbarViewModel : ViewModel<GalleryToolbarView>
{
#pragma warning disable CA1822 
    // Mark members as static => RelayCommand's cannot be static 

    [RelayCommand]
    public void OnFullscreen() =>
        new ToolbarCommandMessage(ToolbarCommandMessage.ToolbarCommand.GoFullscreen).Publish();

#pragma warning restore CA1822
}
