namespace Lyt.PhotoPostPro.Workflow.Library;

// using static Lyt.Persistence.FileManagerModel;

public sealed partial class LibraryThumbnailsPanelViewModel :
    ViewModel<LibraryThumbnailsPanelView>,
    ISelectListener,
    IRecipient<LanguageChangedMessage>
{
    private readonly PhotoPostProModel photoPostProModel;
    private readonly LibraryViewModel cameraViewModel;

    [ObservableProperty]
    public partial bool SortOrder { get; set; }

    [ObservableProperty]
    public partial ObservableCollection<LibraryThumbnailViewModel> Thumbnails { get; set; }

    [ObservableProperty]
    public partial int ProvidersSelectedIndex { get; set; }

    [ObservableProperty]
    public partial string EmptyMessage { get; set; }

    public LibraryThumbnailsPanelViewModel(PhotoPostProModel photoPostProModel, LibraryViewModel collectionViewModel)
    {
        this.photoPostProModel = photoPostProModel;
        this.cameraViewModel = collectionViewModel;
        this.Thumbnails = [];
        this.EmptyMessage = string.Empty;
        this.SortOrder = true; 
        this.Subscribe<LanguageChangedMessage>();
    }

    public void Receive(LanguageChangedMessage _) { } //  => this.PopulateComboBox();

    public void Sort(bool ascending = true)
    {
        if (this.Thumbnails.Count == 0)
        {
            return;
        }

        if (ascending)
        {
            var sorted =
                (from thumb in this.Thumbnails
                 orderby thumb.Metadata.Captured ascending
                 select thumb).ToList();
            this.Thumbnails = new(sorted);
        }
        else
        {
            var sorted =
                (from thumb in this.Thumbnails
                 orderby thumb.Metadata.Captured descending
                 select thumb).ToList();
            this.Thumbnails = new(sorted);
        }
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

    partial void OnSortOrderChanged(bool value) => this.Sort(ascending: value); 

}
