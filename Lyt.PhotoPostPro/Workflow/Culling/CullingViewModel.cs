namespace Lyt.PhotoPostPro.Workflow.Culling;

using static Lyt.PhotoPostPro.Workflow.Culling.CullingViewModel;

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
        ManyImages,
    }

    public sealed record class UiThumbnail(string Key, Metadata Metadata, WriteableBitmap Bitmap);

    private readonly PhotoPostProModel model;
    private readonly LibraryManager libraryManager;
    private readonly IToaster toaster;

    private readonly Dictionary<string, UiThumbnail> allHdImages = [];

    private LayoutKind layoutKind;
    private bool isShowHintsSingleImageFirstTime;
    private bool isShowHintsDualLandscapeFirstTime;
    private bool isShowHintsDualPortraitFirstTime;

    //[ObservableProperty]
    //public partial bool HasSelection { get; set; }

    [ObservableProperty]
    // The collection of images in the film strip 
    public partial ObservableCollection<UiThumbnail> ImageThumbnails { get; set; } = [];

    [ObservableProperty]
    // the Selected Thumbnails in the film strip 
    public partial ObservableCollection<UiThumbnail> SelectedThumbnails { get; set; } = [];

    [ObservableProperty]
    // SelectedIndex in the film strip 
    public partial int SelectedThumbnailIndex { get; set; }

    [ObservableProperty]
    // The - More than 2 - images selected from the film strip to show in the center area
    public partial ObservableCollection<UiThumbnail> SelectedImages { get; set; } = [];

    [ObservableProperty]
    public partial SpinViewModel SpinViewModel { get; set; }

    [ObservableProperty]
    public partial bool ShowHints { get; set; }

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
    // Single image metadata 
    public partial MetadataViewModel? SelectedImageMetadataViewModel { get; set; }

    [ObservableProperty]
    // Dual image layout bitmap - top or left 
    public partial CullingImageViewModel? SingleImageViewModelTopOrLeft { get; set; }

    [ObservableProperty]
    // Dual image layout bitmap - bottom or right
    public partial CullingImageViewModel? SingleImageViewModelBottomOrRight { get; set; }

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
        this.Subscribe<HotKeyMessage>();
    }

    public override void Activate(object? activationParameters)
    {
        base.Activate(activationParameters);
        this.isShowHintsSingleImageFirstTime = true;
        this.isShowHintsDualLandscapeFirstTime = true;
        this.isShowHintsDualPortraitFirstTime = true;
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
        this.ImageThumbnails = new(list!);

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

        if (list.Count == 0)
        {
            return;
        }

        this.LayoutSelectedImages(list);
    }

    private void LayoutSelectedImages(List<UiThumbnail> list)
    {
        this.ClearImages();

        this.ShowHints = true;
        if (list.Count == 1)
        {
            Schedule.OnUiThread(
                this.isShowHintsSingleImageFirstTime ? 12_000 : 2_000,
                () =>
                {
                    this.ShowHints = false;
                    this.isShowHintsSingleImageFirstTime = false;
                },
                DispatcherPriority.Background);

            this.layoutKind = LayoutKind.SingleImage;
            this.SingleImageLayoutIsVisible = true;
            var uiThumbnail = list[0];
            this.SingleImageViewModel =
                new CullingImageViewModel(this, uiThumbnail.Metadata, uiThumbnail.Bitmap);
            this.SelectedImageMetadataViewModel = new MetadataViewModel(uiThumbnail.Metadata);
            return;
        }

        if (list.Count == 2)
        {
            var uiThumbnail0 = list[0];
            var uiThumbnail1 = list[1];
            var bitmap0 = uiThumbnail0.Bitmap;
            var size0 = bitmap0.PixelSize;
            var bitmap1 = uiThumbnail1.Bitmap;
            var size1 = bitmap1.PixelSize;
            if ((size0.Width >= size0.Height) && (size1.Width >= size1.Height))
            {
                // Both landscape or square
                this.layoutKind = LayoutKind.DualImageLandscape;
                this.DualLandscapeImageLayoutIsVisible = true;
            }

            if ((size0.Width <= size0.Height) && (size1.Width <= size1.Height))
            {
                // Both portrait or square
                this.layoutKind = LayoutKind.DualImagePortrait;
                this.DualPortraitImageLayoutIsVisible = true;
            }

            if (this.DualLandscapeImageLayoutIsVisible || this.DualPortraitImageLayoutIsVisible)
            {
                Schedule.OnUiThread(
                    this.isShowHintsDualLandscapeFirstTime ? 15_000 : 4_000,
                    () =>
                    {
                        this.ShowHints = false;
                        this.isShowHintsDualLandscapeFirstTime = false;
                    },
                    DispatcherPriority.Background);

                this.SingleImageViewModelTopOrLeft =
                    new CullingImageViewModel(this, uiThumbnail0.Metadata, bitmap0);
                this.SingleImageViewModelBottomOrRight =
                    new CullingImageViewModel(this, uiThumbnail1.Metadata, bitmap1);

                // Done 
                return;
            }

            // For images not in same orientation, we use the 'Many' case 
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

    private void ClearImages()
    {
        this.SingleImageLayoutIsVisible = false;
        this.DualLandscapeImageLayoutIsVisible = false;
        this.DualPortraitImageLayoutIsVisible = false;
        this.ManyImagesLayoutIsVisible = false;

        this.SingleImageViewModel = null;
        this.SelectedImageMetadataViewModel = null;
        this.SingleImageViewModelTopOrLeft = null;
        this.SingleImageViewModelBottomOrRight = null;
        this.SelectedImages = [];
    }

    public void OnSelect(object selectedObject) { }

    [RelayCommand]
    public void OnAddStar()
    {
        if (this.layoutKind == LayoutKind.SingleImage && this.SingleImageViewModel is not null)
        {
            this.AddStarTo(this.SingleImageViewModel, isAddStar: true);
        }
    }

    [RelayCommand]
    public void OnRemoveStar()
    {
        if (this.layoutKind == LayoutKind.SingleImage && this.SingleImageViewModel is not null)
        {
            this.AddStarTo(this.SingleImageViewModel, isAddStar: false);
        }
    }

    [RelayCommand]
    public void OnReject()
    {
        if (this.layoutKind == LayoutKind.SingleImage && this.SingleImageViewModel is not null)
        {
            this.Remove(this.SingleImageViewModel);
        } 
    }

    [RelayCommand]
    public void OnSelectTop()
    {
        if (this.layoutKind != LayoutKind.DualImageLandscape)
        {
            return;
        }

        if (this.SingleImageViewModelTopOrLeft is null || this.SingleImageViewModelBottomOrRight is null)
        {
            return;
        }

        // Add Star to Top or Left, Remove Bottom , Select Top or Left 
        this.AddStarTo(this.SingleImageViewModelTopOrLeft, isAddStar: true);
        this.Remove(this.SingleImageViewModelBottomOrRight, SingleImageViewModelTopOrLeft);
    }

    [RelayCommand]
    public void OnSelectBottom()
    {
        if (this.layoutKind != LayoutKind.DualImageLandscape)
        {
            return;
        }

        if (this.SingleImageViewModelTopOrLeft is null || this.SingleImageViewModelBottomOrRight is null)
        {
            return;
        }

        // Add Star to Bottom or Right, Remove Top or Left, Select Bottom or Right
        this.AddStarTo(this.SingleImageViewModelBottomOrRight, isAddStar: true);
        this.Remove(this.SingleImageViewModelTopOrLeft, this.SingleImageViewModelBottomOrRight);
    }

    private void AddStarTo(CullingImageViewModel viewModel, bool isAddStar)
    {
        viewModel.ChangeRating(isAddStar);
        this.libraryManager.SaveMetadata(viewModel.Metadata);
    }

    private void Remove (CullingImageViewModel viewModel, CullingImageViewModel? viewModelToSelect = null )
    {
        // Remove Bottom or Right 
        var metadata = viewModel.Metadata;
        if (metadata.Rating >= 4)
        {
            // TODO
            // Ask before deleting highly rated image 
        }

        if (this.libraryManager.Remove(metadata))
        {
            // Remove the thumbnail from the film strip on the left 
            int index = this.FindIndexOf(viewModel);
            if ( index < 0 || index >= this.ImageThumbnails.Count)
            {
                return; 
            }

            UiThumbnail uiThumbnailToRemove = this.ImageThumbnails[index];
            int wasSelectedIndex = this.SelectedThumbnailIndex;
            this.ImageThumbnails.Remove(uiThumbnailToRemove);

            if (viewModelToSelect is null)
            {
                // Select previous in film strip, unless empty 
                if (this.ImageThumbnails.Count > 0)
                {
                    this.SelectedThumbnailIndex = Math.Max(0, wasSelectedIndex - 1);
                }
            } 
            else
            {
                int indexToSelect = this.FindIndexOf(viewModelToSelect);
                if (indexToSelect < 0 || indexToSelect >= this.ImageThumbnails.Count)
                {
                    return;
                }

                // Select specified in film strip, unless empty 
                if (this.ImageThumbnails.Count > 0)
                {
                    this.SelectedThumbnailIndex = indexToSelect;
                }
            }
        }
    }

    // Returns the Index in the film strip of this image 
    private int FindIndexOf(CullingImageViewModel viewModel)
    {
        for (int index = 0; index < this.ImageThumbnails.Count; ++index)
        {
            UiThumbnail thumbnail = this.ImageThumbnails[index];
            if (thumbnail.Metadata.FullPath.Equals(viewModel.Metadata.FullPath, StringComparison.InvariantCultureIgnoreCase))
            {
                return index;
            }
        }

        if (Debugger.IsAttached)
        {
            // Should never happen
            Debugger.Break();
        }

        return -1;
    }

    public void Receive(HotKeyMessage message)
    {
        if (this.layoutKind == LayoutKind.SingleImage)
        {
            switch (message.Key)
            {
                default:
                    return;

                case Key.Back: // Reject: Remove from Library 
                    this.OnReject();
                    break;

                case Key.Insert: // Add Star 
                    this.OnAddStar();
                    break;

                case Key.Delete: // Demote 
                    this.OnRemoveStar();
                    break;
            }
        }
        else if (this.layoutKind == LayoutKind.DualImageLandscape)
        {

        }
        else if (this.layoutKind == LayoutKind.DualImagePortrait)
        {

        }
    }
}
