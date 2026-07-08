namespace Lyt.PhotoPostPro.Model.LookUp;

using static ResourcesUtilities;

public static class LutsManager
{
    private const string Cube = ".cube";
    private const string ThreeDL = ".3dl";

    public static List<LutMetadata> BuiltInLuts ()
    {
        List<LutMetadata> list = [];

        void AddForExtension (string extension , LutFormat lutFormat)
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

        AddForExtension(Cube, LutFormat.Cube);
        AddForExtension(ThreeDL, LutFormat.ThreeDL);
        return list; 
    }

    public static bool TryLoadLut(LutMetadata lutMetadata, [NotNullWhen(true)] out Lut? lut)
    {
        if (lutMetadata.IsEmbedded)
        {
            return TryLoadBuiltInLut(lutMetadata, out lut); 
        }
        else
        {
            throw new  NotImplementedException("later..."); 
        } 
    }

    public static bool TryLoadBuiltInLut (LutMetadata lutMetadata, [NotNullWhen(true)] out Lut? lut)
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
}
