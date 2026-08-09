namespace Lyt.PhotoPostPro.Workflow.Culling;

public sealed partial class CullingImageViewModel :
    ViewModel<LibraryThumbnailView>,
    IRecipient<LanguageChangedMessage>
{
    public readonly Metadata Metadata;

    private readonly ISelectListener parent;

    [ObservableProperty]
    public partial int Rating { get; set; }

    [ObservableProperty]
    public partial string Title { get; set; }

    [ObservableProperty]
    public partial string Details { get; set; }

    [ObservableProperty]
    public partial WriteableBitmap Image { get; set; }

    /// <summary>  Creates a thumbnail view model </summary>
    public CullingImageViewModel(ISelectListener parent, Metadata metadata, WriteableBitmap image)
    {
        this.parent = parent;
        this.Metadata = metadata;
        this.Image = image;

        this.Rating = metadata.Rating; 
        this.Title = string.Empty;
        this.Details = string.Empty;
        this.SetThumbnailStrings();
        this.Subscribe<LanguageChangedMessage>();
    }

    public bool ChangeRating (bool isAddStar)
    {
        int value = this.Metadata.Rating; 
        if ( isAddStar && value <=5 )
        {
            ++value; 
        }
        else if ( value > 0)
        {
            --value; 
        }
        else
        {
            return false ; 
        }

        this.Metadata.Rating = value; 
        this.Rating = value;
        return true;
    }

    // We need to reload the thumbnail view title, so that it will be properly localized
    public void Receive(LanguageChangedMessage _) => this.SetThumbnailStrings();

    internal void OnSelect() => this.parent.OnSelect(this);

#pragma warning disable CA1822 // Mark members as static
    // Relay commands cannot be static 

    [RelayCommand]
    public void OnIsToAddToLibraryChanged()
    {

    }

    [RelayCommand]
    public void OnIsToRemoveFromCameraChanged()
    {

    }

#pragma warning restore CA1822 // Mark members as static

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
        else
        {
            var fileDate = this.Metadata.FileDateUTC;
            string fileLabel = this.Localize("Workflow.Library.Thumbnail.File");
            this.Details = 
                string.Format ( "{0} {1} {2}", fileLabel, fileDate.ToLongDateString() , fileDate.ToShortTimeString());
        }
    }

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

}

