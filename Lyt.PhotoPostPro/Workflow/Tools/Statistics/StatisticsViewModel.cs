namespace Lyt.PhotoPostPro.Workflow.Tools.Statistics;

public sealed partial class StatisticsViewModel : ViewModel<StatisticsView>
{
    private const float MegaByte = 1024.0f * 1024.0f;
    private const float MinimumDiskAvailableMegaByte = 80.0f * 1024.0f; // 8 GB 

    private readonly PhotoPostProModel model;

    private float availableMegabytes;

    [ObservableProperty]
    public partial string AvailableSpace { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string DiskSpaceAlert { get; set; } = string.Empty;

    public StatisticsViewModel(PhotoPostProModel model)
    {
        this.model = model;
    }

    public override void Activate(object? activationParameters)
    {
        base.Activate(activationParameters);

        if (FileSystemExtensions.DriveInfo(this.model.RootPath) is DriveInfo driveInfo)
        {
            long availableBytes = driveInfo.AvailableFreeSpace;
            this.availableMegabytes = (float)availableBytes / MegaByte;
            string name = driveInfo.Name;
            string label = driveInfo.VolumeLabel;
            string diskSpace = Metadata.DiskSpaceString(this.availableMegabytes);
            string diskSpaceFormat = this.Localize("Workflow.Import.Folder.DriveInfoFormat");
            this.AvailableSpace = string.Format(diskSpaceFormat, name, label, diskSpace);

            bool lowDisk = this.availableMegabytes - MinimumDiskAvailableMegaByte < 0.0f;
            this.DiskSpaceAlert = lowDisk? this.Localize("Tools.Statistics.LowDisk") : string.Empty;
        }
    }

}
