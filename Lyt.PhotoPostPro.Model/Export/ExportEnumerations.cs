namespace Lyt.PhotoPostPro.Model.Export;

[JsonConverter(typeof(JsonStringEnumConverter<ExportAction>))]
public enum ExportAction
{
    None,
    ToScale,
    ToDimensions,
    ToFileSize,
}

[JsonConverter(typeof(JsonStringEnumConverter<OutputFormat>))]
public enum OutputFormat
{
    Jpeg,
    Png,
    Bmp,
}

[JsonConverter(typeof(JsonStringEnumConverter<ImageBorderStyle>))]
public enum ImageBorderStyle
{
    None,
    BlackBorder,
    WhiteBorder,
    Custom,
}

[JsonConverter(typeof(JsonStringEnumConverter<ImageBorderThickness>))]
public enum ImageBorderThickness
{
    Thick,
    Thin,
    Custom,
}

[JsonConverter(typeof(JsonStringEnumConverter<SignatureLocation>))]
public enum SignatureLocation
{
    TopLeft,
    TopRight,
    BottomLeft,
    BottomRight,
}

[Flags]
[JsonConverter(typeof(JsonStringEnumConverter<PppFontStyle>))]
public enum PppFontStyle
{
    Regular = 0,
    Bold = 1,
    Italic = 2,
    BoldItalic = 3
}

public static class OutputFomatExtensions
{
    public static string FileExtension(this OutputFormat outputFormat)
        => outputFormat switch
        {
            OutputFormat.Jpeg => ".jpg",
            OutputFormat.Png => ".png",
            OutputFormat.Bmp => ".bmp",
            _ => throw new NotImplementedException(),
        };

    public static IImageEncoder ImageEncoder(this OutputFormat outputFormat, int jpegQuality)
        => outputFormat switch
        {
            OutputFormat.Jpeg => new JpegEncoder() { ColorType = JpegColorType.Rgb, Quality = jpegQuality },
            OutputFormat.Png => new PngEncoder() { ColorType = PngColorType.Rgb },
            OutputFormat.Bmp => new BmpEncoder() { BitsPerPixel = BmpBitsPerPixel.Bit24 },
            _ => throw new NotImplementedException(),
        };
}