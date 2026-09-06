namespace Lyt.PhotoPostPro.Model.Export;

public sealed class ImageExport : IEditable
{
    public string FriendlyName { get; set; } = string.Empty;

    public ExportAction Action { get; set; } = ExportAction.None;

    public int Dimension { get; set; } = 1920;

    // Target size in megabytes when action is set to ExportAction.ToFileSize
    public float MegaBytes { get; set; } = 1.0f;

    public OutputFormat OutputFormat { get; set; } = OutputFormat.Jpeg;

    public int JpegQuality { get; set; } = 95;

    public bool IsGalleryFormat { get; set; } = false;

    public bool WithSignature { get; set; } = false;

    public string SignatureName { get; set; } = string.Empty;

    public bool WithWatermark { get; set; } = false;

    public string WatermarkName { get; set; } = string.Empty;

    public bool WithBorders { get; set; } = false;

    public ImageBorderStyle BorderStyle { get; set; } = ImageBorderStyle.None;

    public ImageBorderThickness BorderThickness { get; set; } = ImageBorderThickness.Thick;

    // String added to filename to identify the export type
    public string PostFix { get; set; } = string.Empty;

    public ImageExport Clone()
        =>  new()
            {
                Action = this.Action,
                Dimension = this.Dimension,
                MegaBytes = this.MegaBytes,
                OutputFormat = this.OutputFormat,
                JpegQuality = this.JpegQuality,
                IsGalleryFormat = this.IsGalleryFormat,
                WithSignature = this.WithSignature,
                SignatureName = this.SignatureName,
                WithWatermark = this.WithWatermark,
                WatermarkName = this.WatermarkName,
                WithBorders = this.WithBorders,
                BorderStyle = this.BorderStyle,
                BorderThickness = this.BorderThickness,
                PostFix = this.PostFix
            };

    // Default
    //      Original size, no name change, very high JPG quality, gallery format, no watermark,
    //      no signature, no borders, no postfix
    public static ImageExport Default => new() 
    {
        FriendlyName = "Best Quality",
        IsGalleryFormat = true 
    } ;

    // Resized to Full HD in longuest dimension, high JPG quality, no watermark, no signature, no borders
    public static ImageExport FullHd =>
        new()
        {
            FriendlyName = "Full HD",
            PostFix = "_HD",
            Action = ExportAction.ToDimensions,
            Dimension = 1920,
            OutputFormat = OutputFormat.Jpeg,
            JpegQuality = 90,
        };

    // Resized to 480 pixels in longuest dimension, medium JPG quality, no watermark, no signature, no borders
    public static ImageExport ThumbnailLibrary =>
        new()
        {
            FriendlyName = "Thumbnail",
            PostFix = "_THUMB_EDIT",
            Action = ExportAction.ToDimensions,
            Dimension = 480,
            OutputFormat = OutputFormat.Jpeg,
            JpegQuality = 85,
        };

    // Resized to 480 pixels in longuest dimension, medium JPG quality, no watermark, no signature, no borders
    public static ImageExport Thumbnail =>
        new()
        {
            PostFix = "_THUMB_EDIT",
            Action = ExportAction.ToDimensions,
            Dimension = 480,
            OutputFormat = OutputFormat.Jpeg,
            JpegQuality = 85,
        };

    [JsonIgnore]
    public string FileExtension => this.OutputFormat.FileExtension();

    [JsonIgnore]
    public IImageEncoder ImageEncoder => this.OutputFormat.ImageEncoder(this.JpegQuality);
}
