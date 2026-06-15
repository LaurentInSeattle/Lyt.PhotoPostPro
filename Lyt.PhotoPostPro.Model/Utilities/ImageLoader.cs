namespace Lyt.PhotoPostPro.Model.Utilities;

public static class ImageLoader
{
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

            var image = ImageLoader.TryLoadWithImageSharp(imagePath, out errorMessage);
            if (image is null)
            {
                return ImageLoader.TryLoadWithLibRaw(imagePath, out errorMessage);
            }
            else
            {
                return image;
            }
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
        // CRW/CR2, NEF, RAF, DNG, MOS, KDC, DCR 

        //r.DcrawProcess(context =>
        //{
        //    context.HalfSize = false;
        //    context.UseCameraWb = false;
        //    context.Interpolation = true;
        //});

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

                        // var image = ImageLoader.LoadImageExtendingDepth(pixelData, width, height);
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

    public static unsafe Image<Rgb48> LoadImageExtendingDepth(byte* pixelData, int width, int height)
    {
        // Create a new Rgb48 image with the same dimensions
        Image<Rgb48> image = new(width, height);

        // Supposedly paralellize access to rows 
        image.ProcessPixelRows(accessor =>
        {
            for (int y = 0; y < accessor.Height; ++y)
            { 
                byte * rowPixelData = pixelData + y * width;
                Span<Rgb48> pixelRow = accessor.GetRowSpan(y);

                // Loop through all pixels to transcode 
                for (int x = 0; x < pixelRow.Length; ++x)
                {
                    ref Rgb48 pixel = ref pixelRow[x];
                    byte r = *rowPixelData++;
                    byte g = *rowPixelData++;
                    byte b = *rowPixelData++;
                    pixel.R = (ushort)(r << 8);
                    pixel.G = (ushort)(g << 8);
                    pixel.B = (ushort)(b << 8);
                }
            }
        });

        return image;
    }
}