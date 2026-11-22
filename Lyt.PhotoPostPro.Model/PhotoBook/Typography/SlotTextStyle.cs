namespace Lyt.PhotoPostPro.Model.PhotoBook.Typography;

using QuestPDF.Infrastructure;

public sealed partial class SlotTextStyle : IEditable
{
    public string FriendlyName { get; set; } = string.Empty;

    public bool CanDelete => !this.IsBuiltIn; 
    
    public string FontFamily { get; set; } = "Segoe Script";

    public int FontSize { get; set; } = 18;

    public bool IsBuiltIn { get; set; } = false ;

    // New enum because of serialization 
    public BookFontStyle FontStyle { get; set; } = BookFontStyle.Regular;

    // New enum because of serialization 
    public BookTextAlignment TextAlignment { get; set; } = BookTextAlignment.Left;

    // Split into byte because of serialization 
    public byte Red { get; set; } = 255;

    public byte Green { get; set; } = 255;

    public byte Blue { get; set; } = 255;

    [JsonIgnore]
    public Color Color => QuestPDF.Infrastructure.Color.FromRGB(this.Red, this.Green, this.Blue);

    public void Colorize(byte red, byte green, byte blue)
    {
        this.Red = red;
        this.Green = green;
        this.Blue = blue;
    }
}
