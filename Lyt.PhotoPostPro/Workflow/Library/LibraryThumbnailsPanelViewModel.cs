namespace Lyt.PhotoPostPro.Workflow.Library;

public sealed partial class LibraryThumbnailsPanelViewModel :
    ViewModel<LibraryThumbnailsPanelView>,
    ISelectListener,
    IRecipient<LanguageChangedMessage>
{
    private readonly PhotoPostProModel photoPostProModel;
    private readonly LibraryViewModel libraryViewModel;

    [ObservableProperty]
    public partial bool SortOrder { get; set; }

    [ObservableProperty]
    public partial bool ShowAll { get; set; }

    [ObservableProperty]
    public partial int Rating { get; set; }

    public ObservableCollection<LibraryThumbnailViewModel> Thumbnails { get; set; }

    [ObservableProperty]
    public partial ObservableCollection<LibraryThumbnailViewModel> DisplayedThumbnails { get; set; }

    [ObservableProperty]
    public partial string EmptyMessage { get; set; }

    public LibraryThumbnailsPanelViewModel(PhotoPostProModel photoPostProModel, LibraryViewModel libraryViewModel)
    {
        this.photoPostProModel = photoPostProModel;
        this.libraryViewModel = libraryViewModel;
        this.Thumbnails = [];
        this.DisplayedThumbnails = [];
        this.EmptyMessage = string.Empty;
        this.SortOrder = true;
        this.ShowAll = true;
        this.Rating = 1;
        this.Thumbnails.CollectionChanged += (_,_) => this.Sort(); 

        this.Subscribe<LanguageChangedMessage>();
    }

    public void Receive(LanguageChangedMessage _) { } 

    partial void OnSortOrderChanged(bool value) => this.Sort();

    partial void OnShowAllChanged(bool value) => this.Sort();

    partial void OnRatingChanged(int value) => this.Sort();

    public void Sort()
    {
        if (this.Thumbnails.Count == 0)
        {
            return;
        }

        IEnumerable<LibraryThumbnailViewModel> sorted; 
        if (this.ShowAll)
        {
            if (this.SortOrder)
            {
                sorted =
                    (from thumb in this.Thumbnails
                     orderby thumb.Metadata.Captured ascending
                     select thumb);
            }
            else
            {
                sorted =
                    (from thumb in this.Thumbnails
                     orderby thumb.Metadata.Captured descending
                     select thumb);
            }
        } 
        else
        {
            if (this.SortOrder)
            {
                sorted =
                    (from thumb in this.Thumbnails
                     where thumb.Metadata.Rating >= this.Rating
                     orderby thumb.Metadata.Captured ascending
                     select thumb);
            }
            else
            {
                sorted =
                    (from thumb in this.Thumbnails
                     where thumb.Metadata.Rating >= this.Rating
                     orderby thumb.Metadata.Captured descending
                     select thumb);
            }
        }

        this.DisplayedThumbnails = new(sorted);
    }

    public void OnSelect(object selectedObject)
    {
        //if (selectedObject is ThumbnailViewModel thumbnailViewModel)
        //{
        //    this.selectedThumbnail = thumbnailViewModel;
        //    var game = thumbnailViewModel.Game;
        //    if (this.selectedGame is null || this.selectedGame.Name != game.Name)
        //    {
        //        this.selectedGame = game;
        //        this.collectionViewModel.Select(game);
        //    }

        //    this.UpdateVisualSelection();
        //}
    }

    internal void UpdateVisualSelection()
    {
        //if (this.selectedGame is not null)
        //{
        //    foreach (ThumbnailViewModel thumbnailViewModel in this.Thumbnails)
        //    {
        //        if (thumbnailViewModel.Game == this.selectedGame)
        //        {
        //            thumbnailViewModel.ShowSelected();
        //        }
        //        else
        //        {
        //            thumbnailViewModel.ShowDeselected(this.selectedGame);
        //        }
        //    }
        //}
    }

}
