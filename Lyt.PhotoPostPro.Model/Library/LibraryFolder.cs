namespace Lyt.PhotoPostPro.Model.Library;

using System.IO;

public sealed class LibraryFolder
{
    public string FriendlyName { get; set; } = string.Empty;

    public string FullPath { get; set; } = string.Empty;

    public const string DefaultLibraryFolderName = "Default";

    private static LibraryFolder DefaultLibraryFolder
        => new()
        {
            FriendlyName = DefaultLibraryFolderName,
            FullPath =
                    Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.MyPictures),
                        PhotoPostProModel.PhotoPostProAppName,
                        LibraryManager.LibraryFolderName),
        };

    public static LibraryFolder Default => DefaultLibraryFolder;
}
