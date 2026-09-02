namespace Lyt.PhotoPostPro.Model.LookUp;

using System.IO;

using static ResourcesUtilities;

public sealed class LutsManager
{
    public const string LutsFolderName = "Luts";

    public const string Wildcard = "*";
    private const string CubeExtension = ".cube";
    private const string ThreeDLExtension = ".3dl";

    private readonly PhotoPostProModel model;
    private readonly ILogger logger;
    private readonly string lutsFolderPath;

    private readonly LruDictionary<string, LutHalf> loadedLuts = new(16);

    public LutsManager(PhotoPostProModel model, ILogger logger)
    {
        this.model = model;
        this.logger = logger;

        this.lutsFolderPath =
            Path.Combine(this.model.RootPath, PhotoPostProModel.PhotoPostProAppName, LutsFolderName);
        if (!Directory.Exists(this.lutsFolderPath))
        {
            Directory.CreateDirectory(this.lutsFolderPath);
        }
    }

    public List<LutMetadata> EnumerateLuts()
    {
        List<LutMetadata> list = EnumerateBuiltInLuts();
        var userList = this.EnumerateUserLuts();
        list.AddRange(userList);
        return list;
    }

    public bool TryLoadLut(LutMetadata lutMetadata, [NotNullWhen(true)] out LutHalf? lutHalf)
    {
        if (this.loadedLuts.TryGetValue(lutMetadata.FriendlyName, out lutHalf))
        {
            return true;
        }

        if (lutMetadata.IsEmbedded)
        {
            if (TryLoadBuiltInLut(lutMetadata, out lutHalf))
            {
                this.loadedLuts.Add(lutMetadata.FriendlyName, lutHalf);
                return true;
            }
        }
        else
        {
            if (TryLoadLutFromFile(lutMetadata, out lutHalf))
            {
                this.loadedLuts.Add(lutMetadata.FriendlyName, lutHalf);
                return true;
            }
        }

        return false;
    }

    public static bool Validate(string path, out string message)
    {
        message = string.Empty;
        string extension = Path.GetExtension(path).ToLowerInvariant();
        if ((extension != CubeExtension) && (extension != ThreeDLExtension))
        {
            message = "Not a .cube or .3dl file.";
            return false;
        }

        if (!path.IsReadable())
        {
            message = "File is locked.";
            return false;
        }

        return true;
    }

    public bool AddLut(string lutFilePath, out string message, [NotNullWhen(true)] out LutMetadata? lutMetadata)
    {
        message = string.Empty;
        lutMetadata = null;
        if (!Validate(lutFilePath, out message))
        {
            throw new Exception("Should have called Validate");
        }

        LutFormat lutFormat;
        string extension = Path.GetExtension(lutFilePath).ToLowerInvariant();
        if (extension == CubeExtension)
        {
            lutFormat = LutFormat.Cube;
        }
        else if (extension == ThreeDLExtension)
        {
            lutFormat = LutFormat.ThreeDL;
        }
        else
        {
            throw new Exception("Should have called Validate");
        }

        string friendlyName = Path.GetFileNameWithoutExtension(lutFilePath);
        friendlyName = StringExtensions.Wordify(friendlyName);
        lutMetadata = new LutMetadata(friendlyName, lutFilePath, lutFormat, IsEmbedded: false);
        bool canLoad = TryLoadLutFromFile(lutMetadata, out LutHalf? lutHalf);
        if (!canLoad || lutHalf is null)
        {
            message = "Corrupted? Cannot load.";
            return false;
        }

        this.loadedLuts.Add(friendlyName, lutHalf);

        // Try to copy the provided file to our LUT folder for future use
        // Ok to fail
        try
        {
            string fileName = Path.GetFileName(lutFilePath);
            string targetPath = Path.Combine(this.lutsFolderPath, fileName);
            File.Copy(lutFilePath, targetPath, overwrite: true);
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex);
        }

