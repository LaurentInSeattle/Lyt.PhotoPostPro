namespace Lyt.PhotoPostPro.Workflow.Book.Edit;

using Slots = Model.PhotoBook.Slots;

using Typography = Model.PhotoBook.Typography;

public static class TextStyleUtilities
{
    public static SolidColorBrush FromTextStyle(Typography.SlotTextStyle textStyle)
        => new(Color.FromRgb(textStyle.Red, textStyle.Green, textStyle.Blue));

    public static TextAlignment FromTextStyleTextAlignment(Typography.SlotTextStyle textStyle)
        => textStyle.TextAlignment switch
        {
            BookTextAlignment.Center => TextAlignment.Center,
            BookTextAlignment.Right => TextAlignment.Right,
            _ => TextAlignment.Left,
        };

    public static (FontStyle, FontWeight) FromTextStyleFontStyle(Typography.SlotTextStyle textStyle)
    {
        var fontStyle = textStyle.FontStyle switch
        {
            BookFontStyle.BoldItalic or BookFontStyle.Italic => FontStyle.Italic,
            _ => FontStyle.Normal,
        };

        var fontWeight = textStyle.FontStyle switch
        {
            BookFontStyle.BoldItalic or BookFontStyle.Bold => FontWeight.Bold,
            _ => FontWeight.Normal,
        };

        return (fontStyle, fontWeight);
    }

    public static Stretch StretchFromImageSlot(Slots.ImageSlot slot)
        => slot.ImageStretch switch
        {
            BookImageStretch.Stretch => Stretch.Fill,
            _ => Stretch.Uniform,
        };
}
