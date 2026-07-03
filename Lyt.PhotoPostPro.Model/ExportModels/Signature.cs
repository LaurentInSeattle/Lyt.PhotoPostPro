namespace Lyt.PhotoPostPro.Model.ExportModels;

public sealed class Signature
{
    public const string DefaultKey = "Default";

    private static Signature defaultSignature => new() { Key = DefaultKey };

    public static Signature Default => defaultSignature;

    public string Key { get; set; } = string.Empty;

    public string FontFamily { get; set; } = "Segoe Script";

    public int FontSize { get; set; } = 36;

    public FontStyle FontStyle { get; set; } = FontStyle.Italic;

    public string Text { get; set; } = "Edited with Lyt.PhotoPostPro";

    public SignatureLocation Location { get; set; } = SignatureLocation.BottomRight;

    public uint HexColorArgb { get; set; } = 0xFFFFFFFF;

    public Color Color => Color.Parse (this.HexColorArgb.ToString("X"), ColorHexFormat.Argb);
}

/// <summary> Will load from disk ~ LATER </summary>
public sealed class Signatures
{
    public List<Signature> AvailableSignatures { get; set; } = [];

    public Signatures() => this.AvailableSignatures.Add(Signature.Default);

    public Signature? FromKey(string key) => this.AvailableSignatures.FirstOrDefault(s => s.Key == key);
}