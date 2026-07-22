namespace Lyt.PhotoPostPro.Model.Loader;

public sealed class LoadedImage
{
    public bool IsSuccess { get; set; }

    public string ErrorMessage { get; set; } = string.Empty;

    public string Exception { get; set; } = string.Empty;

    public Image<RgbaVector>? Image { get; set; }

    public Metadata? Metadata { get; set; }

    public byte[]? JpgThumbnail { get; set; }

    /// <summary> Image is ready and metadata is present, thumbnail may be present or not  </summary>
    public bool IsFullyLoaded
        => this.IsSuccess && this.Image is not null && this.Metadata is not null;

    /// <summary> Image is ready and metadata is present, thumbnail is also present </summary>
    public bool IsFullyLoadedWithThumbnail
        => this.IsSuccess && this.Image is not null && this.JpgThumbnail is not null && this.Metadata is not null;

    /// <summary> Image is not ready but metadata is present and the thumbnail is also present </summary>
    public bool IsPreLoaded
        => this.IsSuccess && this.JpgThumbnail is not null && this.Metadata is not null;

    public static LoadedImage Fail(string message, string exception = "")
        => new() { ErrorMessage = message, Exception = exception };

    public static LoadedImage FullyLoaded(Image<RgbaVector>? image, Metadata? metadata)
        => new() { IsSuccess = true, Image = image, Metadata = metadata };

    public static LoadedImage PreLoaded(Metadata? metadata, byte[] jpgThumbnail)
        => new() { IsSuccess = true, Metadata = metadata, JpgThumbnail = jpgThumbnail };

    public void CreateThumbnail()
    {
        if (this.Image != null)
        {
            this.JpgThumbnail = ImageLoader.GenerateJpgThumbnailWithClone(this.Image);
        }
    }

    /// <summary> Generates rotated source image, if needed. </summary>
    public void RotateIfNeeded()
    {
        if (this.Metadata is null || this.Image is null)
        {
            // We should never called this in such case 
            if (Debugger.IsAttached) { Debugger.Break(); }
            return;
        }

        // Rotate if metadata says so
        if (this.Metadata.IsOrientationActionRequired)
        {
            RotateMode rotateMode = RotateMode.Rotate180;
            if (this.Metadata.OrientationActionRequired == Metadata.OrientationAction.Rotate90Cw)
            {
                rotateMode = RotateMode.Rotate90;
            }
            else if (this.Metadata.OrientationActionRequired == Metadata.OrientationAction.Rotate90Ccw)
            {
                rotateMode = RotateMode.Rotate270;
            }

            this.Image.Mutate(x => x.Rotate(rotateMode));
        }
    }
}
