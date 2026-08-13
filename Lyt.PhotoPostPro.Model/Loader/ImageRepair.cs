namespace Lyt.PhotoPostPro.Model.Loader;

// Needed to prevent conflicts with global usings 
using System.IO;

public static class ImageRepair
{
    public static bool TryFixMissingJpgSOI(string corruptedFilePath, out string fixedFilePath)
    {
        fixedFilePath = string.Empty;
        try
        {
            byte[] fileBytes = File.ReadAllBytes(corruptedFilePath);

            // Check if the file already starts with the JPEG SOI marker (0xFF, 0xD8)
            if (fileBytes.Length >= 2 && fileBytes[0] == 0xFF && fileBytes[1] == 0xD8)
            {
                // SOI is already present; the corruption lies deeper, possibly in DQT/DHT markers.
                return false;
            }

            string? directory = Path.GetDirectoryName(corruptedFilePath);
            string? filename = Path.GetFileNameWithoutExtension(corruptedFilePath);
            string? extension = Path.GetExtension(corruptedFilePath);
            if (string.IsNullOrWhiteSpace(directory) ||
                string.IsNullOrWhiteSpace(filename) ||
                string.IsNullOrWhiteSpace(extension))
            {
                return false;
            }

            filename = string.Concat("Repaired_", filename, extension);
            fixedFilePath = Path.Combine(directory, filename);
            using FileStream fs = new(fixedFilePath, FileMode.Create, FileAccess.Write);
            using BinaryWriter writer = new(fs);

            // Explicitly write the missing JPEG SOI marker bytes
            writer.Write((byte)0xFF);
            writer.Write((byte)0xD8);

            // Append the rest of the original data payload
            writer.Write(fileBytes);
            return true;
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex);
            return false;
        }
    }
}
