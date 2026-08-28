namespace Lyt.PhotoPostPro.Model.Loader;

using System.IO;

public static partial class ImageLoader
{
    #region Extensions Definitions

#pragma warning disable CA2211 // Non-constant fields should not be visible

    public static List<string> ExcludedExtensions = [".aae", ".docx", ".xlsx", ".pdf"];

    public static bool HasExcludedExtension(string path)
        => ExcludedExtensions.Contains(System.IO.Path.GetExtension(path).ToLower());

    public static List<string> MovieExtensions = [".mp4", ".mov", ".mkv", ".avi", ".webm"];

    public static bool HasMovieExtension(string path)
        => MovieExtensions.Contains(System.IO.Path.GetExtension(path).ToLower());

    public static List<string> HeifExtensions = [".heic", ".heif", ".hif"];

    public static bool HasHiefExtension(string path)
        => HeifExtensions.Contains(System.IO.Path.GetExtension(path).ToLower());

    // https://en.wikipedia.org/wiki/Raw_image_format
    // Many raw file formats, including IIQ (Phase One), 3FR (Hasselblad), DCR, K25, KDC (Kodak),
    // CRW, CR2 (Canon), ERF (Epson), MEF (Mamiya), MOS (Leaf), NEF NRW (Nikon), ORF (Olympus),
    // PEF (Pentax), RW2 (Panasonic) and ARW, SRF, SR2 (Sony), are based on TIFF, the Tag Image
    // File Format.[2]
    //
    // These files may deviate from the TIFF standard in a number of ways, including the use of a
    // non-standard file header, the inclusion of additional image tags and the encryption of some
    // of the tag data. 
    public static List<string> RawExtensions =
        [
            // Manufacturers 
            ".ari", // Arri Alexa
            ".bay", // Casio
            ".braw", // Blackmagic Design
            ".cri",  // Cintel
            ".iqq", ".cap", ".eip", // Phase One 
            ".3fr", // Hasselblad
            ".mos", // Leaf
            ".mef", // Mamiya
            ".pef", ".ptx", // Pentax
            ".erf", // Epson
            ".crw" ,".cr2" , ".cr3", // Canon 
            ".nef" , ".nrw", // Nikon 
            ".arw" , "srf", "sr2", // Sony 
            ".raf" , // Fuji 
            ".rw2" , // Leica / Panasonic 
            ".orf" , // Olympus 
            ".dcr", ".k25", ".kdc", // Kodak, 
            ".fff", // Imacon / Hasselblad
            ".gpr", // GoPro
            ".mdc", // Minolta, Agfa
            ".mrw", // Minolta, Konica Minolta
            ".pxn", // Logitech
            ".r3d", // RED Digital Cinema
            ".rwl", // Leica
            ".rwz", // Rawzor
            ".srw", // Samsung
            ".tco", // intoPIX
            ".x3f", // Sigma

            // Generic 
            ".dng" , // Adobe 
            ".raw" , // Generic 
        ];

    public static bool HasRawExtension(string path)
        => RawExtensions.Contains(System.IO.Path.GetExtension(path).ToLower());

    public static List<string> ImageSharpExtensions =
        [
            ".tiff", ".cur", ".png", ".pbm", ".qoi", ".tga",
            ".webp", ".ico", ".gif", ".jpg", ".jpeg", ".jfif" , ".bmp", ".exr",
        ];

    public static bool HasImageSharpExtension(string path)
        => ImageSharpExtensions.Contains(System.IO.Path.GetExtension(path).ToLower());

    public static List<string> JpgExtensions =
        [
            ".jpg", ".jpeg", ".jfif" ,
        ];

    public static bool HasJpgExtension(string path)
        => JpgExtensions.Contains(System.IO.Path.GetExtension(path).ToLower());

#pragma warning restore CA2211 // Non-constant fields should not be visible

    #endregion Extensions Definitions

    private readonly static EnumerationOptions enumerationOptions = new()
    {
        IgnoreInaccessible = true,
        RecurseSubdirectories = false,
        MatchType = MatchType.Simple,
    };

    public static FolderStatistics InspectFolder(string folderPath)
    {
        var statistics = new FolderStatistics(folderPath);
        if (string.IsNullOrWhiteSpace(folderPath))
        {
            statistics.Fail("Invalid folder path");
            return statistics;
        }

        if (!Directory.Exists(folderPath))
        {
            statistics.Fail("Invalid folder path");
            return statistics;
        }

        ProcessDirectory(folderPath, statistics);
        statistics.Pack(); 
        return statistics;
    }

    private static bool ProcessDirectory(string folderPath, FolderStatistics statistics)
    {
        try
        {
            var files = folderPath.EnumerateFiles(enumerationOptions, "*.*");
            if (files.Count > 0)
            {
                foreach (string file in files)
                {
                    var fileInfo = new FileInfo(file);
                    long size = fileInfo.Length;
                    float sizeMB = size / (1024.0f * 1024.0f);
                    ImageKind kind = FromFilePath(file);
                    statistics.Add(kind, file, sizeMB);
                }
            }

            // Recurse to sub folders
            var subDirs = folderPath.EnumerateDirectories();
            foreach (string subDir in subDirs)
            {
                if (!ProcessDirectory(subDir, statistics))
                {
                    return false;
                }
            }

            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine("Exception thrown: \n" + ex);
            statistics.Fail(ex.ToString());
            return false;
        }
    }

    private static ImageKind FromFilePath(string filePath)
    {
        if (HasMovieExtension(filePath))
        {
            return ImageKind.Movie;
        }

        if (HasJpgExtension(filePath))
        {
            return ImageKind.Jpeg;
        }

        if (HasHiefExtension(filePath))
        {
            return ImageKind.Heic;
        }

        if (HasRawExtension(filePath))
        {
            return ImageKind.Raw;
        }

        // This check must be done last 
        if (HasImageSharpExtension(filePath))
        {
            return ImageKind.OtherImages;
        }

        return ImageKind.Unrecognized;
    }
}