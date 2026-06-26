namespace Lyt.PhotoPostPro.Model.Utilities;

using Openize.Heic.Decoder;

public static class ImageLoader
{
    public static List<string> HeifExtensions = [".heic", ".heif", ".hif"];

    public static bool HasHiefExtension(string path) => HeifExtensions.Contains(Path.GetExtension(path).ToLower());

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
            ".iqq", // Phase One 
            ".3fr", // Hasselblad
            ".mos", // Leaf
            ".mef", // Mamiya
            ".pef", // Pentax
            ".erf", // Epson
            ".crw" ,".cr2" , ".cr3", // Canon 
            ".nef" , ".nrw", // Nikon 
            ".arw" , "srf", "sr2", // Sony 
            ".raf" , // Fuji 
            ".rw2" , // Leica / Panasonic 
            ".orf" , // Olympus 
            ".dcr", ".k25", ".kdc", // Kodak, 
            // 
            ".dng" , // Adobe 
            ".raw" , // Generic 
        ];

    public static bool HasRawExtension(string path) => RawExtensions.Contains(Path.GetExtension(path).ToLower());

    public static List<string> ImageSharpExtensions =
        [
            ".tiff", ".cur", ".png", ".pbm", ".qoi", ".tga",
            ".webp", ".ico", ".gif", ".jpg", ".jpeg", ".bmp", ".exr",
        ];

    public static bool HasImageSharpExtension(string path) => ImageSharpExtensions.Contains(Path.GetExtension(path).ToLower());

    public static Image<Rgb48>? LoadImage(string imagePath, out string errorMessage)
    {
        errorMessage = string.Empty;
        try
        {
            if (!File.Exists(imagePath))
            {
                errorMessage = "Source image file does not exist.";
                return null;
            }

            string? extension = Path.GetExtension(imagePath);
            Debug.WriteLine(extension);

            Image<Rgb48>? image = null;
            if (HasHiefExtension(imagePath))
            {
                image = ImageLoader.TryLoadHiecWithOpenize(imagePath, out errorMessage);
                if (image is null)
                {
                    image = ImageLoader.TryLoadWithLibRaw(imagePath, out errorMessage);
                    if (image is null)
                    {
                        image = ImageLoader.TryLoadWithImageSharp(imagePath, out errorMessage);
                    }
                }
            }
            else if (HasRawExtension(imagePath))
            {
                image = ImageLoader.TryLoadWithLibRaw(imagePath, out errorMessage);
                if (image is null)
                {
                    image = ImageLoader.TryLoadWithImageSharp(imagePath, out errorMessage);
                    if (image is null)
                    {
                        image = ImageLoader.TryLoadHiecWithOpenize(imagePath, out errorMessage);
                    }
                }
            }
            else
            {
                image = ImageLoader.TryLoadWithImageSharp(imagePath, out errorMessage);
                if (image is null)
                {
                    image = ImageLoader.TryLoadWithLibRaw(imagePath, out errorMessage);
                    if (image is null)
                    {
                        image = ImageLoader.TryLoadHiecWithOpenize(imagePath, out errorMessage);
                    }
                }
            }

            return image;
        }
        catch (Exception ex)
        {
            errorMessage = "An error occurred while loading the source image." + ex.Message;
            Debug.WriteLine(ex);
            return null;
        }
    }

    public static Image<Rgb48>? TryLoadWithImageSharp(string imagePath, out string errorMessage)
    {
        errorMessage = string.Empty;
        try
        {
            // Load the image file into memory 
            var imageFormat = Image.DetectFormat(imagePath);
            if (imageFormat is null)
            {
                errorMessage = "Unsupported image format in ImageSharp.";
                return null;
            }

            Debug.WriteLine(imageFormat.Name);
            var image = Image.Load<Rgb48>(imagePath);
            if (image is null)
            {
                errorMessage = "Failed to load the source image with ImageSharp.";
                return null;
            }

            Debug.WriteLine("Image loaded with ImageSharp: " + imagePath);
            return image;
        }
        catch (Exception ex)
        {
            errorMessage = "An error occurred while loading the source image with ImageSharp." + ex.Message;
            Debug.WriteLine(ex);
            return null;
        }
    }

    public static Image<Rgb48>? TryLoadWithLibRaw(string imagePath, out string errorMessage)
    {
        errorMessage = string.Empty;
        try
        {
            using RawContext r = RawContext.OpenFile(imagePath);
            r.Unpack();
            r.DcrawProcess();
            using ProcessedImage rawImage = r.MakeDcrawMemoryImage();

            int width = rawImage.Width;
            int height = rawImage.Height;
            var pixelDataSpan = rawImage.AsSpan<byte>();
            nint pixelDataPtr = rawImage.DataPointer;

            unsafe
            {
                // Pixel data from LIBRAw in in C++ memory, need to pin it
                fixed (byte* pixelData = &pixelDataSpan[0])
                {
                    if (rawImage.Bits == 8 && rawImage.Channels == 3)
                    {
                        var image24 = Image.LoadPixelData<Rgb24>(pixelDataSpan, width, height);
                        var image = image24.CloneAs<Rgb48>();
                        if (image is null)
                        {
                            errorMessage = "Failed to load the source image with ImageSharp.";
                            return null;
                        }

                        Debug.WriteLine("8 bits Image loaded with LibRaw: " + imagePath);
                        return image;
                    }
                    else if (rawImage.Bits == 16 && rawImage.Channels == 3)
                    {
                        var image = Image.LoadPixelData<Rgb48>(pixelDataSpan, width, height);
                        if (image is null)
                        {
                            errorMessage = "Failed to load the source image with ImageSharp.";
                            return null;
                        }

                        Debug.WriteLine("16 bits Image loaded with LibRaw: " + imagePath);
                        return image;
                    }
                    else
                    {
                        errorMessage = "Unsupported image format.";
                        return null;
                    }

                }
            }
        }
        catch (Exception ex)
        {
            errorMessage = "An error occurred while loading the source image with LibRaw: " + ex.Message;
            Debug.WriteLine(ex);
            return null;
        }
    }

    public static unsafe Image<Rgb48>? TryLoadHiecWithOpenize(string imagePath, out string errorMessage)
    {
        errorMessage = string.Empty;

        try
        {
            using var fs = new FileStream(imagePath, FileMode.Open);
            var image = HeicImage.Load(fs);
            byte[] pixels = image.GetByteArray(Openize.Heic.Decoder.PixelFormat.Rgb24);
            int width = (int)image.Width;
            int height = (int)image.Height;

            var image24 = Image.LoadPixelData<Rgb24>(pixels, width, height);
            var image48 = image24.CloneAs<Rgb48>();
            if (image48 is null)
            {
                errorMessage = "Failed to load the source image with Openize.";
                return null;
            }

            Debug.WriteLine("HIEC Image loaded with Openize: " + imagePath);
            return image48;
        }
        catch (Exception ex)
        {
            errorMessage = "An error occurred while loading the HIEC image with Openize." + ex.Message;
            Debug.WriteLine(ex);
            return null;
        }
    }
}