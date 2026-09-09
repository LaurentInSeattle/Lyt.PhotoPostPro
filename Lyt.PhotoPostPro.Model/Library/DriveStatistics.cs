namespace Lyt.PhotoPostPro.Model.Library;

public sealed record class DriveStatistics(
    long AvailableFreeSpace, string Name = "", string VolumeLabel = "");
