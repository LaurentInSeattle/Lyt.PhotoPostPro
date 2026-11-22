namespace Lyt.PhotoPostPro.Model.PhotoBook.Typography;

public sealed partial class SlotTextStyle : IEditable
{
#pragma warning disable CA2211 // Non-constant fields should not be visible

    public static SlotTextStyle Title => new()
    {
        FriendlyName = "Title",
        IsBuiltIn = true,
        FontFamily = "Segoe Script",
        FontSize = 42,
        FontStyle = BookFontStyle.Bold,
        TextAlignment = BookTextAlignment.Center,
        Red = 255,
        Blue = 255,
        Green = 255,
    };

    public static SlotTextStyle Header_1 => new()
    {
        FriendlyName = "Header_1",
        IsBuiltIn = true,
        FontFamily = "Segoe Script",
        FontSize = 36,
        FontStyle = BookFontStyle.Regular,
        TextAlignment = BookTextAlignment.Left,
        Red = 255,
        Blue = 255,
        Green = 255,
    };

    public static SlotTextStyle Header_2 => new()
    {
        FriendlyName = "Header_2",
        IsBuiltIn = true,
        FontFamily = "Segoe Script",
        FontSize = 30,
        FontStyle = BookFontStyle.Bold,
        TextAlignment = BookTextAlignment.Left,
        Red = 255,
        Blue = 255,
        Green = 255,
    };

    public static SlotTextStyle LargeBodyText => new()
    {
        FriendlyName = "LargeBodyText",
        IsBuiltIn = true,
        FontFamily = "Segoe Script",
        FontSize = 24,
        FontStyle = BookFontStyle.Bold,
        TextAlignment = BookTextAlignment.Left,
        Red = 255,
        Blue = 255,
        Green = 255,
    };

    public static SlotTextStyle SmallBodyText => new()
    {
        FriendlyName = "SmallBodyText",
        IsBuiltIn = true,
        FontFamily = "Segoe Script",
        FontSize = 18,
        FontStyle = BookFontStyle.Regular,
        TextAlignment = BookTextAlignment.Left,
        Red = 255,
        Blue = 255,
        Green = 255,
    };

#pragma warning restore CA2211 // Non-constant fields should not be visible
}
