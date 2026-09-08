namespace Lyt.PhotoPostPro.Workflow.Tools;

internal static class ToolsStatics
{
    internal static readonly List<int> SupportedFontWeightValues =
    [
        100, 200, 300, 350 ,
        400, 500, 600, 700,
        800, 900, 
        // 950 // Apparently not supported 
    ];

    internal static readonly List<string> SupportedFontWeightText =
    [
        "Thin - 100",
        "Extra Light - 200",
        "Light - 300",
        "Semi Light - 350",

        "Normal / Regular - 400",
        "Medium - 500",
        "Semi Bold - 600",
        "Bold - 700",

        "Extra Bold - 800",
        "Heavy - 900",
        // "Solid - 950", // 950 // Apparently not supported 
    ];

    internal static readonly List<SignatureLocation> SupportedSignaturePlacementValues =
    [
        SignatureLocation.TopLeft,
        SignatureLocation.TopRight,
        SignatureLocation.BottomLeft,
        SignatureLocation.BottomRight,
    ];

    internal static readonly List<string> SupportedSignaturePlacementText =
    [
        "Tools.Editor.TopLeft",
        "Tools.Editor.TopRight",
        "Tools.Editor.BottomLeft",
        "Tools.Editor.BottomRight",
    ];

    internal static readonly List<PppFontStyle> SupportedFontStyleValues =
    [
        PppFontStyle.Regular,
        PppFontStyle.Bold ,
        PppFontStyle.Italic,
        PppFontStyle.BoldItalic,
    ];

    internal static readonly List<string> SupportedFontStyleText =
    [
        "Tools.Editor.Regular",
        "Tools.Editor.Bold",
        "Tools.Editor.Italic",
        "Tools.Editor.BoldItalic",
    ];

    internal static readonly List<OutputFormat> SupportedOutputFormatValues =
    [
        OutputFormat.Jpeg, 
        OutputFormat.WebP, 
        OutputFormat.Png, 
        OutputFormat.Bmp,
    ];

    internal static readonly List<string> SupportedOutputFormatText =
    [
        "Tools.Editor.Jpg",
        "Tools.Editor.Webp",
        "Tools.Editor.Png",
        "Tools.Editor.Bmp",
    ];

    internal static readonly List<ImageBorderStyle> SupportedImageBorderStyleValues =
    [
        ImageBorderStyle.None,
        ImageBorderStyle.BlackBorder,
        ImageBorderStyle.WhiteBorder,
    ];

    internal static readonly List<string> SupportedImageBorderStyleText =
    [
        "Tools.Editor.None",
        "Tools.Editor.BlackBorder",
        "Tools.Editor.WhiteBorder",
    ];

    internal static readonly List<ImageBorderThickness> SupportedImageBorderThicknessValues =
    [
        ImageBorderThickness.Thin,
        ImageBorderThickness.Medium,
        ImageBorderThickness.Thick,
    ];

    internal static readonly List<string> SupportedImageBorderThicknessText =
    [
        "Tools.Editor.Thin",
        "Tools.Editor.Medium",
        "Tools.Editor.Thick",
    ];

    internal static List<FontFamily> FontFamilies()
    {
        var fontCollection = FontManager.Current.SystemFonts;
        var fontFamilies = new List<FontFamily>(fontCollection).OrderBy(x => x.Name).ToList();

        // UGLY HACK !
        // Crash when opening the combo if the InterV font is present in the list
        // Note: Inter is doing fine...
        var toRemove =
            (from family in fontFamilies
             where family.Name.StartsWith("InterV", StringComparison.InvariantCultureIgnoreCase)
             // where family.Name.StartsWith("Inter", StringComparison.InvariantCultureIgnoreCase) 
             select family).ToList();
        if (toRemove.Count > 0)
        {
            foreach (var family in toRemove)
            {
                fontFamilies.Remove(family);
            }
        }

        return fontFamilies;
    }
}
