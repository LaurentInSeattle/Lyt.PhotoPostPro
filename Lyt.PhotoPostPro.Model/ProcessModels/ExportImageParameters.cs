namespace Lyt.PhotoPostPro.Model.ProcessModels;

public sealed class ExportImageParameters
{
    public ExportAction Action { get; set; } = ExportAction.ToScale;

    public float ScaleFactor { get; set; } = 1.0f;

    public float MegaBytes { get; set; } = 1.0f;

    public OutputFormat OutputFormat { get; set; } = OutputFormat.Jpeg;

    public float JpegQuality { get; set; } = 0.95f; 

    public bool WithSignature { get; set; } = false;

    public string SignatureKey { get; set; } = string.Empty;

    public bool WithWatermark { get; set; } = false;

    public string WatermarkKey { get; set; } = string.Empty;

    public bool WithBorders { get; set; } = false;

    public ImageBorderStyle BorderStyle {  get ; set; } = ImageBorderStyle.None;

    public string BorderStyleKey { get; set; } = string.Empty;
}
