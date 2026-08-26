namespace Lyt.PhotoPostPro.Workflow.Library;

using static Lyt.PhotoPostPro.Workflow.Library.LibraryViewModel;

public sealed partial class LibraryThumbnailsPanelViewModel :
    ViewModel<LibraryThumbnailsPanelView>,
    ISelectListener
{
    private readonly PhotoPostProModel photoPostProModel;
    private readonly LibraryViewModel libraryViewModel;

    [ObservableProperty]
    public partial bool SortOrder { get; set; }

    [ObservableProperty]
    public partial bool ShowRatingFilter { get; set; }

    [ObservableProperty]
    public partial bool ShowRatingControl { get; set; }

    [ObservableProperty]
    public partial bool ShowAll { get; set; }

    [ObservableProperty]
    public partial int Rating { get; set; }

    private ObservableCollection<LibraryThumbnailViewModel> Thumbnails { get; set; }

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
    }

    public bool IsEmpty => this.Thumbnails.Count == 0 ;

    public void SetViewingMode (Viewing viewing)
    {
        this.ShowRatingFilter = viewing == Viewing.Captured;
        this.ShowRatingControl = this.ShowRatingFilter && !this.ShowAll; 
    }

    public IEnumerable<string> GetUnratedThumbnailsPaths()
        =>   from thumb in this.Thumbnails
             // Filter out images already rated (0 => unrated) 
             where thumb.Metadata.Rating == 0
             // Reorder files by Date Captured 
             orderby thumb.Metadata.Captured ascending
             select thumb.Path;

    public void Populate(List<LibraryThumbnailViewModel> list)
    {
        this.Thumbnails.CollectionChanged -= (_, _) => this.FilterAndSort();
        var collection = new ObservableCollection<LibraryThumbnailViewModel>(list);
        this.Thumbnails = collection;
        this.Thumbnails.CollectionChanged += (_, _) => this.FilterAndSort();
        this.FilterAndSort();
    }

    public LibraryThumbnailViewModel? GetFirstDisplayed()
        => this.DisplayedThumbnails.FirstOrDefault();

    public void Remove(LibraryThumbnailViewModel thumbnail)
        // This should fire a collection changed event and trigger a sort
        => this.Thumbnails.Remove(thumbnail);

    public void Update(string path)
    {
        // Find old View model using the provided path and remove it 
        var oldVm = 
            (from vm in this.Thumbnails 
             where vm.Path.Equals( path, StringComparison.InvariantCultureIgnoreCase) 
             select vm).FirstOrDefault();
        if (oldVm is null)
        {
            return;
        }

        this.Thumbnails.Remove(oldVm);

        // Bring in the new one 
        if (this.photoPostProModel.LibraryManager.LoadedThumbnails.TryGetValue(path, out var thumbnail))
        {
            // Add to list 
            LibraryThumbnailViewModel libraryThumbnailViewModel =
                new(this, path, thumbnail.Metadata, thumbnail.ImageBytes);
            this.Thumbnails.Add(libraryThumbnailViewModel);
        }
    }

    public bool TryFindThumbnail(Metadata metadata, [NotNullWhen(true)] out LibraryThumbnailViewModel? foundThumbnail)
    {
        foundThumbnail = null;
        foreach (var thumbnail in this.Thumbnails)
        {
            if (thumbnail.Metadata.FullPath.Equals(metadata.FullPath, StringComparison.InvariantCultureIgnoreCase))
            {
                foundThumbnail = thumbnail;
                return true;
            }
        }

        return false;
    }

    partial void OnSortOrderChanged(bool value) => this.FilterAndSort();

    partial void OnShowAllChanged(bool value)
    {
        // If we moved this setting, we can only be viewing in Captured Mode 
        this.SetViewingMode(Viewing.Captured); 
        this.FilterAndSort();
    } 

    partial void OnRatingChanged(int value) => this.FilterAndSort();

    private void FilterAndSort()
    {
        if (this.Thumbnails.Count == 0)
        {
            this.DisplayedThumbnails.Clear();
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
