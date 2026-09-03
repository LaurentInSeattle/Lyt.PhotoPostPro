namespace Lyt.PhotoPostPro.Workflow.Tools.Exporting;

// Do not add those ImageSharp namespaces to global using as some class definitions conflict
// with the ones from Avalonia. (Point, Rectangle, etc.) 
//using SixLabors.ImageSharp;
//using SixLabors.ImageSharp.PixelFormats;

public sealed partial class ExportsViewModel : ViewModel<ExportsView>
{
    private readonly PhotoPostProModel model;
    private readonly IToaster toaster;

    public ExportsViewModel(PhotoPostProModel model, IToaster toaster)
    {
        this.model = model;
        this.toaster = toaster;
    }
}
