namespace Lyt.PhotoPostPro.Model.Export;

/// <summary> Will load from disk ~ LATER </summary>
public sealed class ImageExportsCollection
{
    public List<ImageExport> AvailableImageExports { get; set; } = [];

    public ImageExportsCollection()
    {
        // Adds the default export
        this.AvailableImageExports.Add(ImageExport.Default);

        // Adds the HD sized export
        this.AvailableImageExports.Add(ImageExport.FullHd);

        // Adds the HD sized export with black borders
        var hdWithBlackBorders = ImageExport.FullHd.Clone();
        hdWithBlackBorders.JpegQuality = ImageLoader.ThumbnailQuality;
        hdWithBlackBorders.WithBorders = true;
        hdWithBlackBorders.BorderStyle = ImageBorderStyle.BlackBorder;
        hdWithBlackBorders.BorderThickness = ImageBorderThickness.Thin;
        hdWithBlackBorders.WithSignature = true;
        hdWithBlackBorders.SignatureKey = Signature.DefaultName;
        //hdWithBlackBorders.WithWatermark = true;
        //hdWithBlackBorders.WatermarkKey = Watermark.DefaultKey;
        hdWithBlackBorders.PostFix = "_HDBB";
        this.AvailableImageExports.Add(hdWithBlackBorders);
    }

    public ImageExport? FromFriendlyName(string friendlyName)
            => this.AvailableImageExports.FirstOrDefault(s => s.FriendlyName == friendlyName);
}
