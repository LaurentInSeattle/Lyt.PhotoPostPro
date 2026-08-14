namespace Lyt.PhotoPostPro.Workflow.Gallery;

public sealed partial class GalleryViewModel :
    ViewModel<GalleryView>,
    IRecipient<HotKeyMessage>
{
    private const double FadeDuration = 1.7;

    private readonly PhotoPostProModel model;
    private readonly LibraryManager libraryManager;
    private readonly IAnimationService animationService;
    private readonly IRandomizer randomizer; 
    private readonly IToaster toaster;

    private bool isFirstActivate;
    private List<string> galleryContent = [];
    private bool nothingToShow;
    private int nowShowingIndex = 0;
    private string nowShowing = string.Empty;
    private bool showNextOnOne;

    [ObservableProperty]
    public partial bool ButtonsAreDisabled { get; set; }

    [ObservableProperty]
    public partial WriteableBitmap? GalleryImage1 { get; set; }

    [ObservableProperty]
    public partial WriteableBitmap? GalleryImage2 { get; set; }

    public GalleryViewModel(
        PhotoPostProModel model, 
        IAnimationService animationService, 
        IRandomizer randomizer,
        IToaster toaster)
    {
        this.model = model;
        this.libraryManager = model.LibraryManager;
        this.animationService = animationService;
        this.randomizer = randomizer;
        this.toaster = toaster;
        this.isFirstActivate = true;
    }

    public override void Deactivate()
    {
        this.GalleryImage1 = null;
        this.GalleryImage2 = null;
        this.Unregister<HotKeyMessage>();
        base.Deactivate();
    }

    public override void Activate(object? activationParameters)
    {
        base.Activate(activationParameters);

        if (isFirstActivate)
        {
            this.isFirstActivate = false;
        }

        this.Subscribe<HotKeyMessage>(); 

        // Creates a local copy so that we can shuffle 
        this.galleryContent = this.libraryManager.GalleryContent.ToList();
        randomizer.Shuffle(this.galleryContent); 

        this.nothingToShow = this.galleryContent.Count == 0;
        this.View.Image1.IsVisible = false;
        this.View.Image2.IsVisible = false;
        if (this.nothingToShow)
        {
            // TODO
            // Show "nothing"  
        }
        else
        {
            this.nowShowingIndex = 0;
            this.showNextOnOne = true;
            Dispatch.OnUiThread(this.Show);
        }
    }

    public void Receive(HotKeyMessage message)
    {
        if (this.ButtonsAreDisabled)
        {
            // Dont bypass with keys 
            return;
        }

        if ((message.Key == Key.PageDown) || (message.Key == Key.PageUp))
        {
            if (message.Key == Key.PageDown)
            {
                this.OnNext(); 
            }
            else // (message.Key == Key.PageUp))
            {
                this.OnBack();
            }
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

        int next = this.nowShowingIndex + 1;
        if (next == this.galleryContent.Count)
        {
            next = 0;
        }

        // Get ready for next image 
        string nextPath = this.galleryContent[next];
        Schedule.OnUiThread(
            100 + (int)(FadeDuration * 1_000),
            this.LoadNextGalleryFile,
            nextPath,
            DispatcherPriority.ApplicationIdle); 
    }

    private void LoadNextGalleryFile(string nextPath) => this.libraryManager.LoadGalleryFile(nextPath);

    private void Show()
    {
        if (nothingToShow)
        {
            return;
        }

        // Disable buttons while fading in and out 
        this.ButtonsAreDisabled = true;
        Schedule.OnUiThread(
            200 + (int)(FadeDuration * 1000),
            () => { this.ButtonsAreDisabled = false; },
            DispatcherPriority.Background);

        this.nowShowing = this.galleryContent[this.nowShowingIndex];
        byte[]? imageBytes = this.libraryManager.GetGalleryImage(this.nowShowing);
        if (imageBytes is null)
        {
            return;
        }

        var newBitmap = WriteableBitmap.Decode(new MemoryStream(imageBytes));
        if (this.showNextOnOne)
        {
            this.animationService.FadeOut(this.View.Image2, FadeDuration);
            this.GalleryImage1 = newBitmap;
            this.animationService.FadeIn(this.View.Image1, FadeDuration);
        }
        else
        {
            this.animationService.FadeOut(this.View.Image1, FadeDuration);
            this.GalleryImage2 = newBitmap;
            this.animationService.FadeIn(this.View.Image2, FadeDuration);
        }

        this.showNextOnOne = !this.showNextOnOne;
    }

    internal void OnWallpaper()
    {
        if( galleryContent.Count == 0 )
        {
            return; 
        }

        string path = this.galleryContent[nowShowingIndex]; 
        App.WallpaperService.Set(path, WallpaperStyle.Fill);
    }
}
