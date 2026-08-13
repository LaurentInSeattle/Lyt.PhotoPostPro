namespace Lyt.PhotoPostPro.Model.LookUp;

using static ResourcesUtilities;

public sealed class LutsManager
{
    public const string LutsFolderName = "Luts";

    private const string CubeExtension = ".cube";
    private const string ThreeDLExtension = ".3dl";

    private readonly string lutsFolderPath;
    private readonly LruDictionary<string, Lut> loadedLuts = new(16);

    public LutsManager()
    {
        string pictures = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);
        this.lutsFolderPath = 
            System.IO.Path.Combine(pictures, PhotoPostProModel.PhotoPostProAppName, LutsFolderName);
        if (!Directory.Exists(this.lutsFolderPath))
        {
            Directory.CreateDirectory(this.lutsFolderPath);
        }
    }

    public List<LutMetadata> EnumerateBuiltInLuts()
    {
        List<LutMetadata> list = [];

        void AddForExtension(string extension, LutFormat lutFormat)
        {
            List<string> resources = EnumerateEmbeddedResourceNames(extension);
            foreach (string resource in resources)
            {
                string trimmed = resource.Replace(extension, string.Empty);
                string[] tokens = trimmed.Split('.', StringSplitOptions.RemoveEmptyEntries);
                string friendly = tokens[^1];
                friendly = StringExtensions.Wordify(friendly);
                var lutMetadata = new LutMetadata(friendly, resource, lutFormat, IsEmbedded: true);
                list.Add(lutMetadata);
            }
        }

        AddForExtension(CubeExtension, LutFormat.Cube);
        AddForExtension(ThreeDLExtension, LutFormat.ThreeDL);
        return list;
    }

    public bool TryLoadLut(LutMetadata lutMetadata, [NotNullWhen(true)] out Lut? lut)
    {
        if (this.loadedLuts.TryGetValue(lutMetadata.FriendlyName, out lut))
        {
            return true;
        }

        if (lutMetadata.IsEmbedded)
        {
            if (this.TryLoadBuiltInLut(lutMetadata, out lut))
            {
                this.loadedLuts.Add(lutMetadata.FriendlyName, lut);
                return true;
            }
        }
        else
        {
            if (this.TryLoadLutFromFile(lutMetadata, out lut))
            {
                this.loadedLuts.Add(lutMetadata.FriendlyName, lut);
                return true;
            }
        }

        return false;
    }

    private bool TryLoadBuiltInLut(LutMetadata lutMetadata, [NotNullWhen(true)] out Lut? lut)
    {
        lut = null;
        try
        {
            string text = LoadEmbeddedTextResource(lutMetadata.Path, out string? resourceName);

            // Splits by both \r\n and \n, trimming and removing any empty entries 
            string[] lines =
                text.Split(
                    ["\r\n", "\r", "\n"],
                    StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            if (lutMetadata.LutFormat == LutFormat.Cube)
            {
                lut = Lut.FromCubeLines(lines);
                return true;
            }
            else if (lutMetadata.LutFormat == LutFormat.ThreeDL)
            {
                lut = Lut.From3dlLines(lines);
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

    private bool TryLoadLutFromFile(LutMetadata lutMetadata, [NotNullWhen(true)] out Lut? lut)
    {
        lut = null;
        try
        {
            string path = lutMetadata.Path;
            FileInfo fileInfo = new(path);
            string[] lines = File.ReadAllLines(path);
            string extension = fileInfo.Extension.ToLowerInvariant();
            if (extension == CubeExtension)
            {
                lut = Lut.FromCubeLines(lines);
            }
            else if (extension == ThreeDLExtension)
            {
                lut = Lut.From3dlLines(lines);
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
