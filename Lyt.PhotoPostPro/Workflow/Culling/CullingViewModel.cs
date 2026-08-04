namespace Lyt.PhotoPostPro.Workflow.Culling;

// Do not add those ImageSharp namespaces to global using as some class definitions conflict
// with the ones from Avalonia. (Point, Rectangle, etc.) 
//using SixLabors.ImageSharp;
//using SixLabors.ImageSharp.PixelFormats;

public sealed partial class CullingViewModel : ViewModel<CullingView>
{
    public sealed class UiThumbnail(string key, WriteableBitmap bitmap)
    {
        public string Key { get; } = key;

        public WriteableBitmap Bitmap { get; } = bitmap;
    }


    private readonly PhotoPostProModel model;
    private readonly LibraryManager libraryManager;
    private readonly IToaster toaster;

    private readonly List<LoadedThumbnail> allThumbnails = [];

    [ObservableProperty]
    public partial UiThumbnail? SelectedThumbnail { get; set; }

    [ObservableProperty]
    public partial bool HasSelection { get; set; }

    [ObservableProperty]
    public partial List<UiThumbnail> ImageThumbnails { get; set; } = [];

    [ObservableProperty]
    public partial ObservableCollection<UiThumbnail> SelectedThumbnails { get; set; } = [];

    public CullingViewModel(PhotoPostProModel model, IToaster toaster)
    {
        this.model = model;
        this.libraryManager = model.LibraryManager;
        this.toaster = toaster;

        this.SelectedThumbnails.CollectionChanged += (s, e) => this.OnSelectedThumbnailsChanged(e);
    }

    public override void Activate(object? activationParameters)
    {
        base.Activate(activationParameters);
    }

    public override void Deactivate()
    {
        this.allThumbnails.Clear();
        this.ImageThumbnails.Clear();
        this.SelectedThumbnails.Clear();
        base.Deactivate();
    }

    internal void Initialize(List<string> files)
    {
        var list = new List<UiThumbnail>();
        foreach (string file in files)
        {
            if (this.libraryManager.LoadedThumbnails.TryGetValue(file, out var loadedThumbnail))
            {
                this.allThumbnails.Add(loadedThumbnail);
                var thumbnail = WriteableBitmap.Decode(new MemoryStream(loadedThumbnail.ImageBytes));
                list.Add(new UiThumbnail(file , thumbnail));
            }
        }

        this.ImageThumbnails = list;
    }

    private void OnSelectedThumbnailsChanged(NotifyCollectionChangedEventArgs e)
    {
        if (!this.IsActivated)
        {
            return;
        }

        if (e.NewItems is not null)
        {
            Debug.WriteLine(" OnSelectedThumbnailsChanged: New: " + e.NewItems.Count);
        }

        if (e.OldItems is not null)
        {
            Debug.WriteLine(" OnSelectedThumbnailsChanged: Old: " + e.OldItems.Count);
        }
    }
}
