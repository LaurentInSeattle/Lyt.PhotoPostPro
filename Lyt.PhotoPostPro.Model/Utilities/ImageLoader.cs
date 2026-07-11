namespace Lyt.PhotoPostPro.Model.Utilities;

// Dont move to Global Usings : Conflicting with ImageSharp 
using Openize.Heic.Decoder;


public static class ImageLoader
{
#pragma warning disable CA2211 // Non-constant fields should not be visible
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

    public static bool HasRawExtension(string path) 
        => RawExtensions.Contains(System.IO.Path.GetExtension(path).ToLower());

    public static List<string> ImageSharpExtensions =
        [
            ".tiff", ".cur", ".png", ".pbm", ".qoi", ".tga",
            ".webp", ".ico", ".gif", ".jpg", ".jpeg", ".bmp", ".exr",
        ];

    public static bool HasImageSharpExtension(string path) 
        => ImageSharpExtensions.Contains(System.IO.Path.GetExtension(path).ToLower());

#pragma warning restore CA2211 // Non-constant fields should not be visible

    public static (Image<Rgb48>?, Metadata?) LoadImage(string imagePath, out string errorMessage)
    {
        (Image<Rgb48> ?, Metadata?) fail = (null, null); 
        errorMessage = string.Empty;
        try
        {
            if (!File.Exists(imagePath))
            {
                errorMessage = "Source image file does not exist.";
                return fail;
            }

            string? extension = System.IO.Path.GetExtension(imagePath);
            Debug.WriteLine(extension);

            Image<Rgb48>? image = null;
            Metadata? metadata = null;
            if (HasHiefExtension(imagePath))
            {
                (image, metadata) = ImageLoader.TryLoadHiecWithOpenize(imagePath, out errorMessage);
                if (image is null)
                {
                    (image, metadata) = ImageLoader.TryLoadWithLibRaw(imagePath, out errorMessage);
                    if (image is null)
                    {
                        (image, metadata) = ImageLoader.TryLoadWithImageSharp(imagePath, out errorMessage);
                    }
                }
            }
            else if (HasRawExtension(imagePath))
            {
                (image, metadata) = ImageLoader.TryLoadWithLibRaw(imagePath, out errorMessage);
                if (image is null)
                {
                    (image, metadata) = ImageLoader.TryLoadWithImageSharp(imagePath, out errorMessage);
                    if (image is null)
                    {
                        (image, metadata) = ImageLoader.TryLoadHiecWithOpenize(imagePath, out errorMessage);
                    }
                }
            }
            else
            {
                (image, metadata) = ImageLoader.TryLoadWithImageSharp(imagePath, out errorMessage);
                if (image is null)
                {
                    (image, metadata) = ImageLoader.TryLoadWithLibRaw(imagePath, out errorMessage);
                    if (image is null)
                    {
                        (image, metadata) = ImageLoader.TryLoadHiecWithOpenize(imagePath, out errorMessage);
                    }
                }
            }

            return (image, metadata);
        }
        catch (Exception ex)
        {
            errorMessage = "An error occurred while loading the source image." + ex.Message;
            Debug.WriteLine(ex);
            return fail;
        }
    }

    public static (Image<Rgb48>?, Metadata?) TryLoadWithImageSharp(string imagePath, out string errorMessage)
    {
        (Image<Rgb48>?, Metadata?) fail = (null, null);
        errorMessage = string.Empty;
        try
        {
            // Load the image file into memory 
            var imageFormat = Image.DetectFormat(imagePath);
            if (imageFormat is null)
            {
                errorMessage = "Unsupported image format in ImageSharp.";
                return fail;
            }

            Debug.WriteLine(imageFormat.Name);
            var image48 = Image.Load<Rgb48>(imagePath);
            if (image48 is null)
            {
                errorMessage = "Failed to load the source image with ImageSharp.";
                return fail;
            }

            Debug.WriteLine("Image loaded with ImageSharp: " + imagePath);

            IReadOnlyList<MetadataExtractor.Directory>? directories = null;
            ImageMetadata imageMetadata = image48.Metadata;
            ExifProfile? exifProfile = imageMetadata.ExifProfile;
            if (exifProfile is not null)
            {
                var fieldInfo = exifProfile.GetType().GetField("data", BindingFlags.Instance | BindingFlags.NonPublic); 
                if ( fieldInfo is not null)
                {
                    object? fieldData = fieldInfo.GetValue(exifProfile);
                    if (fieldData is  byte[] exifRawData )
                    {
                        var memoryStream = new MemoryStream(exifRawData);
                        directories = MetadataExtractor.ImageMetadataReader.ReadMetadata(memoryStream);
                    }
                }
            }

            var metadata = new Metadata(imagePath, image48.Width, image48.Height, directories);
            return (image48, metadata);
        }
        catch (Exception ex)
        {
            errorMessage = "An error occurred while loading the source image with ImageSharp." + ex.Message;
            Debug.WriteLine(ex);
            return fail;
        }
    }

