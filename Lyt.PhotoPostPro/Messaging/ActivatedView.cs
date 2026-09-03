namespace Lyt.PhotoPostPro.Messaging;

public enum ActivatedView : int
{
    // Utilities - not used for now 
    GoBack,
    Exit,

    // Main selector views
    Library,
    Camera,
    Gallery,
    Import,
    Settings,
    Tools,
    Language,

    // Hidden views, not directly accessible from the main selector
    Process,
    Culling,

    // Secondary views (activated from ProcessView) 
    // In alphabetical order, not the workflow order, which is determined elsewhere.
    Cleanup, 
    Color, 
    Compose,
    Contrast,
    Denoise,
    Export,
    Exposure,
    Filters,
    Lut,
    Orient,
    Recovery,
    Sharpen,
    Straighten,
    TouchUp,
    Vignette,
    WhiteBalance,
    Exports,
    Signatures,
    Watermarks,
}
