namespace Lyt.PhotoPostPro.Model.ExportModels;

public sealed class Watermark
{
    public const string DefaultKey = "Default";

    private static Watermark defaultWatermark => new() { Key = DefaultKey };

    public static Watermark Default => defaultWatermark;

    public string Key { get; set; } = string.Empty;

    public string FontFamily { get; set; } = "Arial";

    public int FontSize { get; set; } = 142;

    public FontStyle FontStyle { get; set; } = FontStyle.Bold;

    public string Text { get; set; } = "... ... Copyright © 2026 Laurent From San Francisco. All rights reserved. ... ...";

    public uint HexColorArgb { get; set; } = 0x80FFFFFF;

    // No transparency because we are using RGB 
    public Color Color => Color.Parse(this.HexColorArgb.ToString("X"), ColorHexFormat.Argb);
}

/// <summary> Will load from disk ~ LATER </summary>
public sealed class Watermarks
{
    public List<Watermark> AvailableWatermarks { get; set; } = [];

    public Watermarks() => this.AvailableWatermarks.Add(Watermark.Default);

    public Watermark? FromKey(string key) => this.AvailableWatermarks.FirstOrDefault(w => w.Key == key);
}