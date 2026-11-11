namespace Lyt.PhotoPostPro.Model.Library;

using System.IO;

public sealed class LibraryFolder : IEditable
{
    public string FriendlyName { get; set; } = string.Empty;

    public string FullPath { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public const string DefaultLibraryFolderName = "Default";

    private static LibraryFolder DefaultLibraryFolder
        => new()
        {
            FriendlyName = DefaultLibraryFolderName,
            Description = "The Default Library Folder cannot be deleted or modified.",
            FullPath =
                    Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.MyPictures),
                        PhotoPostProModel.PhotoPostProAppName,
                        LibraryManager.LibraryFolderName),
        };

    public static LibraryFolder Default => DefaultLibraryFolder;
}
