namespace Lyt.PhotoPostPro.Workflow.Documentation;

// Do not add those ImageSharp namespaces to global using as some class definitions conflict
// with the ones from Avalonia. (Point, Rectangle, etc.) 
//using SixLabors.ImageSharp;
//using SixLabors.ImageSharp.PixelFormats;

public sealed partial class DocumentationViewModel : ViewModel<DocumentationView>
{
    private readonly PhotoPostProModel model;
    private readonly IToaster toaster;

    public DocumentationViewModel(PhotoPostProModel model, IToaster toaster)
    {
        this.model = model;
        this.toaster = toaster;
    }
}
