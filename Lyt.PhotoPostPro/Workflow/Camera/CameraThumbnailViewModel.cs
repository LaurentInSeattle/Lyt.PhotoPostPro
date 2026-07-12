namespace Lyt.PhotoPostPro.Workflow.Camera;

public sealed partial class CameraThumbnailViewModel : 
    ViewModel<CameraThumbnailView>, 
    IRecipient<LanguageChangedMessage>
{
    public const double LargeBorderHeight = 260;
    public const double LargeImageHeight = 200;
    public const int LargeThumbnailWidth = 360;

    public readonly Metadata Metadata;

    private readonly ISelectListener parent;

    [ObservableProperty]
    public partial double BorderHeight { get; set; }

    [ObservableProperty]
    public partial double ImageHeight { get; set; }

    [ObservableProperty]
    public partial string Title { get; set; }

    [ObservableProperty]
    public partial string Details { get; set; }

    [ObservableProperty]
    public partial WriteableBitmap Thumbnail { get; set; }

    /// <summary>  Creates a thumbnail view model </summary>
    public CameraThumbnailViewModel(ISelectListener parent, Metadata metadata, byte[] imageBytes)
    {
        this.parent = parent;
        this.Metadata = metadata;
        this.BorderHeight = LargeBorderHeight;
        this.ImageHeight = LargeImageHeight;
        this.Thumbnail = WriteableBitmap.Decode(new MemoryStream(imageBytes));

        this.Title = string.Empty; 
        this.Details = string.Empty;
        this.SetThumbnailStrings(); 
        this.Subscribe<LanguageChangedMessage>();
    }

    // We need to reload the thumbnail view title, so that it will be properly localized
    public void Receive(LanguageChangedMessage _) => this.SetThumbnailStrings();

    internal void OnSelect() => this.parent.OnSelect(this);

    //internal void ShowDeselected(Model.GameObjects.Game game)
    //{
    //    if (this.Game == game)
    //    {
    //        return;
    //    }

    //    if (this.IsBound)
    //    {
    //        this.View.Deselect();
    //    }
    //}

    //internal void ShowSelected()
    //{
    //    if (this.IsBound)
    //    {
    //        this.View.Select();
    //    }
    //}

    private void SetThumbnailStrings()
    {
        string? currentLanguage = this.Localizer.CurrentLanguage;
        if (!string.IsNullOrEmpty(currentLanguage))
        {
            Thread.CurrentThread.CurrentCulture = new CultureInfo(currentLanguage);
            Thread.CurrentThread.CurrentUICulture = new CultureInfo(currentLanguage);
        }

        this.Title =
            string.Format(
                "{0} - {1} - {2}",
                this.Metadata.Filename, this.Metadata.Extension, this.Metadata.Dimensions);
        
        if (this.Metadata.HasExifMetadata)
        {
            var captured = this.Metadata.Captured; 
            this.Details = captured.ToLongDateString() + " " + captured.ToShortTimeString();
        }

        //string dateString =
        //    string.Format(this.Localize("Collection.Thumbs.StartedFormat"), this.Game.Started.Date.ToShortDateString());
        //string progressString =
        //    this.Game.IsCompleted ?
        //        string.Empty :
        //        string.Format(this.Localize("Collection.Thumbs.ProgressFormat"), this.Game.Progress);
        //this.Title =
        //    this.Game.IsCompleted ?
        //        dateString :
        //        string.Concat(dateString, " - ", progressString);
        //this.Details = string.Format(this.Localize("Collection.Thumbs.PuzzleFormat"), this.Game.PieceCount);
    }
}
