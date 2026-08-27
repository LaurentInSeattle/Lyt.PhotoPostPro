namespace Lyt.PhotoPostPro.Model.Export;

public sealed class Signature
{
    public const string DefaultKey = "Default";

    private static Signature DefaultSignature => new() { Key = DefaultKey };

    public static Signature Default => DefaultSignature;

    public string Key { get; set; } = string.Empty;

    public string FontFamily { get; set; } = "Segoe Script";

    public int FontSize { get; set; } = 26;

    public PppFontStyle PppFontStyle { get; set; } = PppFontStyle.Italic;

    public string Text { get; set; } = "Edited with Photo Rebel";

    public SignatureLocation Location { get; set; } = SignatureLocation.BottomRight;

    public uint HexColorArgb { get; set; } = 0xFFFFFFFF;

    public FontStyle FontStyle => (FontStyle)(int)this.PppFontStyle;

    public Color Color => Color.Parse (this.HexColorArgb.ToString("X"), ColorHexFormat.Argb);
}

/// <summary> Will load from disk ~ LATER </summary>
public sealed class Signatures
{
    public List<Signature> AvailableSignatures { get; set; } = [];

    public Signatures() => this.AvailableSignatures.Add(Signature.Default);

    public Signature? FromKey(string key) => this.AvailableSignatures.FirstOrDefault(s => s.Key == key);
}