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
