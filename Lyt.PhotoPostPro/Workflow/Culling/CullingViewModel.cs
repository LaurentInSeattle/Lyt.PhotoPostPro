namespace Lyt.PhotoPostPro.Workflow.Culling;

// Do not add those ImageSharp namespaces to global using as some class definitions conflict
// with the ones from Avalonia. (Point, Rectangle, etc.) 
//using SixLabors.ImageSharp;
//using SixLabors.ImageSharp.PixelFormats;

public sealed partial class CullingViewModel : ViewModel<CullingView>
{
    private readonly PhotoPostProModel model;
    private readonly IToaster toaster;

    public CullingViewModel(PhotoPostProModel model, IToaster toaster)
    {
        this.model = model;
        this.toaster = toaster;
    }
}