        return true;
    }

    private static List<LutMetadata> EnumerateBuiltInLuts()
    {
        ResourcesUtilities.SetExecutingAssembly(Assembly.GetExecutingAssembly());
        ResourcesUtilities.SetResourcesPath("Lyt.PhotoPostPro.Model");

        List<LutMetadata> list = [];

        void AddForExtension(string extension, LutFormat lutFormat)
        {
            List<string> resources = EnumerateEmbeddedResourceNames(extension);
            foreach (string resource in resources)
            {
                string trimmed = resource.Replace(extension, string.Empty);
                string[] tokens = trimmed.Split('.', StringSplitOptions.RemoveEmptyEntries);
                string friendlyName = tokens[^1];
                friendlyName = StringExtensions.Wordify(friendlyName);
                var lutMetadata = new LutMetadata(friendlyName, resource, lutFormat, IsEmbedded: true);
                list.Add(lutMetadata);
            }
        }

        AddForExtension(CubeExtension, LutFormat.Cube);
        AddForExtension(ThreeDLExtension, LutFormat.ThreeDL);
        return list;
    }

    private List<LutMetadata> EnumerateUserLuts()
    {
        List<LutMetadata> list = [];

        List<string> EnumerateLutFiles(string extension)
        {
            List<string> lutFiles = [];
            try
            {
                string searchPattern = string.Concat(Wildcard, extension);
                lutFiles = Directory.EnumerateFiles(this.lutsFolderPath, searchPattern).ToList();
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
            }

            return lutFiles;
        }

        void AddForExtension(string extension, LutFormat lutFormat)
        {
            List<string> lutFiles = EnumerateLutFiles(extension);
            foreach (string lutFile in lutFiles)
            {
                if (!lutFile.IsReadable())
                {
                    Debug.WriteLine(" Cannot be read: " + lutFile);
                    continue;
                }

                string friendlyName = Path.GetFileNameWithoutExtension(lutFile);
                friendlyName = StringExtensions.Wordify(friendlyName);
                var lutMetadata = new LutMetadata(friendlyName, lutFile, lutFormat, IsEmbedded: false);
                list.Add(lutMetadata);
            }
        }

        AddForExtension(CubeExtension, LutFormat.Cube);
        AddForExtension(ThreeDLExtension, LutFormat.ThreeDL);
        return list;
    }

    private static bool TryLoadBuiltInLut(LutMetadata lutMetadata, [NotNullWhen(true)] out LutHalf? lutHalf)
    {
        lutHalf = null;
        try
        {
            ResourcesUtilities.SetExecutingAssembly(Assembly.GetExecutingAssembly());
            ResourcesUtilities.SetResourcesPath("Lyt.PhotoPostPro.Model");

            string text = LoadEmbeddedTextResource(lutMetadata.Path, out string? resourceName);

            // Splits by both \r\n and \n, trimming and removing any empty entries 
            string[] lines =
                text.Split(
                    ["\r\n", "\r", "\n"],
                    StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            if (lutMetadata.LutFormat == LutFormat.Cube)
            {
                lutHalf = LutHalf.FromCubeLines(lines);
                return true;
            }
            else if (lutMetadata.LutFormat == LutFormat.ThreeDL)
            {
                lutHalf = LutHalf.From3dlLines(lines);
                return true;
            }
            else
            {
                throw new NotSupportedException("LUT format not supported");
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex);
            return false;
        }
    }

    private static bool TryLoadLutFromFile(LutMetadata lutMetadata, [NotNullWhen(true)] out LutHalf? lutHalf)
    {
        lutHalf = null;
        try
        {
            string path = lutMetadata.Path;
            FileInfo fileInfo = new(path);
            string[] lines = File.ReadAllLines(path);
            string extension = fileInfo.Extension.ToLowerInvariant();
            if (extension == CubeExtension)
            {
                lutHalf = LutHalf.FromCubeLines(lines);
            }
            else if (extension == ThreeDLExtension)
            {
                lutHalf = LutHalf.From3dlLines(lines);
            }
            else
            {
                throw new NotSupportedException("LUT format not supported");
            }

            return true;
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex);
            return false;
        }
    }
}
