namespace Lyt.PhotoPostPro.Model.PhotoBook.Slots;

using QuestPDF.Infrastructure;

public sealed class SlotTextStyle
{
    public string Key { get; set; } = string.Empty;

    public string FontFamily { get; set; } = string.Empty;

    public int FontSize { get; set; } = 18;

    // New enum because of serialization 
    public BookTextAlignment TextAlignment { get; set; } = BookTextAlignment.Left;

    // Split because of serialization 
    public byte Red { get; set; } = 255;

    public byte Green { get; set; } = 255;
    
    public byte Blue  { get; set; } = 255;

    [JsonIgnore]
    public Color Color => QuestPDF.Infrastructure.Color.FromRGB(this.Red, this.Green, this.Blue) ;

    public void Colorize(byte red, byte green, byte blue)
    {
        this.Red = red;
        this.Green = green;
        this.Blue = blue; 
    }
}

public sealed class TextSlot : Slot
{
    public string Text { get; set; } = string.Empty;

    public SlotTextStyle Style { get; set; } = new SlotTextStyle();
}