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
    public partial List<UiThumbnail> SelectedImages { get; set; } = [];    

    [ObservableProperty]
    public partial ObservableCollection<UiThumbnail> SelectedThumbnails { get; set; } = [];

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

    private void OnSelectedThumbnailsChanged( object? sender, SelectionChangedEventArgs e)
    {
        if (!this.IsActivated || sender is null)
        {
            return;
        }

        this.SelectedImages = this.SelectedThumbnails.ToList(); 
    }
}