    public static (Image<Rgb48>?, Metadata?) TryLoadWithLibRaw(string imagePath, out string errorMessage)
    {
        (Image<Rgb48>?, Metadata?) fail = (null, null);
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

            Image<Rgb48>? image48= null; 
            unsafe
            {
                // Pixel data from LIBRAw is in C++ memory, need to pin it
                fixed (byte* pixelData = &pixelDataSpan[0])
                {
                    if (rawImage.Bits == 8 && rawImage.Channels == 3)
                    {
                        var image24 = Image.LoadPixelData<Rgb24>(pixelDataSpan, width, height);
                        image48 = image24.CloneAs<Rgb48>();
                        if (image48 is null)
                        {
                            errorMessage = "Failed to load the source image with ImageSharp.";
                            return fail;
                        }

                        Debug.WriteLine("8 bits Image loaded with LibRaw: " + imagePath);
                    }
                    else if (rawImage.Bits == 16 && rawImage.Channels == 3)
                    {
                        image48 = Image.LoadPixelData<Rgb48>(pixelDataSpan, width, height);
                        if (image48 is null)
                        {
                            errorMessage = "Failed to load the source image with ImageSharp.";
                            return fail;
                        }

                        Debug.WriteLine("16 bits Image loaded with LibRaw: " + imagePath);
                    }
                    else
                    {
                        errorMessage = "Unsupported image format.";
                        return fail;
                    }
                }
            }

            var directories = MetadataExtractor.ImageMetadataReader.ReadMetadata(imagePath);
            var metadata = new Metadata(imagePath, width, height, directories );
            return ( image48, metadata); 
        }
        catch (Exception ex)
        {
            errorMessage = "An error occurred while loading the source image with LibRaw: " + ex.Message;
            Debug.WriteLine(ex);
            return fail;
        }
    }

    public static (Image<Rgb48>?, Metadata?) TryLoadHiecWithOpenize(string imagePath, out string errorMessage)
    {
        (Image<Rgb48>?, Metadata?) fail = (null, null);
        errorMessage = string.Empty;
        try
        {
            using var fs = new FileStream(imagePath, FileMode.Open);
            if ( !HeicImage.CanLoad(fs))
            {
                errorMessage = "The source image cannot be loaded with Openize.";
                return fail;
            }

            var image = HeicImage.Load(fs);
            var frame = image.DefaultFrame; 
            int width = (int)frame.Width;
            int height = (int)frame.Height;
            byte[] pixels = frame.GetByteArray(Openize.Heic.Decoder.PixelFormat.Rgb24);
            var image24 = Image.LoadPixelData<Rgb24>(pixels, width, height);
            var image48 = image24.CloneAs<Rgb48>();
            if (image48 is null)
            {
                errorMessage = "Failed to load the source image with Openize.";
                return fail;
            }

            Debug.WriteLine("HIEC Image loaded with Openize: " + imagePath);

            IReadOnlyList<MetadataExtractor.Directory>? directories = null; 
            ExifData? exif = image.Exif;
            if ( exif is not null)
            {
                directories = exif.DirectoriesList;
            }
            // else // No metadata : Directories stays null 

            var metadata = new Metadata(imagePath, width, height, directories);
            return (image48, metadata);
        }
        catch (Exception ex)
        {
            errorMessage = "An error occurred while loading the HIEC image with Openize." + ex.Message;
            Debug.WriteLine(ex);
            return fail;
        }
    }
}