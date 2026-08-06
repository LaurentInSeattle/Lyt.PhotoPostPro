namespace Lyt.PhotoPostPro.Workflow.Culling;

// Do not add those ImageSharp namespaces to global using as some class definitions conflict
// with the ones from Avalonia. (Point, Rectangle, etc.) 
//using SixLabors.ImageSharp;
//using SixLabors.ImageSharp.PixelFormats;

public sealed partial class CullingViewModel : ViewModel<CullingView>
{
    public sealed record class UiThumbnail(string Key, WriteableBitmap Bitmap); 

    private readonly PhotoPostProModel model;
    private readonly LibraryManager libraryManager;
    private readonly IToaster toaster;

    private readonly Dictionary<string, UiThumbnail> allHdImages = [];

    [ObservableProperty]
    public partial UiThumbnail? SelectedThumbnail { get; set; }

    [ObservableProperty]
    public partial bool HasSelection { get; set; }

    [ObservableProperty]
    public partial List<UiThumbnail> ImageThumbnails { get; set; } = [];

    [ObservableProperty]
    public partial ObservableCollection<UiThumbnail> SelectedThumbnails { get; set; } = [];

    [ObservableProperty]
    public partial ObservableCollection<UiThumbnail> SelectedImages { get; set; } = [];

    public CullingViewModel(PhotoPostProModel model, IToaster toaster)
    {
        this.model = model;
        this.libraryManager = model.LibraryManager;
        this.toaster = toaster;
    }

    public override void Activate(object? activationParameters)
    {
        base.Activate(activationParameters);
        this.View.StripListBox.SelectionChanged += this.OnSelectedThumbnailsChanged;
    }

    public override void Deactivate()
    {
        this.View.StripListBox.SelectionChanged -= this.OnSelectedThumbnailsChanged;
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
                list[index] = new UiThumbnail(file, thumbnail);
                pathList.Add(loadedThumbnail.Metadata.FullPath);
            }
        });

        // We may have 'holes' in the list if some files failed to load, so we filter them out
        list = list.Where(t => t is not null).ToList();

        // ! 'holes' have been filtered out, so we can safely cast to non-nullable type
        this.ImageThumbnails = list!;

        Task.Run(() =>
        {
            Thread.CurrentThread.Name = "CullingViewModel.LoadHdImages";

            // Delay so the UI can render the thumbnails first
            Task.Delay(420).Wait();

            // Then load HD images in the background
            this.LoadHdImages(pathList);
        });

    }

    private void LoadHdImages(List<string> pathList)
    {
        Parallel.For(0, pathList.Count, index =>
        {
            if (0 == index % 2)
            {
                // throttle 
                Task.Delay(120).Wait();
            }

            string path = pathList[index];
            LoadedImage? loadedHdImage = ImageLoader.LoadHdImage(path);
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
                        this.allHdImages.Add(key, new UiThumbnail(key, bitmap));
                    }
                }

                // throttle 
                Task.Delay(120).Wait();
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
                list.Add(hdImage    );
            }
            else
            {
                list.Add(selectedThumbnail);
            }
        }

        this.SelectedImages = new(list);
    }

    private void ClearAllCollections()
    {
        this.allHdImages.Clear();
        this.ImageThumbnails.Clear();
        this.SelectedThumbnails = [];
        this.SelectedImages = [];
    }
}
