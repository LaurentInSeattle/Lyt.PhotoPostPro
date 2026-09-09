namespace Lyt.PhotoPostPro.Model.Messaging;

public sealed record class SystemStatisticsMessage
(
    DriveStatistics DriveStatistics,
    FolderStatistics LibraryStatistics,
    FolderStatistics GalleryStatistics,
    FolderStatistics ExportsStatistics,
    FolderStatistics LutsStatistics); 
