namespace Lyt.PhotoPostPro.Workflow.Tools.Statistics;

public sealed partial class FolderStatisticsViewModel(PhotoPostProModel model) : ViewModel<FolderStatisticsView>
{
    private readonly PhotoPostProModel model = model;

    private string navigationPath = string.Empty;

    [ObservableProperty]
    public partial string FolderName { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string SizeOnDisk { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string TotalFileCount { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string ImageFileCount { get; set; } = string.Empty;

    public void Update(FolderStatistics folderStatistics)
    {
        this.navigationPath = folderStatistics.Path;
        string folderName = this.Localize(folderStatistics.LocalizationKey);
        var libraryFolders = this.model.LibraryFolders;
        if (folderStatistics.LocalizationKey.Contains("Library") && (libraryFolders.AvailableLibraryFolders.Count > 1))
        {
            // More than one library folder, so we need to show which library this is
            string libraryName = libraryFolders.SelectedLibraryFolderName;
            this.FolderName = $"{folderName} ({libraryName})";
        }
        else
        {
            // Not the library statistics, or only one library folder, so we don't need to show extra info
            this.FolderName = this.Localize(folderStatistics.LocalizationKey);
        }

        string sizeOnDisk = Metadata.DiskSpaceString(folderStatistics.TotalSizeOnDiskMB);
        this.SizeOnDisk = sizeOnDisk;
        this.ImageFileCount = folderStatistics.ImageFileCount.ToString("D");
        this.TotalFileCount = folderStatistics.TotalFileCount.ToString("D");
    }

    [RelayCommand]
    public void OnFolderNavigate()
    {
        if (string.IsNullOrWhiteSpace(this.navigationPath))
        {
            return;
        }

        PhotoPostProModel.NavigateTo(this.navigationPath);
    }
}
