namespace Lyt.PhotoPostPro.Messaging;

public enum ActivatedView : int
{
    GoBack,
    Exit,

    // Main selector views
    Folder,
    Language,
    Single,
    Process,

    // Secondary views (activated from ProcessView) 
    // In alphabetical order, not the workflow order, which is determined elsewhere.
    Cleanup, 
    Color, 
    Compose,
    Contrast,
    Denoise,
    Export,
    Exposure,
    Orient,
    Sharpen,
    Straighten,
    TouchUp,
    Recovery,
    WhiteBalance,
    Vignette,
    Filters,
}
