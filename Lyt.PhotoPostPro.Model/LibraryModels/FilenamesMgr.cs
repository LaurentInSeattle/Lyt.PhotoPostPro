namespace Lyt.PhotoPostPro.Model.LibraryModels;

internal static class FilenamesMgr
{
    public static readonly string ProjectsFolder = "Projects";

    // URL-Safe Base64
    public static string NewShortId()
        => Convert
            .ToBase64String(Guid.NewGuid().ToByteArray())
            .Replace("/", "_")   // Make URL safe
            .Replace("+", "-")   // Make URL safe
            .Replace("=", "");   // Remove Base64 padding
}
