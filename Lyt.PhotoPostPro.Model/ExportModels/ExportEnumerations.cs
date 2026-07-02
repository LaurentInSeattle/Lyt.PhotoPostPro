namespace Lyt.PhotoPostPro.Model.ExportModels;

public enum ExportAction
{
    None,
    ToScale,
    ToDimensions,
    ToFileSize,
}

public enum OutputFormat
{
    Jpeg,
    Png,
    Bmp,
}

public enum ImageBorderStyle
{
    None,
    BlackBorder,
    WhiteBorder,
    Custom,
}

public enum ImageBorderThickness
{
    Thick,
    Thin,
    Custom,
}

public enum SignatureLocation
{
    TopLeft,
    TopRight,
    BottomLeft,
    BottomRight,
}
