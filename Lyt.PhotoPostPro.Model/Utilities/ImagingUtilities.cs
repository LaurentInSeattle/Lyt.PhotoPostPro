namespace Lyt.PhotoPostPro.Model.Utilities;

public static class ImagingUtilities
{
#pragma warning disable CA2211 
    // Non-constant fields should not be visible

    public static Half hZero = (Half)0.0f;
    public static Half hOne = (Half)1.0f;

#pragma warning restore CA2211 


    public const ushort pixMaxU = ushort.MaxValue;
    public const float pixMaxF = 65535.0f;
    public const double pixMaxD = 65535.0;

    public const int pixRangeI = (int)(1 + ushort.MaxValue);
    public const float pixRangeF = 65536.0f;
    public const double pixRangeD = 65536.0;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Half ClipH(Half value)
        => value < hZero ?
            hZero :
            value > hOne ?
                hOne :
                value;

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

    public static Frame ToFrame(this Image<RgbaHalf> image)
    {
        try
        {
            PixelTypeInfo pixelTypeInfo = image.PixelType;
            if (!pixelTypeInfo.ColorType.HasFlag(PixelColorType.RGB) || (pixelTypeInfo.BitsPerPixel != 64))
            {
                throw new InvalidOperationException($"Unsupported pixel format: {image.PixelType}. Expected RgbaHalf.");
            }

            if (image is Image<RgbaHalf> rgbFp)
            {
                var frame = new Frame(image.Width, image.Height);
                if (frame.Data is null)
                {
                    throw new OutOfMemoryException("Failed to allocate buffer for a new frame.");
                }

                byte[] rgbaData = frame.Data;

                // Directly grab the contiguous Memory reference
                if (image.DangerousTryGetSinglePixelMemory(out Memory<RgbaHalf> pixelMemory))
                {
                    // Access the span directly without copying any data
                    int offset = 0;
                    Span<RgbaHalf> pixelSpan = pixelMemory.Span;
                    for (int i = 0; i < pixelSpan.Length; i++)
                    {
                        RgbaHalf pixelVector = pixelSpan[i];
                        var pixel = pixelVector.ToRgba32();
                        rgbaData[offset++] = pixel.R;
                        rgbaData[offset++] = pixel.G;
                        rgbaData[offset++] = pixel.B;
                        rgbaData[offset++] = pixel.A;
                    }
                }
                else
                {
                    // Fallback if memory padding or fragmentation prevented a single contiguous buffer
                    image.ProcessPixelRows(accessor =>
                    {
                        int offset = 0;
                        for (int y = 0; y < accessor.Height; y++)
                        {
                            var row = accessor.GetRowSpan(y);
                            foreach (ref RgbaHalf pixelVector in row)
                            {
                                var pixel = pixelVector.ToRgba32();
                                rgbaData[offset++] = pixel.R;
                                rgbaData[offset++] = pixel.G;
                                rgbaData[offset++] = pixel.B;
                                rgbaData[offset++] = pixel.A;
                            }
                        }
                    });
                }

                return frame;
            }

            throw new InvalidOperationException($"Unsupported pixel format: {image.PixelType}. Expected RgbaHalf.");
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Failed to get pixel buffer.", ex);
        }
    }

    public static void PixelRgbaBuffer(this Image<RgbaHalf> image, byte[] rgbaData)
    {
        try
        {
            // Consider: Pin the RGBA buffer and use a pointer 
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Failed to get pixel buffer.", ex);
        }
    }
}
