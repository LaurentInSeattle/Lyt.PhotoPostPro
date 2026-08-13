namespace Lyt.PhotoPostPro.Workflow.Gallery;

public sealed partial class GalleryViewModel : ViewModel<GalleryView>
{
    private readonly PhotoPostProModel model;
    private readonly LibraryManager libraryManager;
    private readonly IAnimationService animationService;
    private readonly IToaster toaster;

    private bool isFirstActivate;
    private List<string> galleryContent = [];
    private bool nothingToShow;
    private int nowShowingIndex = 0;
    private string nowShowing = string.Empty;

    [ObservableProperty]
    public partial WriteableBitmap? GalleryImage { get; set; } 

    public GalleryViewModel(PhotoPostProModel model, IAnimationService animationService, IToaster toaster)
    {
        this.model = model;
        this.libraryManager = model.LibraryManager;
        this.animationService = animationService;
        this.toaster = toaster;
        this.isFirstActivate = true;
    }

    public override void Activate(object? activationParameters)
    {
        base.Activate(activationParameters);

        if (isFirstActivate)
        {
            this.isFirstActivate = false;
        }

        this.galleryContent = this.libraryManager.GalleryContent;
        bool nothingToShow = this.galleryContent.Count == 0;
        if (nothingToShow)
        {
        }
        else
        {
            this.nowShowingIndex = 0;
            Dispatch.OnUiThread(this.Show);
        }
    }


    [RelayCommand]
    public void OnBack()
    {
        --this.nowShowingIndex;
        if (this.nowShowingIndex < 0)
        {
            this.nowShowingIndex = this.galleryContent.Count - 1;
        }

        this.Show();
    }

    [RelayCommand]
    public void OnNext()
    {
        ++this.nowShowingIndex;
        if (this.nowShowingIndex == this.galleryContent.Count)
        {
            this.nowShowingIndex = 0;
        }

        this.Show();
    }

    private void Show()
    {
        if (nothingToShow)
        {
            return;
        }

        this.nowShowing = this.galleryContent[this.nowShowingIndex];
        byte[]? imageBytes = this.libraryManager.GetGalleryImage(this.nowShowing);
        if (imageBytes is null)
        {
            return;
        }

        this.GalleryImage = WriteableBitmap.Decode(new MemoryStream(imageBytes));
    }
}
