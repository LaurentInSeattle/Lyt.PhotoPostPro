namespace Lyt.PhotoPostPro.Workflow.Gallery;

public sealed partial class GalleryViewModel :
    ViewModel<GalleryView>,
    IRecipient<HotKeyMessage>
{
    private const double FadeDuration = 1.6;

#if DEBUG 
    private const int SlideDuration = 12;
#else
    private const int SlideDuration = 32;
#endif

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
    private DispatcherTimer? slideShowTimer;

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

        // We are potentially about to launch heavy stuff, so clean up while we still can
        // We have about at least one second for Drag and drop to happen 
        this.Dispatcher.OnIdle(() => GC.Collect());
    }

    public void Receive(HotKeyMessage message)
    {
        if (message.Key == Key.Escape)
        {
            this.OnSlideShowEnd();
            return;
        }

        if (this.ButtonsAreDisabled)
        {
            // Dont bypass with keys 
            return;
        }

        if ((message.Key == Key.PageDown) || (message.Key == Key.PageUp))
        {
            if (message.Key == Key.PageDown)
            {
                this.Next();
            }
            else // (message.Key == Key.PageUp))
            {
                this.Back();
            }
        }
    }

    [RelayCommand]
    public void OnBack() => this.Back();

    [RelayCommand]
    public void OnNext() => this.Next();

    [RelayCommand]
    public void OnSlideShowBegin()
    {
        if (this.nothingToShow)
        {
            return;
        }

        this.slideShowTimer = new DispatcherTimer()
        {
            Interval = TimeSpan.FromSeconds(SlideDuration),
            IsEnabled = false,
        };

        this.ButtonsAreDisabled = true;
        this.slideShowTimer.Tick += this.OnSlideShowTimerTick;
        this.slideShowTimer.IsEnabled = true;
        this.slideShowTimer.Start();
        new ToolbarCommandMessage(ToolbarCommandMessage.ToolbarCommand.GoFullscreen).Publish();
    }

    private void OnSlideShowTimerTick(object? sender, EventArgs e)
    {
        if (this.nothingToShow)
        {
            return;
        }

        this.Next();
    }

    internal void OnImageClicked()
    {
        if (this.slideShowTimer is null)
        {
            // Not running the slide show 
            return; 
        } 

        this.OnSlideShowEnd(); 
        new ToolbarCommandMessage(ToolbarCommandMessage.ToolbarCommand.BackToWindowed).Publish();
    }

    private void OnSlideShowEnd()
    {
        this.ButtonsAreDisabled = false;

        if ( this.slideShowTimer is not null)
        {
            this.slideShowTimer.Stop();
            this.slideShowTimer.IsEnabled = false;
            this.slideShowTimer = null; 
        }
    }

    private void LoadNextGalleryFile(string nextPath) => this.libraryManager.LoadGalleryFile(nextPath);

    private void Next()
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

    private void Back()
    {
        --this.nowShowingIndex;
        if (this.nowShowingIndex < 0)
        {
            this.nowShowingIndex = this.galleryContent.Count - 1;
        }

        this.Show();
    }

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
        if (galleryContent.Count == 0)
        {
            return;
        }

        string path = this.galleryContent[nowShowingIndex];
        App.WallpaperService.Set(path, WallpaperStyle.Fill);
    }
}
