namespace Lyt.PhotoPostPro.Model.Messaging;

public sealed record class DevicesFoundMessage(List<FoundDevice> Devices);

public sealed record class DeviceStatusMessage(bool IsConnected, FoundDevice Device);

public sealed record class DeviceFileListMessage(FoundDevice Device, List<string> Files);

public sealed record class DeviceFileDownloadedMessage(
    bool IsSuccess, 
    FoundDevice Device, 
    string File, 
    string Path, 
    Metadata? Metadata = null,
    byte[]? ThumbnailBytes = null, 
    string ThumbnailPath = "");

public sealed record class DeviceDownloadCompleteMessage(
    FoundDevice Device,
    bool Completed,
    int FileCount,
    int DownloadedCount,
    int ErrorCount);

public sealed record class DeviceDeleteCompleteMessage(
    FoundDevice Device,
    bool Completed,
    int FileCount,
    int DeletedCount,
    int ErrorCount);
