namespace Lyt.PhotoPostPro.Model.Loader;

// Dont move to Global Usings : Conflicting with ImageSharp 
using Openize.Heic.Decoder;

public static class ImageLoader
{
    public const int ThumbnailLargestDimension = 480;

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

    #region Pre Loading 

    public static LoadedImage LoadImage(string imagePath)
    {
        try
        {
            LoadedImage? loadedImage = Guard(imagePath);
            if ( loadedImage is not null)
            {
                return loadedImage;
            }

            string? extension = System.IO.Path.GetExtension(imagePath);
            Debug.WriteLine(extension);
            if (HasHiefExtension(imagePath))
            {
                loadedImage = ImageLoader.TryLoadHiecWithOpenize(imagePath);
                if (!loadedImage.IsSuccess)
                {
                    loadedImage = ImageLoader.TryLoadWithLibRaw(imagePath);
                    if (!loadedImage.IsSuccess)
                    {
                        loadedImage = ImageLoader.TryLoadWithImageSharp(imagePath);
                    }
                }
            }
            else if (HasRawExtension(imagePath))
            {
                loadedImage = ImageLoader.TryLoadWithLibRaw(imagePath);
                if (!loadedImage.IsSuccess)
                {
                    loadedImage = ImageLoader.TryLoadWithImageSharp(imagePath);
                    if (!loadedImage.IsSuccess)
                    {
                        loadedImage = ImageLoader.TryLoadHiecWithOpenize(imagePath);
                    }
                }
            }
            else
            {
                loadedImage = ImageLoader.TryLoadWithImageSharp(imagePath);
                if (!loadedImage.IsSuccess)
                {
                    loadedImage = ImageLoader.TryLoadWithLibRaw(imagePath);
                    if (!loadedImage.IsSuccess)
                    {
                        loadedImage = ImageLoader.TryLoadHiecWithOpenize(imagePath);
                    }
                }
            }

            if (loadedImage is null)
            {
                return LoadedImage.Fail("Model.Loader.NoImage");
            }
            else
            {
                if (loadedImage.IsSuccess)
                {
                    Debug.WriteLine(" Image loaded");
                }

                return loadedImage;
            }
        }
        catch (Exception ex)
        {
            // errorMessage = "An error occurred while loading the source image." + ex.Message;
            Debug.WriteLine(ex);
            return LoadedImage.Fail("Model.Loader.Exception", ex.ToString());
        }
    }

    public static LoadedImage TryLoadHiecWithOpenize(string imagePath)
    {
        try
        {
            using var fs = new FileStream(imagePath, FileMode.Open);
            if (!HeicImage.CanLoad(fs))
            {
                // errorMessage = "The source image cannot be loaded with Openize.";
                return LoadedImage.Fail("Model.Loader.OpenizeCantLoad");
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
                // errorMessage = "Failed to load the source image with Openize.";
                return LoadedImage.Fail("Model.Loader.OpenizeFailedLoad");
            }

            Debug.WriteLine("HIEC Image loaded with Openize: " + imagePath);

            IReadOnlyList<MetadataExtractor.Directory>? directories = null;
            ExifData? exif = image.Exif;
            if (exif is not null)
            {
                directories = exif.DirectoriesList;
            }
            // else // No metadata : Directories stays null 

            var metadata = new Metadata(imagePath, width, height, directories);
            return LoadedImage.FullyLoaded(image48, metadata);
        }
        catch (Exception ex)
        {
            // errorMessage = "An error occurred while loading the HIEC image with Openize." + ex.Message;
            Debug.WriteLine(ex);
            return LoadedImage.Fail("Model.Loader.Exception", ex.ToString());
        }
    }

    public static LoadedImage TryLoadWithImageSharp(string imagePath)
    {
        try
        {
            // Load the image file into memory 
            var imageFormat = Image.DetectFormat(imagePath);
            if (imageFormat is null)
            {
                // errorMessage = "Unsupported image format in ImageSharp.";
                return LoadedImage.Fail("Model.Loader.ImageSharpNotDetected");
            }

            Debug.WriteLine(imageFormat.Name);
            var image48 = Image.Load<Rgb48>(imagePath);
            if (image48 is null)
            {
                // errorMessage = "Failed to load the source image with ImageSharp.";
                return LoadedImage.Fail("Model.Loader.ImageSharpFailedLoad");
            }

            Debug.WriteLine("Image loaded with ImageSharp: " + imagePath);

            IReadOnlyList<MetadataExtractor.Directory>? directories = null;
            ImageMetadata imageMetadata = image48.Metadata;
            ExifProfile? exifProfile = imageMetadata.ExifProfile;
            if (exifProfile is not null)
            {
                var fieldInfo = exifProfile.GetType().GetField("data", BindingFlags.Instance | BindingFlags.NonPublic);
                if (fieldInfo is not null)
                {
                    object? fieldData = fieldInfo.GetValue(exifProfile);
                    if (fieldData is byte[] exifRawData)
                    {
                        var memoryStream = new MemoryStream(exifRawData);
                        directories = MetadataExtractor.ImageMetadataReader.ReadMetadata(memoryStream);
                    }
                }
            }

            var metadata = new Metadata(imagePath, image48.Width, image48.Height, directories);
            return LoadedImage.FullyLoaded(image48, metadata);
        }
        catch (Exception ex)
        {
            // errorMessage = "An error occurred while loading the source image with ImageSharp." + ex.Message;
            Debug.WriteLine(ex);
            return LoadedImage.Fail("Model.Loader.Exception", ex.ToString());
        }
    }

