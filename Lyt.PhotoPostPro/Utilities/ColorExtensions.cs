namespace Lyt.PhotoPostPro.Utilities;

public static class ColorExtensions
{
    public static double Luminance (this global::Avalonia.Media.Color color)
        => (color.R * 0.299 + color.G * 0.587 + color.B * 0.114) / 255.0;
}
