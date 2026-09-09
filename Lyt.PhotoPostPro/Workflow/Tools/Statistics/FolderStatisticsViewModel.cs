namespace Lyt.PhotoPostPro.Workflow.Tools.Statistics;

public sealed partial class FolderStatisticsViewModel : ViewModel<FolderStatisticsView>
{
    private readonly PhotoPostProModel model;

    private string navigationPath;

    [ObservableProperty]
    public partial string FolderName { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string SizeOnDisk { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string TotalFileCount { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string ImageFileCount { get; set; } = string.Empty;

    public FolderStatisticsViewModel(PhotoPostProModel model)
    {
        this.model = model;
        this.navigationPath = string.Empty;
    }

    public void Update(FolderStatistics folderStatistics)
    {
        this.navigationPath = folderStatistics.Path;
        this.FolderName = this.Localize(folderStatistics.LocalizationKey);
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