    public static LoadedImage TryLoadWithLibRaw(string imagePath)
    {
        try
        {
            using var r = RawContext.OpenFile(imagePath);
            r.Unpack();
            r.DcrawProcess();
            using ProcessedImage rawImage = r.MakeDcrawMemoryImage();

            int width = rawImage.Width;
            int height = rawImage.Height;
            var pixelDataSpan = rawImage.AsSpan<byte>();
            nint pixelDataPtr = rawImage.DataPointer;

            Image<Rgb48>? image48 = null;
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
                            // errorMessage = "Failed to load the source image with ImageSharp.";
                            return LoadedImage.Fail("Model.Loader.LibRawFailToConvert24to48");
                        }

                        Debug.WriteLine("8 bits Image loaded with LibRaw: " + imagePath);
                    }
                    else if (rawImage.Bits == 16 && rawImage.Channels == 3)
                    {
                        image48 = Image.LoadPixelData<Rgb48>(pixelDataSpan, width, height);
                        if (image48 is null)
                        {
                            // errorMessage = "Failed to load the source image with ImageSharp.";
                            return LoadedImage.Fail("Model.Loader.LibRawFailToConvert48to48");
                        }

                        Debug.WriteLine("16 bits Image loaded with LibRaw: " + imagePath);
                    }
                    else
                    {
                        // errorMessage = "Unsupported image format.";
                        return LoadedImage.Fail("Model.Loader.LibRawUnsupportedFormat");
                    }
                }
            }

            var directories = MetadataExtractor.ImageMetadataReader.ReadMetadata(imagePath);
            var metadata = new Metadata(imagePath, width, height, directories);
            return LoadedImage.FullyLoaded(image48, metadata);
        }
        catch (Exception ex)
        {
            // errorMessage = "An error occurred while loading the source image with LibRaw: " + ex.Message;
            Debug.WriteLine(ex);
            return LoadedImage.Fail("Model.Loader.Exception", ex.ToString());
        }
    }

    #endregion Loading 

    #region Pre Loading 

    public static LoadedImage PreLoadImage(string imagePath)
    {
        try
        {
            LoadedImage? loadedImage = Guard(imagePath);
            if (loadedImage is not null)
            {
                return loadedImage;
            }

            string? extension = System.IO.Path.GetExtension(imagePath);
            Debug.WriteLine(extension);
            if (HasHiefExtension(imagePath))
            {
                loadedImage = ImageLoader.TryPreLoadHiecWithOpenize(imagePath);
                if (!loadedImage.IsSuccess)
                {
                    loadedImage = ImageLoader.TryPreLoadWithLibRaw(imagePath);
                    if (!loadedImage.IsSuccess)
                    {
                        loadedImage = ImageLoader.TryPreLoadWithImageSharp(imagePath);
                    }
                }
            }
            else if (HasRawExtension(imagePath))
            {
                loadedImage = ImageLoader.TryPreLoadWithLibRaw(imagePath);
                if (!loadedImage.IsSuccess)
                {
                    loadedImage = ImageLoader.TryPreLoadWithImageSharp(imagePath);
                    if (!loadedImage.IsSuccess)
                    {
                        loadedImage = ImageLoader.TryPreLoadHiecWithOpenize(imagePath);
                    }
                }
            }
            else
            {
                loadedImage = ImageLoader.TryPreLoadWithImageSharp(imagePath);
                if (!loadedImage.IsSuccess)
                {
                    loadedImage = ImageLoader.TryPreLoadWithLibRaw(imagePath);
                    if (!loadedImage.IsSuccess)
                    {
                        loadedImage = ImageLoader.TryPreLoadHiecWithOpenize(imagePath);
                    }
                }
            }

            if (loadedImage is null)
            {
                return LoadedImage.Fail("Model.Loader.NoImage");
            }
            else
            {
                if (loadedImage.IsSuccess)
                {
                    Debug.WriteLine(" Image loaded");
                }

                return loadedImage;
            }
        }
        catch (Exception ex)
        {
            // errorMessage = "An error occurred while loading the source image." + ex.Message;
            Debug.WriteLine(ex);
            return LoadedImage.Fail("Model.Loader.Exception", ex.ToString());
        }
    }

    public static LoadedImage TryPreLoadHiecWithOpenize(string imagePath)
    {
        try
        {
            using var fs = new FileStream(imagePath, FileMode.Open);
            if (!HeicImage.CanLoad(fs))
            {
                // errorMessage = "The source image cannot be loaded with Openize.";
                return LoadedImage.Fail("Model.Loader.OpenizeCantLoad");
            }

            var image = HeicImage.Load(fs);
            var frames = image.Frames;
            if (frames is null || frames.Count == 0)
            {
                // errorMessage = "The source image cannot be loaded with Openize.";
                return LoadedImage.Fail("Model.Loader.ImageSharpFailedLoad");
            }

            var frame = image.DefaultFrame;
            int width = (int)frame.Width;
            int height = (int)frame.Height;

            byte[] pixels; 
            var thumbnailFrame = 
                ( from f in frames.Values where f.Width <  width select f).FirstOrDefault();
            if (thumbnailFrame is not null)
            {
                pixels = frame.GetByteArray(Openize.Heic.Decoder.PixelFormat.Rgb24);
                width = (int)thumbnailFrame.Width;
                height = (int)thumbnailFrame.Height;
            }
            else
            {
                pixels = frame.GetByteArray(Openize.Heic.Decoder.PixelFormat.Rgb24);
            }

            var image24 = Image.LoadPixelData<Rgb24>(pixels, width, height);

            // Create thumbnail 
            image24.Mutate(x => x.Resize(
                new ResizeOptions
                {
                    Size = ThumbnailSize(image24.Width, image24.Height),
                    Mode = ResizeMode.Max, // Constrains dimensions while keeping aspect ratio
                    Sampler = KnownResamplers.Lanczos3 // High quality downsampling filter
                }));

            var saveMemoryStream = new MemoryStream();
            image24.SaveAsJpeg(saveMemoryStream, new JpegEncoder() { Quality = 80 });
            byte[] jpgEncoded = saveMemoryStream.ToArray();
            Debug.WriteLine("HIEC Image thumbnaiil loaded with Openize: " + imagePath);

            IReadOnlyList<MetadataExtractor.Directory>? directories = null;
            ExifData? exif = image.Exif;
            if (exif is not null)
            {
                directories = exif.DirectoriesList;
            }
            // else // No metadata : Directories stays null 

            var metadata = new Metadata(imagePath, width, height, directories);
            return LoadedImage.PreLoaded( metadata, jpgEncoded);
        }
        catch (Exception ex)
        {
            // errorMessage = "An error occurred while loading the HIEC image with Openize." + ex.Message;
            Debug.WriteLine(ex);
            return LoadedImage.Fail("Model.Loader.Exception", ex.ToString());
        }
    }

    public static LoadedImage TryPreLoadWithImageSharp(string imagePath)
    {
        try
        {
            // Load the image file into memory 
            var imageFormat = Image.DetectFormat(imagePath);
            if (imageFormat is null)
            {
                // errorMessage = "Unsupported image format in ImageSharp.";
                return LoadedImage.Fail("Model.Loader.ImageSharpNotDetected");
            }

            Debug.WriteLine(imageFormat.Name);

            var image24 = Image.Load<Rgb24>(imagePath);
            if (image24 is null)
            {
                // errorMessage = "Failed to load the source image with ImageSharp.";
                return LoadedImage.Fail("Model.Loader.ImageSharpFailedLoad");
            }

            Debug.WriteLine("Image 24 loaded with ImageSharp: " + imagePath);

            IReadOnlyList<MetadataExtractor.Directory>? directories = null;
            ImageMetadata imageMetadata = image24.Metadata;
            ExifProfile? exifProfile = imageMetadata.ExifProfile;
            if (exifProfile is not null)
            {
                var fieldInfo = exifProfile.GetType().GetField("data", BindingFlags.Instance | BindingFlags.NonPublic);
                if (fieldInfo is not null)
                {
                    object? fieldData = fieldInfo.GetValue(exifProfile);
                    if (fieldData is byte[] exifRawData)
                    {
                        var memoryStream = new MemoryStream(exifRawData);
                        directories = MetadataExtractor.ImageMetadataReader.ReadMetadata(memoryStream);
                    }
                }
            }

            // Create thumbnail and metadata
            byte[] jpgEncoded = GenerateJpgThumbnail(image24);
            var metadata = new Metadata(imagePath, image24.Width, image24.Height, directories);
            return LoadedImage.PreLoaded(metadata, jpgEncoded);
        }
        catch (Exception ex)
        {
            // errorMessage = "An error occurred while loading the source image with ImageSharp." + ex.Message;
            Debug.WriteLine(ex);
            return LoadedImage.Fail("Model.Loader.Exception", ex.ToString());
        }
    }

    public static LoadedImage TryPreLoadWithLibRaw(string imagePath)
    {
        try
        {
            using var r = RawContext.OpenFile(imagePath);
            r.Unpack();
            int width = r.Width;
            int height = r.Height;
            var directories = MetadataExtractor.ImageMetadataReader.ReadMetadata(imagePath);
            var metadata = new Metadata(imagePath, width, height, directories);
            ProcessedImage thumbnail = r.ExportThumbnail();

            byte[]? jpgEncoded; 
            var pixelDataSpan = thumbnail.AsSpan<byte>();
            if (pixelDataSpan.Length == 0)
            {
                // TODO: Replace thumb with placeholder image 
                return LoadedImage.Fail("Model.Loader.LibRawNoThumnail");
            }

            nint pixelDataPtr = thumbnail.DataPointer;

            unsafe
            {
                // Pixel data from LIB-Raw is in C++ memory, need to pin it
                fixed (byte* pixelData = &pixelDataSpan[0])
                {
                    if (thumbnail.Bits == 8 && thumbnail.Channels == 3)
                    {
                        var image24 = Image.LoadPixelData<Rgb24>(pixelDataSpan, width, height);
                        jpgEncoded = GenerateJpgThumbnail(image24);
                        Debug.WriteLine("8 bits Image loaded with LibRaw: " + imagePath);
                    }
                    else if (thumbnail.Bits == 16 && thumbnail.Channels == 3)
                    {
                        // WTF ? 48 bits for thumbs? 
                        return LoadedImage.Fail("Model.Loader.LibRawUnsupportedFormat");
                    }
                    else
                    {
                        // errorMessage = "Unsupported image format.";
                        return LoadedImage.Fail("Model.Loader.LibRawUnsupportedFormat");
                    }
                }
            }

            if (jpgEncoded is not null)
            {
                return LoadedImage.PreLoaded(metadata, jpgEncoded);
            }

            return LoadedImage.Fail("Model.Loader.LibRawFailLoad");
        }
        catch (Exception ex)
        {
            // errorMessage = "An error occurred while loading the source image with LibRaw: " + ex.Message;
            Debug.WriteLine(ex);
            return LoadedImage.Fail("Model.Loader.Exception", ex.ToString());
        }
    }

    #endregion Pre Loading 

    private static byte[] GenerateJpgThumbnail (Image<Rgb24> image24)
    {
        // Create thumbnail 
        image24.Mutate(x => x.Resize(
            new ResizeOptions
            {
                Size = ThumbnailSize(image24.Width, image24.Height),
                Mode = ResizeMode.Max, // Constrains dimensions while keeping aspect ratio
                Sampler = KnownResamplers.Lanczos3 // High quality downsampling filter
            }));

        var saveMemoryStream = new MemoryStream();
        image24.SaveAsJpeg(saveMemoryStream, new JpegEncoder() { Quality = 80 });
        byte[] jpgEncoded = saveMemoryStream.ToArray();
        return jpgEncoded;
    }

    public static Size ThumbnailSize(int width, int height)
    {
        int newWidth;
        int newHeight;
        if (width > height)
        {
            newWidth = ThumbnailLargestDimension;
            float ratio = width / (float)ThumbnailLargestDimension;
            newHeight = (int)(0.5f + height / ratio);
        }
        else
        {
            newHeight = ThumbnailLargestDimension;
            float ratio = height / (float)ThumbnailLargestDimension;
            newWidth = (int)(0.5f + width / ratio);
        }

        return new Size(newWidth, newHeight);
    }

    /// <summary> Returns null if OK ! </summary>
    private static LoadedImage? Guard (string imagePath)
    {
        if (string.IsNullOrWhiteSpace(imagePath))
        {
            return LoadedImage.Fail("Model.Loader.InvalidPath");
        }

        if (!File.Exists(imagePath))
        {
            // "Source file does not exist."
            return LoadedImage.Fail("Model.Loader.NotExisting");
        }

        if (HasExcludedExtension(imagePath))
        {
            // "Source file has a know extension for something that is def' not an image.";
            // Play safe with user documents 
            return LoadedImage.Fail("Model.Loader.ExcludedNotImage");
        }

        if (HasMovieExtension(imagePath))
        {
            // "Source file is likely a movie.";
            return LoadedImage.Fail("Model.Loader.MaybeMovie");
        }

        return null; 
    }
}