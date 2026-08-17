namespace Lyt.PhotoPostPro.Model.Loader;

// Dont move to Global Usings : Conflicting with ImageSharp 
using Openize.Heic.Decoder;

using System.Net.Http.Headers;

public static class ImageLoader
{
    public const int ThumbnailQuality = 80;
    public const int HdImageQuality = 90;
    public const int ThumbnailLargestDimension = 420;
    public const int HdWidth = 1920;
    public const int HdHeight = 1080;

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

    public static string LibRawVersion => RawContext.Version;

    #region Loading 

    private static bool repairedImage = false;

    public static LoadedImage LoadImage(string imagePath)
    {
        repairedImage = false;
        return LoadImageInternal(imagePath);
    }

    internal static LoadedImage LoadImageInternal(string imagePath)
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
                loadedImage.LoadedFrom = imagePath;
                if (loadedImage.IsSuccess)
                {
                    loadedImage.RotateIfNeeded();
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

    private static LoadedImage TryLoadHiecWithOpenize(string imagePath)
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
            var imageFp = image24.CloneAs<RgbaHalf>();
            if (imageFp is null)
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
            return LoadedImage.FullyLoaded(imageFp, metadata);
        }
        catch (Exception ex)
        {
            // errorMessage = "An error occurred while loading the HIEC image with Openize." + ex.Message;
            Debug.WriteLine(ex);
            return LoadedImage.Fail("Model.Loader.Exception", ex.ToString());
        }
    }

    private static LoadedImage TryLoadWithImageSharp(string imagePath)
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
            var imageFp = Image.Load<RgbaHalf>(imagePath);
            if (imageFp is null)
            {
                // errorMessage = "Failed to load the source image with ImageSharp.";
                return LoadedImage.Fail("Model.Loader.ImageSharpFailedLoad");
            }

            Debug.WriteLine("Image loaded with ImageSharp: " + imagePath);

            IReadOnlyList<MetadataExtractor.Directory>? directories = null;
            ImageMetadata imageMetadata = imageFp.Metadata;
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

            var metadata = new Metadata(imagePath, imageFp.Width, imageFp.Height, directories);
            return LoadedImage.FullyLoaded(imageFp, metadata);
        }
        catch (Exception ex)
        {
            if (HasJpgExtension(imagePath))
            {
                if (!repairedImage)
                {
                    if (ImageRepair.TryFixMissingJpgSOI(imagePath, out string repairedImagePath))
                    {
                        repairedImage = true;
                        try
                        {
                            var loadedImage = TryLoadWithImageSharp(repairedImagePath);
                            if (loadedImage is null)
                            {
                                // still broken  
                                throw;
                            }
                        }
                        catch (Exception innerEx)
                        {
                            Debug.WriteLine(innerEx);
                            return LoadedImage.Fail("Model.Loader.Exception", ex.ToString());
                        }
                    }
                }
            }

            // errorMessage = "An error occurred while loading the source image with ImageSharp." + ex.Message;
            Debug.WriteLine(ex);
            return LoadedImage.Fail("Model.Loader.Exception", ex.ToString());
        }
    }

    private static unsafe LoadedImage TryLoadWithLibRaw(string imagePath)
    {
        try
        {
            using var r = RawContext.OpenFile(imagePath);
            r.OutputBitsPerSample = 16;
            r.Unpack();
            r.DcrawProcess();
            using ProcessedImage rawImage = r.MakeDcrawMemoryImage();
            int width = rawImage.Width;
            int height = rawImage.Height;

            Image<RgbaHalf>? imageFp = null;
            if (rawImage.Bits == 8 && rawImage.Channels == 3)
            {
                var pixelDataByteSpan = rawImage.AsSpan<byte>();

                // Pixel data from LIBRAw is in C++ memory, need to pin it
                fixed (byte* pixelData = &pixelDataByteSpan[0])
                {
                    var image24 = Image.LoadPixelData<Rgb24>(pixelDataByteSpan, width, height);
                    imageFp = image24.CloneAs<RgbaHalf>();
                    if (imageFp is null)
                    {
                        // errorMessage = "Failed to load the source image with ImageSharp.";
                        return LoadedImage.Fail("Model.Loader.LibRawFailToConvert24to48");
                    }
                }

                Debug.WriteLine("8 bits Image loaded with LibRaw: " + imagePath);
            }
            else if (rawImage.Bits == 16 && rawImage.Channels == 3)
            {
                Span<ushort> pixelDataUshortSpan = rawImage.AsSpan<ushort>();
                // Pixel data from LIBRAw is in C++ memory, need to pin it
                fixed (ushort* pixelData = &pixelDataUshortSpan[0])
                {
                    Span<byte> byteSpan = MemoryMarshal.AsBytes(pixelDataUshortSpan);
                    var image48 = Image.LoadPixelData<Rgb48>(byteSpan, width, height);
                    imageFp = image48.CloneAs<RgbaHalf>();
                    if (imageFp is null)
                    {
                        // errorMessage = "Failed to load the source image with ImageSharp.";
                        return LoadedImage.Fail("Model.Loader.LibRawFailToConvert48to48");
                    }

                    Debug.WriteLine("16 bits Image loaded with LibRaw: " + imagePath);
                }
            }
            else
            {
                // errorMessage = "Unsupported image format.";
                return LoadedImage.Fail("Model.Loader.LibRawUnsupportedFormat");
            }

            var directories = MetadataExtractor.ImageMetadataReader.ReadMetadata(imagePath);
            var metadata = new Metadata(imagePath, width, height, directories, alreadyRotated: true);
            var loadedImage = LoadedImage.FullyLoaded(imageFp, metadata);
            return loadedImage;
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
                loadedImage = ImageLoader.TryPreLoadHiecWithOpenize(imagePath, isHd: false);
                if (!loadedImage.IsSuccess)
                {
                    loadedImage = ImageLoader.TryPreLoadWithLibRaw(imagePath);
                    if (!loadedImage.IsSuccess)
                    {
                        loadedImage = ImageLoader.TryPreLoadWithImageSharp(imagePath, isHd: false);
                    }
                }
            }
            else if (HasRawExtension(imagePath))
            {
                loadedImage = ImageLoader.TryPreLoadWithLibRaw(imagePath);
                if (!loadedImage.IsSuccess)
                {
                    loadedImage = ImageLoader.TryPreLoadWithImageSharp(imagePath, isHd: false);
                    if (!loadedImage.IsSuccess)
                    {
                        loadedImage = ImageLoader.TryPreLoadHiecWithOpenize(imagePath, isHd: false);
                    }
                }
            }
            else
            {
                loadedImage = ImageLoader.TryPreLoadWithImageSharp(imagePath, isHd: false);
                if (!loadedImage.IsSuccess)
                {
                    loadedImage = ImageLoader.TryPreLoadWithLibRaw(imagePath);
                    if (!loadedImage.IsSuccess)
                    {
                        loadedImage = ImageLoader.TryPreLoadHiecWithOpenize(imagePath, isHd: false);
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

    private static LoadedImage TryPreLoadWithLibRaw(string imagePath)
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

            // Extract the raw byte span containing the JPEG data
            ReadOnlySpan<byte> jpgEncoded = thumbnail.AsSpan<byte>();

            if (metadata.IsOrientationActionRequired)
            {
                // If orientation action is required, we need to load the image in ImageSharp and
                // apply the correct orientation
                var image = Image.Load(jpgEncoded);
                LoadedImage.RotateIfNeeded(metadata, image);
                var saveMemoryStream = new MemoryStream();
                image.SaveAsJpeg(saveMemoryStream, new JpegEncoder() { Quality = ThumbnailQuality });
                byte[] jpgRotatedEncoded = saveMemoryStream.ToArray();
                if (jpgRotatedEncoded.Length > 0)
                {
                    return LoadedImage.PreLoaded(metadata, jpgRotatedEncoded);
                }
            }
            else
            {
                // Nothing to do, the image is already in the correct orientation
                if (jpgEncoded.Length > 0)
                {
                    return LoadedImage.PreLoaded(metadata, jpgEncoded.ToArray());
                }
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

    #region Loading HD Size Images

    public static LoadedImage? LoadHdImage(string imagePath)
    {
        try
        {
            // Guard against null or empty image path, returns null if valid
            LoadedImage? loadedImage = Guard(imagePath);
            if (loadedImage is not null)
            {
                return loadedImage;
            }

            string? extension = System.IO.Path.GetExtension(imagePath);
            Debug.WriteLine(extension);
            if (HasHiefExtension(imagePath))
            {
                loadedImage = ImageLoader.TryPreLoadHiecWithOpenize(imagePath, isHd: true);
                if (!loadedImage.IsSuccess)
                {
                    loadedImage = ImageLoader.TryLoadHdWithLibRaw(imagePath);
                    if (!loadedImage.IsSuccess)
                    {
                        loadedImage = ImageLoader.TryPreLoadWithImageSharp(imagePath, isHd: true);
                    }
                }
            }
            else if (HasRawExtension(imagePath))
            {
                loadedImage = ImageLoader.TryLoadHdWithLibRaw(imagePath);
                if (!loadedImage.IsSuccess)
                {
                    loadedImage = ImageLoader.TryPreLoadWithImageSharp(imagePath, isHd: true);
                    if (!loadedImage.IsSuccess)
                    {
                        loadedImage = ImageLoader.TryPreLoadHiecWithOpenize(imagePath, isHd: true);
                    }
                }
            }
            else
            {
                loadedImage = ImageLoader.TryPreLoadWithImageSharp(imagePath, isHd: true);
                if (!loadedImage.IsSuccess)
                {
                    loadedImage = ImageLoader.TryLoadHdWithLibRaw(imagePath);
                    if (!loadedImage.IsSuccess)
                    {
                        loadedImage = ImageLoader.TryPreLoadHiecWithOpenize(imagePath, isHd: true);
                    }
                }
            }

            if (loadedImage is null)
            {
                return LoadedImage.Fail("Model.Loader.NoImage");
            }
            else
            {
                loadedImage.LoadedFrom = imagePath;
                Debug.WriteLine(" Image loaded");
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

    private static unsafe LoadedImage TryLoadHdWithLibRaw(string imagePath)
    {
        try
        {
            using var r = RawContext.OpenFile(imagePath);
            r.OutputBitsPerSample = 16;
            r.Unpack();
            r.DcrawProcess();
            using ProcessedImage rawImage = r.MakeDcrawMemoryImage();
            int width = rawImage.Width;
            int height = rawImage.Height;

            var directories = MetadataExtractor.ImageMetadataReader.ReadMetadata(imagePath);

            // LibRaw rotates the image based on the EXIF orientation tag
            var metadata = new Metadata(imagePath, width, height, directories, alreadyRotated: true);

            if (rawImage.Bits == 8 && rawImage.Channels == 3)
            {
                var pixelDataByteSpan = rawImage.AsSpan<byte>();

                // Pixel data from LIBRAw is in C++ memory, need to pin it
                fixed (byte* pixelData = &pixelDataByteSpan[0])
                {
                    var image24 = Image.LoadPixelData<Rgb24>(pixelDataByteSpan, width, height);
                    byte[] jpgEncoded = GenerateJpgThumbnailWithMutate(image24, metadata, isHd: true);
                    Debug.WriteLine("8 bits Image loaded with LibRaw: " + imagePath);
                    return LoadedImage.PreLoaded(metadata, jpgEncoded);
                }
            }
            else if (rawImage.Bits == 16 && rawImage.Channels == 3)
            {
                Span<ushort> pixelDataUshortSpan = rawImage.AsSpan<ushort>();
                // Pixel data from LIBRAw is in C++ memory, need to pin it
                fixed (ushort* pixelData = &pixelDataUshortSpan[0])
                {
                    Span<byte> byteSpan = MemoryMarshal.AsBytes(pixelDataUshortSpan);
                    var image48 = Image.LoadPixelData<Rgb48>(byteSpan, width, height);
                    byte[] jpgEncoded = GenerateJpgThumbnailWithMutate(image48, metadata, isHd: true);
                    Debug.WriteLine("16 bits Image loaded with LibRaw: " + imagePath);
                    return LoadedImage.PreLoaded(metadata, jpgEncoded);
                }
            }
            else
            {
                // errorMessage = "Unsupported image format.";
                return LoadedImage.Fail("Model.Loader.LibRawUnsupportedFormat");
            }

        }
        catch (Exception ex)
        {
            // errorMessage = "An error occurred while loading the source image with LibRaw: " + ex.Message;
            Debug.WriteLine(ex);
            return LoadedImage.Fail("Model.Loader.Exception", ex.ToString());
        }
    }

    #endregion Loading HD Size Images

    #region Shared

    private static LoadedImage TryPreLoadHiecWithOpenize(string imagePath, bool isHd)
    {
        try
        {
            using var fs = new FileStream(imagePath, FileMode.Open);
            if (!HeicImage.CanLoad(fs))
            {
                // errorMessage = "The source image cannot be loaded with Openize.";
                return LoadedImage.Fail("Model.Loader.OpenizeCantLoad");
            }

            var heicImage = HeicImage.Load(fs);
            var frames = heicImage.Frames;
            if (frames is null || frames.Count == 0)
            {
                // errorMessage = "The source image cannot be loaded with Openize.";
                return LoadedImage.Fail("Model.Loader.ImageSharpFailedLoad");
            }

            var frame = heicImage.DefaultFrame;
            int width = (int)frame.Width;
            int height = (int)frame.Height;

            byte[] pixels;
            var thumbnailFrame =
                (from f in frames.Values where f.Width < width select f).FirstOrDefault();
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

            IReadOnlyList<MetadataExtractor.Directory>? directories = null;
            ExifData? exif = heicImage.Exif;
            if (exif is not null)
            {
                directories = exif.DirectoriesList;
            }
            // else // No metadata : Directories stays null 

            var metadata = new Metadata(imagePath, width, height, directories);
            var image24 = Image.LoadPixelData<Rgb24>(pixels, width, height);

            // Create thumbnail 
            byte[] jpgEncoded = GenerateJpgThumbnailWithMutate(image24, metadata, isHd);
            Debug.WriteLine("HIEC Image thumbnaiil loaded with Openize: " + imagePath);

            return LoadedImage.PreLoaded(metadata, jpgEncoded);
        }
        catch (Exception ex)
        {
            // errorMessage = "An error occurred while loading the HIEC image with Openize." + ex.Message;
            Debug.WriteLine(ex);
            return LoadedImage.Fail("Model.Loader.Exception", ex.ToString());
        }
    }

    private static LoadedImage TryPreLoadWithImageSharp(string imagePath, bool isHd)
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

            // Save original image dimensions 
            int width = image24.Width;
            int height = image24.Height;

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

            // Create thumbnail and metadata, image24 mutates! 
            var metadata = new Metadata(imagePath, width, height, directories);
            byte[] jpgEncoded = GenerateJpgThumbnailWithMutate(image24, metadata, isHd);
            return LoadedImage.PreLoaded(metadata, jpgEncoded);
        }
        catch (Exception ex)
        {
            // errorMessage = "An error occurred while loading the source image with ImageSharp." + ex.Message;
            Debug.WriteLine(ex);
            return LoadedImage.Fail("Model.Loader.Exception", ex.ToString());
        }
    }

    /// <summary> Returns null if OK ! </summary>
    private static LoadedImage? Guard(string imagePath)
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

    #endregion Shared

    #region Thumnails

    /// <summary> Generates rotated thumnail from image, original is lost </summary>
    /// <remarks> Returns a higher definition and better quality JPEG, if isHD is true . </remarks>
    /// <remarks> => MUST use TPixel here <= </remarks>
    public static byte[] GenerateJpgThumbnailWithMutate<TPixel>(Image<TPixel> image, Metadata metadata, bool isHd)
        where TPixel : unmanaged, IPixel<TPixel>
    {
        // Rotate if metadata says so
        if (metadata.IsOrientationActionRequired)
        {
            RotateMode rotateMode = RotateMode.Rotate180;
            if (metadata.OrientationActionRequired == Metadata.OrientationAction.Rotate90Cw)
            {
                rotateMode = RotateMode.Rotate90;
            }
            else if (metadata.OrientationActionRequired == Metadata.OrientationAction.Rotate90Ccw)
            {
                rotateMode = RotateMode.Rotate270;
            }

            Debug.WriteLine(" Rotating: " + rotateMode);
            image.Mutate(x => x.Rotate(rotateMode));
        }

        // Create thumbnail 
        Size size = ThumbnailSize(image.Width, image.Height, isHd);
        image.Mutate(x => x.Resize(
            new ResizeOptions
            {
                Size = size,
                Mode = ResizeMode.Max, // Constrains dimensions while keeping aspect ratio
                Sampler = KnownResamplers.Lanczos3 // High quality downsampling filter
            }));

        // Save as JPG 
        var saveMemoryStream = new MemoryStream();
        image.SaveAsJpeg(saveMemoryStream, new JpegEncoder() { Quality = isHd ? HdImageQuality : ThumbnailQuality });
        byte[] jpgEncoded = saveMemoryStream.ToArray();
        return jpgEncoded;
    }

    public static byte[] GenerateJpgThumbnailWithClone(Image<RgbaHalf> imageFp)
    {
        // Create thumbnail with cloning 
        var clone = imageFp.Clone();
        clone.Mutate(x => x.Resize(
            new ResizeOptions
            {
                Size = ThumbnailSize(imageFp.Width, imageFp.Height, isHd: false),
                Mode = ResizeMode.Max, // Constrains dimensions while keeping aspect ratio
                Sampler = KnownResamplers.Lanczos3 // High quality downsampling filter
            }));

        var saveMemoryStream = new MemoryStream();
        clone.SaveAsJpeg(saveMemoryStream, new JpegEncoder() { Quality = ThumbnailQuality });
        byte[] jpgEncoded = saveMemoryStream.ToArray();
        return jpgEncoded;
    }

    public static Size ThumbnailSize(int width, int height, bool isHd)
    {
        int newWidth;
        int newHeight;
        if (isHd)
        {
            if (width > height)
            {
                float hdRatio = (float)width / HdWidth;
                newWidth = HdWidth;
                newHeight = (int)(0.5f + height / hdRatio);
            }
            else
            {
                float hdRatio = (float)height / HdHeight;
                newHeight = HdHeight;
                newWidth = (int)(0.5f + width / hdRatio);
            }
        }
        else
        {
            float thumbRatio;
            if (width > height)
            {
                newWidth = ThumbnailLargestDimension;
                thumbRatio = width / (float)ThumbnailLargestDimension;
                newHeight = (int)(0.5f + height / thumbRatio);
            }
            else
            {
                newHeight = ThumbnailLargestDimension;
                thumbRatio = height / (float)ThumbnailLargestDimension;
                newWidth = (int)(0.5f + width / thumbRatio);
            }
        }

        return new Size(newWidth, newHeight);
    }

    #endregion Thumnails
}