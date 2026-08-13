namespace Lyt.PhotoPostPro.Workflow.Gallery;

public sealed partial class GalleryViewModel : ViewModel<GalleryView>
{
    private readonly PhotoPostProModel model;
    private readonly IToaster toaster;

    public GalleryViewModel(PhotoPostProModel model, IToaster toaster)
    {
        this.model = model;
        this.toaster = toaster;
    }

    [RelayCommand]
    public void OnBack()
    {

    }

    [RelayCommand]
    public void OnNext()
    {

    }
}
