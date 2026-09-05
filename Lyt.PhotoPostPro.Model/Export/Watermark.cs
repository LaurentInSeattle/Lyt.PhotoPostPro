namespace Lyt.PhotoPostPro.Model.Export;

public sealed class Watermark : IEditable
{
    public const string DefaultName = "Default Watermark";

    private static Watermark DefaultWatermark => new() { FriendlyName = DefaultName };

    public static Watermark Default => DefaultWatermark;


    public string FriendlyName { get; set; } = string.Empty;

    public string FontFamily { get; set; } = "Arial";

    public int FontSize { get; set; } = 142;

    public PppFontStyle PppFontStyle { get; set; } = PppFontStyle.Bold;

    public string Text { get; set; } = "... ... Copyright © 2026 Laurent. All rights reserved. ... ...";

    public uint HexColorArgb { get; set; } = 0x80FFFFFF;

    [JsonIgnore]
    public FontStyle FontStyle => (FontStyle)(int)this.PppFontStyle;

    // TODO:
    // Implement transparency because we are not using RGB any longer 
    [JsonIgnore]
    public Color Color => Color.Parse(this.HexColorArgb.ToString("X"), ColorHexFormat.Argb);
}
