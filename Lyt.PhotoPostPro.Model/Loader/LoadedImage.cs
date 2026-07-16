namespace Lyt.PhotoPostPro.Model.Loader;

public sealed class LoadedImage
{
    public bool IsSuccess { get; set; }

    public string ErrorMessage { get; set; } = string.Empty;

    public string Exception { get; set; } = string.Empty;

    public Image<Rgb48>? Image { get; set; }

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

    public static LoadedImage Fail(string message, string exception = "" ) 
        => new() { ErrorMessage = message, Exception = exception };

    public static LoadedImage FullyLoaded(Image<Rgb48>? image, Metadata? metadata) 
        => new() { IsSuccess = true, Image = image, Metadata = metadata };

    public static LoadedImage PreLoaded(Metadata? metadata, byte[] jpgThumbnail)
        => new() { IsSuccess = true, Metadata = metadata, JpgThumbnail = jpgThumbnail };

    public void CreateThumbnail ()
    {
        if (this.Image != null)
        {
            this.JpgThumbnail = ImageLoader.GenerateJpgThumbnailWithClone(this.Image);
        } 
    }
}
