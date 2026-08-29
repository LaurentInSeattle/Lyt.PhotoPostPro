namespace Lyt.PhotoPostPro.Workflow.Import.Folder;

using Lyt.FileSystem;

// Do not add those ImageSharp namespaces to global using as some class definitions conflict
// with the ones from Avalonia. (Point, Rectangle, etc.) 
//using SixLabors.ImageSharp;
//using SixLabors.ImageSharp.PixelFormats;

public sealed partial class FolderImportViewModel : ViewModel<FolderImportView>
{
    private const float MegaByte = 1024.0f * 1024.0f;

    private readonly PhotoPostProModel model;
    private readonly IToaster toaster;

    private FolderStatistics statistics;
    private DispatcherTimer? timer;

    [ObservableProperty]
    public partial bool IsFolderMode { get; set; }

    [ObservableProperty]
    public partial string Message { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool IsMessageVisible { get; set; }

    [ObservableProperty]
    public partial bool AreStatisticsVisible { get; set; }

    [ObservableProperty]
    public partial ObservableCollection<ImageCategoryViewModel> ImageCategories { get; set; }

    [ObservableProperty]
    public partial string TotalSpaceRequiredString { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string AvailableSpace { get; set; } = string.Empty;

    private float totalSpaceRequiredMB;
    private float availableMegabytes;

    public FolderImportViewModel(PhotoPostProModel model, IToaster toaster)
    {
        this.model = model;
        this.toaster = toaster;
        this.statistics = new FolderStatistics(string.Empty);

        this.IsFolderMode = false;
        this.IsMessageVisible = false;
        this.AreStatisticsVisible = false;
        this.ImageCategories = new();
    }

    public void OnFolderDrop(string path)
    {
        this.IsMessageVisible = false;
        this.AreStatisticsVisible = false;
        this.statistics.Clear();
        this.statistics = ImageLoader.InspectFolder(path);
        if (this.statistics.IsEmpty)
        {
            // Empty 
            this.ShowMessage("Workflow.Import.Folder.Empty");
        }
        else if (this.statistics.HasNoImage)
        {
            // With files but no images 
            this.ShowMessage("Workflow.Import.Folder.WithFilesNoImages");
        }
        else if (this.statistics.Success)
        {
            if (FileSystemExtensions.DriveInfo(this.model.RootPath) is DriveInfo driveInfo)
            {
                this.totalSpaceRequiredMB = 0.0f;
                this.TotalSpaceRequiredString = string.Empty;
                long availableBytes = driveInfo.AvailableFreeSpace;
                this.availableMegabytes = (float)availableBytes / MegaByte;
                string name = driveInfo.Name;
                string label = driveInfo.VolumeLabel;
                // string format = "Workflow.Import.Folder.DriveInfo";
                this.TotalSpaceRequiredString = "Required Space: n/a";
                string diskSpace = this.DiskSpaceString(this.availableMegabytes); 
                this.AvailableSpace =
                    string.Format( "Available Space on {0} ({1}): {2}", name, label, diskSpace);
            }

            this.ShowStatistics();
        }
        else
        {
            // Error ? 
            this.ShowMessage("Workflow.Import.Folder.ErrorWhileCollecting");
        }
    }

    private void ShowMessage(string message)
    {
        this.Message = this.Localize(message);
        this.IsMessageVisible = true;
        this.AreStatisticsVisible = false;

        // Launch Timer to go back automatically after 10 seconds 
        this.timer = new DispatcherTimer()
        {
            Interval = TimeSpan.FromSeconds(7),
        };
        timer.Tick += this.OnTimerTick;
        timer.Start();
    }

    private void OnTimerTick(object? sender, EventArgs e)
    {
        this.DestroyTimer();
        this.GoBack();
    }

    private void DestroyTimer()
    {
        this.timer?.Stop();
        this.timer = null;
    }

    private void ShowStatistics()
    {
        this.Message = string.Empty;
        this.IsMessageVisible = false;
        this.AreStatisticsVisible = true;

        string path = this.statistics.Path;
        var list = new List<ImageCategoryViewModel>(this.statistics.ImageStatistics.Count);
        foreach (var imageCategory in this.statistics.ImageStatistics)
        {
            list.Add(new ImageCategoryViewModel(this, path, imageCategory));
        }

        this.ImageCategories = new(list);
    }

    internal void OnImportSelectionChanged(ImageCategoryViewModel vm, bool shouldImport)
    {
        if (shouldImport)
        {
            this.totalSpaceRequiredMB += vm.ImageStatistics.SizeOnDiskMB;

        }
        else
        {
            this.totalSpaceRequiredMB -= vm.ImageStatistics.SizeOnDiskMB;
        }

        string diskSpace = this.DiskSpaceString(this.totalSpaceRequiredMB);
        this.TotalSpaceRequiredString = string.Format("Required Space: {0}", diskSpace);

        if (this.totalSpaceRequiredMB > this.availableMegabytes)
        {
            // Alert 
            // Hide Import buttons 
        }
    }

    private void GoBack()
    {
        // Go back to import drop screen 
        var importVm = App.GetRequiredService<ImportViewModel>();
        importVm.SetInitialState();
    }

#pragma warning disable CA1822 // Mark members as static
    // RelayCommand's cannot be static 

    [RelayCommand]
    public void OnBack() => this.GoBack();
#pragma warning restore CA1822

    private string DiskSpaceString(float megabytes)
    {
        if (megabytes <= 0.0)
        {
            return "n/a";
        }

        bool isBig = megabytes > 1999.999f;
        string unit = isBig ? "GB" : "MB";
        if (isBig)
        {
            megabytes /= 1024.0f;
        }

        bool isHuge = megabytes > 1999.999f;
        if (isHuge)
        {
            unit = "TB";
            megabytes /= 1024.0f;
        }

        return string.Format("{0:F1} {1}", megabytes, unit);
    }
}
