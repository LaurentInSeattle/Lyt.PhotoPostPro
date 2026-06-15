namespace Lyt.PhotoPostPro.Model.Frames;

/// <summary> 
/// Plain old data class somewhat equivalent to a Windows Bitmap for holding raw image frames 
/// and performing basic operations.
///                     ==>     IMPORTANT !   Always RGBA 32   <==
/// </summary>
public sealed partial class Frame : IDisposable
{
    public static readonly ParallelOptions ParallelOptions; 

    static Frame()
    {
        Frame.ParallelOptions = 
            new ParallelOptions 
            { 
                MaxDegreeOfParallelism = Environment.ProcessorCount,                 
            };
    }

    private bool disposedValue;

    /// <summary> Creates a new Frame using the provided dimensions. </summary>
    /// <remarks> The data buffer NOT is zeroed therefore, the new frame is NOT all black.</remarks>
    public Frame(int width, int height)
    {
        this.Width = width;
        this.Height = height;
        this.Data = ArrayPool<byte>.Shared.Rent(this.ByteCount);
    }

    /// <summary> Creates a new Frame using the dimensions provided in the given frame. </summary>
    /// <remarks> The data buffer NOT is zeroed therefore, the new frame is NOT all black.</remarks>
    public Frame(Frame frame)
    {
        this.Width = frame.Width;
        this.Height = frame.Height;
        this.Data = ArrayPool<byte>.Shared.Rent(this.ByteCount);
    }

    public int Width { get; private set; }

    public int Height { get; private set; }

    public byte[]? Data { get; private set; }

    public int PixelCount => this.Width * this.Height;

    public int ByteCount => this.PixelCount * BytesPerPixel;

    public static int BitsPerPixel => 32;

    public static int BytesPerPixel => 4 ;

    public Frame DeepClone()
    {
        var newFrame = (Frame)this.MemberwiseClone();
        int length = this.ByteCount;
        if (this.Data is not null && newFrame.Data is not null)
        {
            // Avoid Buffer.BlockCopy as it creates garbage !
            Array.Copy(this.Data, newFrame.Data, length);
        } 

        return newFrame;
    }

    public void CopyDataTo(Frame newFrame)
    {
        int length = this.ByteCount;
        if (this.Data is not null && newFrame.Data is not null)
        {
            // Avoid Buffer.BlockCopy as it creates garbage !
            Array.Copy(this.Data, newFrame.Data, length);
        }
    }

    private void Dispose(bool disposing)
    {
        if (!disposedValue)
        {
            if (disposing)
            {
                // dispose managed state (managed objects)
                // None for now
            }

            // free unmanaged resources (unmanaged objects) and override finalizer
            // None for now 

            // Recycle the data buffer if it exists.
            // This will allow the buffer to be reused by other frames.
            if (this.Data is not null)
            {
                ArrayPool<byte>.Shared.Return(this.Data);
                this.Data = null;
            }

            disposedValue = true;
        }
    }

    // Not needed for now.
    // Override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
    // ~Frame()
    // {
    //     // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
    //     Dispose(disposing: false);
    // }

    public void Dispose()
    {
        // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        this.Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}