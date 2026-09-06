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
}
