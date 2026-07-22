namespace Lyt.PhotoPostPro.Model.Utilities;

public static class ImagingUtilities
{
    public const ushort pixMaxU = ushort.MaxValue;
    public const float pixMaxF = 65535.0f;
    public const double pixMaxD = 65535.0;

    public const int pixRangeI = (int) ( 1 + ushort.MaxValue) ;
    public const float pixRangeF = 65536.0f;
    public const double pixRangeD = 65536.0;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float ClipF(float value)
        => value < 0.0f ?
            0.0f :
            value > 1.0f ?
                1.0f :
                value;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static double ClipD(double value)
        => value < 0.0 ?
            0.0 :
            value > 1.0 ?
                1.0 :
                value;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static byte Clip8(int value)
        => value < 0 ?
            (byte)0 :
            value > 255 ?
                (byte)255 :
                (byte)value;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static byte Clip8(float value)
        => value < 0.0f ?
            (byte)0 :
            value > 255.0f ?
                (byte)255 :
                (byte)Math.Round(value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static byte Clip8(double value)
        => value < 0.0 ?
            (byte)0 :
            value > 255.0 ?
                (byte)255 :
                (byte)Math.Round(value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ushort Clip16(int value)
        => value < 0 ?
            (ushort)0 :
            value > ushort.MaxValue ?
                ushort.MaxValue :
                (ushort)value;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ushort Clip16(double value)
        => value < 0.0 ?
            (ushort)0 :
            value > ushort.MaxValue ?
                ushort.MaxValue :
                (ushort)Math.Round(value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ushort Clip16(float value)
        => value < 0.0f ?
            (ushort)0 :
            value > ushort.MaxValue ?
                ushort.MaxValue :
                (ushort)Math.Round(value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ushort DeNormalizeClip16(float value)
        => value < 0.0f ?
            (ushort)0 :
            value > 1.0f ?
                ushort.MaxValue :
                (ushort)Math.Round(value * 65535.0f);

    public static Image ToThumbnail(this Image image, int width)
    {
        try
        {
            // Create a thumbnail of the specified width
            var copy = image.Clone(x => { });
            copy
            .Mutate(
                img => img.Resize(new ResizeOptions
                {
                    Size = new Size(width, (int)(image.Height * (double)width / image.Width)),
                    Mode = ResizeMode.Crop
                })
            .GaussianSharpen());
            return copy;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Failed to create thumbnail.", ex);
        }
    }

    public static Frame ToFrame(this Image<RgbaVector> image)
    {
        try
        {
            PixelTypeInfo pixelTypeInfo = image.PixelType;
            if (!pixelTypeInfo.ColorType.HasFlag(PixelColorType.RGB) || (pixelTypeInfo.BitsPerPixel != 128))
            {
                throw new InvalidOperationException($"Unsupported pixel format: {image.PixelType}. Expected RgbaVector.");
            }

            if (image is Image<RgbaVector> rgbFp)
            {
                var frame = new Frame(image.Width, image.Height);
                if (frame.Data is null)
                {
                    throw new OutOfMemoryException("Failed to allocate buffer for a new frame.");
                }

                rgbFp.PixelRgbaBuffer(frame.Data);
                return frame;
            }

            throw new InvalidOperationException($"Unsupported pixel format: {image.PixelType}. Expected RgbaVector.");
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Failed to get pixel buffer.", ex);
        }
    }

    public static void PixelRgbaBuffer(this Image<RgbaVector> image, byte[] rgbaData)
    {
        try
        {
            // Consider: Pin the RGBA buffer and use a pointer 
            image.ProcessPixelRows(accessor =>
            {
                int offset = 0;
                for (int y = 0; y < accessor.Height; y++)
                {
                    var row = accessor.GetRowSpan(y);
                    foreach (ref RgbaVector pixelVector in row)
                    {
                        Rgba32 pixel = pixelVector.ToRgba32(); 
                        rgbaData[offset++] = pixel.R ;
                        rgbaData[offset++] = pixel.G ;
                        rgbaData[offset++] = pixel.B ;
                        rgbaData[offset++] = pixel.A ; 
                    }
                }
            });
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Failed to get pixel buffer.", ex);
        }
    }
}
