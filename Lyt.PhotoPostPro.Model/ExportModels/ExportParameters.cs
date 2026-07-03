namespace Lyt.PhotoPostPro.Model.ExportModels;

public sealed class ExportParameters
{
    public List<ImageParameters> Images { get; set; } = [];

    public ExportParameters()
    {
        // Adds the default export
        this.Images.Add(ImageParameters.Default);

        // Adds the HD sized export
        this.Images.Add(ImageParameters.FullHd);

        // Adds the thumbnail export
        this.Images.Add(ImageParameters.Thumbnail);

        // Adds the HD sized export with black borders
        var hdWithBlackBorders = ImageParameters.FullHd.Clone() ;
        hdWithBlackBorders.JpegQuality = 85; 
        hdWithBlackBorders.WithBorders = true;
        hdWithBlackBorders.BorderStyle = ImageBorderStyle.BlackBorder;
        hdWithBlackBorders.BorderThickness = ImageBorderThickness.Thick;
        hdWithBlackBorders.WithSignature = true;
        hdWithBlackBorders.SignatureKey = Signature.DefaultKey;
        //hdWithBlackBorders.WithWatermark = true;
        //hdWithBlackBorders.WatermarkKey = Watermark.DefaultKey;
        hdWithBlackBorders.PostFix = "_HDBB"; 
        this.Images.Add(hdWithBlackBorders);
    }
}
