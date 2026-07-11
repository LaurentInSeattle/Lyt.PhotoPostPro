namespace Lyt.PhotoPostPro.Model.Loader;

public sealed class LoadedImage
{
    public bool IsSuccess { get; set; }

    public string ErrorMessage { get; set; } = string.Empty;

    public Image<Rgb48>? Image { get; set; }

    public Metadata? Metadata { get; set; }

    public byte[]? JpgThumbnail { get; set; }

    public static LoadedImage Fail(string message) => new() { ErrorMessage = message };

    public static LoadedImage Loaded(Image<Rgb48>? image, Metadata? metadata) 
        => new() { Image = image, Metadata = metadata };

    public static LoadedImage PreLoaded(Metadata? metadata, byte[] jpgThumbnail)
        => new() { Metadata = metadata, JpgThumbnail = jpgThumbnail };
}
