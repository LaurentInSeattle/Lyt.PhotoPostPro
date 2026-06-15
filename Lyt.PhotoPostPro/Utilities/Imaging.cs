namespace Lyt.PhotoPostPro.Utilities;

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
} 