namespace Lyt.PhotoPostPro.Model.Export;

public sealed class ExportParameters
{
    public List<ImageExport> Images { get; set; } = [];

    public ExportParameters()
    {
        // Default stuff because we have no UI for that yet 

        // Adds the default export
        this.Images.Add(ImageExport.Default);

        // Adds the HD sized export
        this.Images.Add(ImageExport.FullHd);

        // Adds the thumbnail export
        // this.Images.Add(ImageParameters.Thumbnail);

        // Adds the HD sized export with black borders
        var hdWithBlackBorders = ImageExport.FullHd.Clone() ;
        hdWithBlackBorders.JpegQuality = ImageLoader.ThumbnailQuality; 
        hdWithBlackBorders.WithBorders = true;
        hdWithBlackBorders.BorderStyle = ImageBorderStyle.BlackBorder;
        hdWithBlackBorders.BorderThickness = ImageBorderThickness.Thin;
        hdWithBlackBorders.WithSignature = true;
        hdWithBlackBorders.SignatureKey = Signature.DefaultName;
        //hdWithBlackBorders.WithWatermark = true;
        //hdWithBlackBorders.WatermarkKey = Watermark.DefaultKey;
        hdWithBlackBorders.PostFix = "_HDBB"; 
        this.Images.Add(hdWithBlackBorders);
    }
}
