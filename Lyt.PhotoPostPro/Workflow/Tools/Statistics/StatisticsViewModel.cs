namespace Lyt.PhotoPostPro.Workflow.Tools.Statistics;

public sealed partial class StatisticsViewModel : 
    ViewModel<StatisticsView>, IRecipient<SystemStatisticsMessage>
{
    private const float MegaByte = 1024.0f * 1024.0f;
    private const float MinimumDiskAvailableMegaByte = 8.0f * 1024.0f; // 8 GB 

    private readonly PhotoPostProModel model;

    private float availableMegabytes;

    [ObservableProperty]
    public partial string AvailableSpace { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string DiskSpaceAlert { get; set; } = string.Empty;

    [ObservableProperty]
    public partial FolderStatisticsViewModel LibraryStatistics { get; set; }

    [ObservableProperty]
    public partial FolderStatisticsViewModel ExportsStatistics { get; set; }

    [ObservableProperty]
    public partial FolderStatisticsViewModel GalleryStatistics { get; set; }

    [ObservableProperty]
    public partial FolderStatisticsViewModel LutsStatistics { get; set; }

    public StatisticsViewModel(PhotoPostProModel model)
    {
        this.model = model;
        this.Subscribe<SystemStatisticsMessage>(); 
        this.LibraryStatistics = new FolderStatisticsViewModel(model);
        this.ExportsStatistics = new FolderStatisticsViewModel(model);
        this.GalleryStatistics = new FolderStatisticsViewModel(model);
        this.LutsStatistics = new FolderStatisticsViewModel(model);
    }

    public override void Activate(object? activationParameters)
    {
        base.Activate(activationParameters);
        ImageLoader.CollectStatistics(this.model); 
    }

    public void Receive(SystemStatisticsMessage message) =>
        Dispatch.OnUiThread(() =>
        {
            this.ReceiveOnUiThread(message);
        }, DispatcherPriority.Background);  
    
    public void ReceiveOnUiThread(SystemStatisticsMessage message)
    {
        var driveInfo = message.DriveStatistics; 
        long availableBytes = driveInfo.AvailableFreeSpace;
        this.availableMegabytes = (float)availableBytes / MegaByte;
        string name = driveInfo.Name;
        string label = driveInfo.VolumeLabel;
        string diskSpace = Metadata.DiskSpaceString(this.availableMegabytes);
        string diskSpaceFormat = this.Localize("Workflow.Import.Folder.DriveInfoFormat");
        this.AvailableSpace = string.Format(diskSpaceFormat, name, label, diskSpace);

        bool lowDisk = this.availableMegabytes - MinimumDiskAvailableMegaByte < 0.0f;
        this.DiskSpaceAlert = lowDisk ? this.Localize("Tools.Statistics.LowDisk") : string.Empty;

        this.LibraryStatistics.Update(message.LibraryStatistics);
        this.ExportsStatistics.Update(message.ExportsStatistics);
        this.GalleryStatistics.Update(message.GalleryStatistics);
        this.LutsStatistics.Update(message.LutsStatistics);
    }
}
