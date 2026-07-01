namespace Lyt.PhotoPostPro.Model.ProcessModels;

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
