namespace Lyt.PhotoPostPro.Workflow.Gallery;

public sealed partial class GalleryToolbarViewModel : ViewModel<GalleryToolbarView>
{
#pragma warning disable CA1822 
    // Mark members as static => RelayCommand's cannot be static 

    [RelayCommand]
    public void OnFullscreen() =>
        new ToolbarCommandMessage(ToolbarCommandMessage.ToolbarCommand.GoFullscreen).Publish();

    [RelayCommand]
    public void OnNavigate()
    { 
        var model = App.GetRequiredService<PhotoPostProModel>();
        model.NavigateToGallery();
    }

    [RelayCommand]
    public void OnWallpaper()
    {
        var vm = App.GetRequiredService<GalleryViewModel>();
        vm.OnWallpaper();
    }

#pragma warning restore CA1822

}
