namespace Lyt.PhotoPostPro.Workflow.Culling;

// Do not add those ImageSharp namespaces to global using as some class definitions conflict
// with the ones from Avalonia. (Point, Rectangle, etc.) 
//using SixLabors.ImageSharp;
//using SixLabors.ImageSharp.PixelFormats;

public sealed partial class CullingViewModel : 
    ViewModel<CullingView>, 
    ISelectListener, 
    IRecipient<HotKeyMessage>
{
    public enum LayoutKind
    {
        None,
        SingleImage,
        DualImageLandscape,
        DualImagePortrait,
        ManyImages ,
    }

    public sealed record class UiThumbnail(string Key, Metadata Metadata, WriteableBitmap Bitmap);

    private readonly PhotoPostProModel model;
    private readonly LibraryManager libraryManager;
    private readonly IToaster toaster;

    private readonly Dictionary<string, UiThumbnail> allHdImages = [];

    private LayoutKind layoutKind ;

    [ObservableProperty]
    public partial UiThumbnail? SelectedThumbnail { get; set; }

    [ObservableProperty]
    public partial bool HasSelection { get; set; }

    [ObservableProperty]
    public partial List<UiThumbnail> ImageThumbnails { get; set; } = [];

    [ObservableProperty]
    public partial ObservableCollection<UiThumbnail> SelectedThumbnails { get; set; } = [];

    [ObservableProperty]
    public partial int SelectedThumbnailIndex { get; set; }
        
    [ObservableProperty]
    public partial ObservableCollection<UiThumbnail> SelectedImages { get; set; } = [];

    [ObservableProperty]
    public partial SpinViewModel SpinViewModel { get; set; }

    [ObservableProperty]
    public partial string Status { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool StatusIsVisible { get; set; }

    [ObservableProperty]
    public partial bool SingleImageLayoutIsVisible { get; set; }

    [ObservableProperty]
    public partial bool DualLandscapeImageLayoutIsVisible { get; set; }

    [ObservableProperty]
    public partial bool DualPortraitImageLayoutIsVisible { get; set; }

    [ObservableProperty]
    public partial bool ManyImagesLayoutIsVisible { get; set; }

    [ObservableProperty]
    // Single image layout bitmap
    public partial CullingImageViewModel? SingleImageViewModel { get; set; }

    [ObservableProperty]
    // Dual image layout bitmap - top or left 
    public partial WriteableBitmap? Bitmap1 { get; set; }

    [ObservableProperty]
    // Dual image layout bitmap - bottom or right
    public partial WriteableBitmap? Bitmap2 { get; set; }

    public CullingViewModel(PhotoPostProModel model, IToaster toaster)
    {
        this.model = model;
        this.libraryManager = model.LibraryManager;
        this.layoutKind = LayoutKind.None;
        this.toaster = toaster;
        this.SpinViewModel = new SpinViewModel()
        {
            IsVisible = false,
            IsActive = false,
        };
    }

    public override void Activate(object? activationParameters)
    {
        base.Activate(activationParameters);
        this.View.StripListBox.SelectionChanged += this.OnSelectedThumbnailsChanged;
    }

    public override void Deactivate()
    {
        this.View.StripListBox.SelectionChanged -= this.OnSelectedThumbnailsChanged;
        this.ClearImages();
        this.ClearAllCollections();
        base.Deactivate();
    }

    internal void Initialize(List<string> files)
    {
        this.ClearAllCollections();

        if (files.Count == 0)
        {
            return;
        }


        this.SpinWait(); 
        this.Status = "Loading Thumbnails...";
        this.StatusIsVisible = true;

        // Create empty slots so that we dont need to use Add which would cause losing the ordering of the files.
        var list = new List<UiThumbnail?>();
        for (int i = 0; i < files.Count; ++i)
        {
            list.Add(null);
        }

        var pathList = new List<string>();
        Parallel.For(0, files.Count, index =>
        {
            string file = files[index];
            if (this.libraryManager.LoadedThumbnails.TryGetValue(file, out var loadedThumbnail))
            {
                var thumbnail = WriteableBitmap.Decode(new MemoryStream(loadedThumbnail.ImageBytes));

                // Using an index so that the ordering of the list is maintained 
                list[index] = new UiThumbnail(file, loadedThumbnail.Metadata, thumbnail);
                pathList.Add(loadedThumbnail.Metadata.FullPath);
            }
        });

        // We may have 'holes' in the list if some files failed to load, so we filter them out
        list = list.Where(t => t is not null).ToList();

        // ! 'holes' have been filtered out, so we can safely cast to non-nullable type
        this.ImageThumbnails = list!;

        this.Status = this.Localize("Workflow.Culling.LoadingHD"); 
        this.StatusIsVisible = true;    

        Task.Run(() =>
        {
            Thread.CurrentThread.Name = "CullingViewModel.LoadHdImages";

            // Delay so the UI can fadein - fadeout and render the thumbnails first
            Task.Delay(240).Wait();

            // Then load HD images in the background
            this.libraryManager.LoadHdImages(pathList);

            Dispatch.OnUiThread(() =>
            {
                this.Status = this.Localize("Workflow.Culling.DecodingHD");
                this.StatusIsVisible = true;
            });

            this.DecodeHdImages(pathList);

            Dispatch.OnUiThread(() =>
            {
                this.Status = this.Localize("Workflow.Culling.Ready");
                this.StatusIsVisible = true;
                this.SpinWait(start: false);

                // Clear Selection and select first 
                this.SelectedThumbnailIndex = -1;
                this.SelectedThumbnailIndex = 0;
            });

            Schedule.OnUiThread(2_500, () =>
            {
                this.Status = string.Empty;
                this.StatusIsVisible = false;
            }, DispatcherPriority.ApplicationIdle);
        });
    }

    private void DecodeHdImages(List<string> pathList)
    {
        Parallel.For(0, pathList.Count, index =>
        {
            // Throttle
            Task.Delay(40).Wait();
            if (this.libraryManager.LoadedHdImages.TryGetValue(pathList[index], out LoadedImage? loadedHdImage))
            {
                if (loadedHdImage is not null)
                {
                    if (loadedHdImage.JpgThumbnail is byte[] imageBytes && loadedHdImage.Metadata is not null)
                    {
                        // Decode the image and store it in a dictionary for later use in the UI
                        // when the user selects one or more thumbnails. 
                        var bitmap = WriteableBitmap.Decode(new MemoryStream(imageBytes));
                        string key = loadedHdImage.Metadata.MetadataFullPath();
                        lock (this.allHdImages)
                        {
                            this.allHdImages.Add(key, new UiThumbnail(key, loadedHdImage.Metadata, bitmap));
                        }
                    }
                }
            }
        });
    }

    private void OnSelectedThumbnailsChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (!this.IsActivated || sender is null)
        {
            return;
        }

        var list = new List<UiThumbnail>(this.SelectedThumbnails.Count);
        foreach (UiThumbnail selectedThumbnail in this.SelectedThumbnails.ToList())
        {
            string key = selectedThumbnail.Key;
            if (this.allHdImages.TryGetValue(key, out var hdImage))
            {
                list.Add(hdImage);
            }
            else
            {
                list.Add(selectedThumbnail);
            }
        }

        if ( list.Count== 0)
        {
            return; 
        }

        this.LayoutSelectedImages(list); 
    }

    private void LayoutSelectedImages(List<UiThumbnail> list)
    {
        this.ClearImages(); 

        if (list.Count == 1)
        {
            this.layoutKind = LayoutKind.SingleImage;
            this.SingleImageLayoutIsVisible = true;
            var uiThumbnail = list[0]; 
            this.SingleImageViewModel =
                new CullingImageViewModel(this, uiThumbnail.Metadata, uiThumbnail.Bitmap);
            this.Bitmap1 = null;
            this.Bitmap2 = null;
            return; 
        }
        
        if (list.Count == 2)
        {
            var bitmap1 = list[0].Bitmap;
            var size1 = bitmap1.PixelSize;
            var bitmap2 = list[1].Bitmap;
            var size2 = bitmap2.PixelSize;
            if ( ( size1.Width >= size1.Height) && ( size2.Width >= size2.Height))
            {
                // Both landscape or square
                this.layoutKind = LayoutKind.DualImageLandscape;
                this.DualLandscapeImageLayoutIsVisible = true;
                this.Bitmap1 = bitmap1;
                this.Bitmap2 = bitmap2;
                return; 
            }

            if ((size1.Width <= size1.Height) && (size2.Width <= size2.Height))
            {
                // Both portrait or square
                this.layoutKind = LayoutKind.DualImagePortrait;
                this.DualPortraitImageLayoutIsVisible = true;
                this.Bitmap1 = bitmap1;
                this.Bitmap2 = bitmap2;
                return;
            }
        }

        // All other cases: use the uniform grid 
        this.layoutKind = LayoutKind.ManyImages;
        this.ManyImagesLayoutIsVisible = true;
        this.SelectedImages = new(list);
    }

    private void SpinWait(bool start = true)
    {
        this.SpinViewModel.IsVisible = start;
        this.SpinViewModel.IsActive = start;
    }

    private void ClearAllCollections()
    {
        this.allHdImages.Clear();
        this.ImageThumbnails.Clear();
        this.SelectedThumbnails = [];
        this.SelectedImages = [];
    }

    private void ClearImages ()
    {
        this.SingleImageLayoutIsVisible = false;
        this.DualLandscapeImageLayoutIsVisible = false;
        this.DualPortraitImageLayoutIsVisible = false;
        this.ManyImagesLayoutIsVisible = false;

        this.SingleImageViewModel = null;
        this.Bitmap1 = null;
        this.Bitmap2 = null;
        this.SelectedImages = [];
    }

    public void OnSelect(object selectedObject) {}

    [RelayCommand]
    public void OnAddStar()
    {
        if ( this.layoutKind == LayoutKind.SingleImage && this.SingleImageViewModel is not null)
        {
            this.SingleImageViewModel.ChangeRating(isAddStar: true);
        }
    }

    [RelayCommand]
    public void OnRemoveStar()
    {
        if (this.layoutKind == LayoutKind.SingleImage && this.SingleImageViewModel is not null)
        {
            this.SingleImageViewModel.ChangeRating(isAddStar: false);
        }
    }

    [RelayCommand]
    public void OnReject ()
    {

    }

    public void Receive(HotKeyMessage message)
    {
        // TODO 
    } 
}
