namespace Lyt.PhotoPostPro.Model.ProcessModels;

public sealed class ImageParameters
{
    public ExportAction Action { get; set; } = ExportAction.None;

    public int Dimension { get; set; } = 1920;

    public float ScaleFactor { get; set; } = 1.0f;

    // Target size in megabytes when action is set to ExportAction.ToFileSize
    public float MegaBytes { get; set; } = 1.0f;

    public OutputFormat OutputFormat { get; set; } = OutputFormat.Jpeg;

    public int JpegQuality { get; set; } = 97;

    public bool WithSignature { get; set; } = false;

    public string SignatureKey { get; set; } = string.Empty;

    public bool WithWatermark { get; set; } = false;

    public string WatermarkKey { get; set; } = string.Empty;

    public bool WithBorders { get; set; } = false;

    public ImageBorderStyle BorderStyle { get; set; } = ImageBorderStyle.None;

    public string BorderStyleKey { get; set; } = string.Empty;

    // String added to filename to identify the export type
    public string PostFix { get; set; } = string.Empty;

    // Original size, no name change, very high JPG quality, no watermark, no signature, no borders, no postfix
    public static ImageParameters Default => new();

    // Resized to Full HD in longuest dimension, high JPG quality, no watermark, no signature, no borders
    public static ImageParameters FullHd =>
        new()
        {
            PostFix = "_HD",
            Action = ExportAction.ToDimensions,
            Dimension = 1920,
            OutputFormat = OutputFormat.Jpeg,
            JpegQuality = 90,
        };

    // Resized to 480 pixels in longuest dimension, medium JPG quality, no watermark, no signature, no borders
    public static ImageParameters Thumbnail =>
        new()
        {
            PostFix = "_THUMB",
            Action = ExportAction.ToDimensions,
            Dimension = 480,
            OutputFormat = OutputFormat.Jpeg,
            JpegQuality = 86,
        };

    public string FileExtension
        => this.OutputFormat switch
        {
            OutputFormat.Jpeg => ".jpg",
            OutputFormat.Png => ".png",
            OutputFormat.Bmp => ".bmp",
            _ => throw new NotImplementedException(),
        };

    public IImageEncoder ImageEncoder
        => this.OutputFormat switch
        {
            OutputFormat.Jpeg => new JpegEncoder() { ColorType = JpegColorType.Rgb, Quality = this.JpegQuality },
            OutputFormat.Png => new PngEncoder() { ColorType = PngColorType.Rgb },
            OutputFormat.Bmp => new BmpEncoder() { BitsPerPixel = BmpBitsPerPixel.Bit24 },
            _ => throw new NotImplementedException(),
        };

}
