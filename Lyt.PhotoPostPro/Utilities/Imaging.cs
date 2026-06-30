namespace Lyt.PhotoPostPro.Utilities;

using System.Runtime.Intrinsics.X86;

public static class Imaging
{
    public static unsafe WriteableBitmap ToWriteableBitmap(this Frame frame)
    {
        try
        {
            byte[]? pixelData = frame.Data;
            if ( pixelData is null || pixelData.Length < 8 )
            {
                throw new InvalidOperationException("Frame data is null or empty.");
            }

            int width = frame.Width;
            int height = frame.Height;
            if (width <= 0 || height <= 0)
            {
                throw new InvalidOperationException("Frame dimensions are invalid.");
            }

            var avaloniaBitmap =
                new WriteableBitmap(
                    new PixelSize(width, height), new Vector(96, 96), PixelFormat.Rgba8888, AlphaFormat.Premul);
            using (ILockedFramebuffer lockedBuffer = avaloniaBitmap.Lock())
            {
                // Get pointers to the source and destination
                IntPtr destPtr = lockedBuffer.Address;

                // Determine size to copy
                int size = frame.ByteCount;

                // Perform the direct memory copy
                unsafe
                {
                    fixed (byte* p = frame.Data)
                    {
                        IntPtr sourcePtr = (IntPtr)p;
                        Buffer.MemoryCopy(
                            sourcePtr.ToPointer(),
                            destPtr.ToPointer(),
                            lockedBuffer.RowBytes * avaloniaBitmap.PixelSize.Height,
                            size);
                    }
                }
            }

            return avaloniaBitmap;

        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Failed to convert pixel data to WriteableBitmap.", ex);
        }
    }

    public static Color? GetPixelColor(this WriteableBitmap bitmap, int x, int y)
    {
        // Ensure target coordinates are within image boundaries
        if (x < 0 || x >= bitmap.PixelSize.Width || y < 0 || y >= bitmap.PixelSize.Height)
        {
            return null;
        }

        // Lock the framebuffer
        using ILockedFramebuffer framebuffer = bitmap.Lock();
        IntPtr backBuffer = framebuffer.Address;
        int stride = framebuffer.RowBytes;
        PixelFormat format = framebuffer.Format;

        unsafe
        {
            byte* ptr = (byte*)backBuffer;

            // Handle specific pixel formats
            if (format == PixelFormat.Bgra8888)
            {
                // 4 bytes per pixel: Blue, Green, Red, Alpha
                int offset = (y * stride) + (x * 4);
                byte b = ptr[offset];
                byte g = ptr[offset + 1];
                byte r = ptr[offset + 2];
                byte a = ptr[offset + 3];

                return Color.FromArgb(a, r, g, b);
            }
            else if (format == PixelFormat.Rgba8888)
            {
                // 4 bytes per pixel: Red, Green, Blue, Alpha
                int offset = (y * stride) + (x * 4);
                byte r = ptr[offset];
                byte g = ptr[offset + 1];
                byte b = ptr[offset + 2];
                byte a = ptr[offset + 3];

                return Color.FromArgb(a, r, g, b);
            }

            throw new NotSupportedException($"Pixel format {format} is not supported.");
        }
    }

    // Average color around the provided pixel 
    public static Color GetColorAroundPixel(this WriteableBitmap bitmap, int x, int y)
    {
        // Ensure target coordinates are within image boundaries, minus one
        if (x < 1 || x >= bitmap.PixelSize.Width - 1 || y < 1 || y >= bitmap.PixelSize.Height - 1)
        {
            throw new ArgumentException($"Inavalid pixel coordinates.");
        }

        // Lock the framebuffer
        using ILockedFramebuffer framebuffer = bitmap.Lock();
        IntPtr backBuffer = framebuffer.Address;
        int stride = framebuffer.RowBytes;
        PixelFormat format = framebuffer.Format;

        int rSum = 0;
        int gSum = 0;
        int bSum = 0;

        unsafe
        {
            byte* ptr = (byte*)backBuffer;

            // Handle specific pixel formats
            if (format == PixelFormat.Bgra8888)
            {
                // 4 bytes per pixel: Blue, Green, Red, Alpha
                for ( int xx = x - 1; xx <= x + 1; ++ xx )
                {
                    for (int yy = y - 1; yy <= y + 1; ++yy)
                    {
                        int offset = (yy * stride) + (xx * 4);
                        byte b = ptr[offset];
                        byte g = ptr[offset + 1];
                        byte r = ptr[offset + 2];

                        rSum += r; 
                        gSum += g;
                        bSum += b;
                    }
                }
            }
            else if (format == PixelFormat.Rgba8888)
            {
                // 4 bytes per pixel: Red, Green, Blue, Alpha
                for (int xx = x - 1; xx <= x + 1; ++xx)
                {
                    for (int yy = y - 1; yy <= y + 1; ++yy)
                    {
                        int offset = (yy * stride) + (xx * 4);
                        byte r = ptr[offset];
                        byte g = ptr[offset + 1];
                        byte b = ptr[offset + 2];

                        rSum += r;
                        gSum += g;
                        bSum += b;
                    }
                }
            }

            if ( (format == PixelFormat.Bgra8888) || (format == PixelFormat.Rgba8888))
            {
                int avgR = (int)((0.5f + rSum) / 9.0f);
                int avgG = (int)((0.5f + gSum) / 9.0f);
                int avgB = (int)((0.5f + bSum) / 9.0f);

                return Color.FromArgb(255, (byte)avgR, (byte)avgG, (byte)avgB);
            }

            throw new NotSupportedException($"Pixel format {format} is not supported.");
        }
    }
}