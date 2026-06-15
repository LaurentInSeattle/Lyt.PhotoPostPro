namespace Lyt.PhotoPostPro.Model.Utilities;

public static class ImagingUtilities
{
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

    public static Frame ToFrame(this Image<Rgb48> image)
    {
        try
        {
            PixelTypeInfo pixelTypeInfo = image.PixelType;
            if (!pixelTypeInfo.ColorType.HasFlag(PixelColorType.RGB) || (pixelTypeInfo.BitsPerPixel != 48))
            {
                throw new InvalidOperationException($"Unsupported pixel format: {image.PixelType}. Expected Rgb48.");
            }

            if (image is Image<Rgb48> rgb48)
            {
                var frame = new Frame(image.Width, image.Height);
                if (frame.Data is null)
                {
                    throw new OutOfMemoryException("Failed to allocate buffer for a new frame.");
                }

                rgb48.PixelRgbaBuffer(frame.Data);
                return frame;
            }

            throw new InvalidOperationException($"Unsupported pixel format: {image.PixelType}. Expected Rgb48.");
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Failed to get pixel buffer.", ex);
        }
    }

    public static void PixelRgbaBuffer(this Image<Rgb48> image, byte[] rgbaData)
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
                    foreach (ref Rgb48 pixel in row)
                    {
                        // Rgb48 stores 16-bit values: Scale them to 8-bit (0-255) by shifting >> 8.
                        rgbaData[offset++] = (byte)(pixel.R >> 8);
                        rgbaData[offset++] = (byte)(pixel.G >> 8);
                        rgbaData[offset++] = (byte)(pixel.B >> 8);
                        rgbaData[offset++] = 255; // Alpha channel (fully opaque)
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
