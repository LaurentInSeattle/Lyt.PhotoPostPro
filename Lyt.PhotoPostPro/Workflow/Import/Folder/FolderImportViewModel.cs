namespace Lyt.PhotoPostPro.Workflow.Import.Folder;

// Do not add those ImageSharp namespaces to global using as some class definitions conflict
// with the ones from Avalonia. (Point, Rectangle, etc.) 
//using SixLabors.ImageSharp;
//using SixLabors.ImageSharp.PixelFormats;

public sealed partial class FolderImportViewModel : ViewModel<FolderImportView>
{
    private readonly PhotoPostProModel model;
    private readonly IToaster toaster;

    [ObservableProperty]
    public partial bool IsFolderMode { get; set; } 

    public FolderImportViewModel(PhotoPostProModel model, IToaster toaster)
    {
        this.model = model;
        this.toaster = toaster;
        this.IsFolderMode = false; 
    }

    public void OnFolderDrop(string path)
    {
        var statistics = ImageLoader.InspectFolder(path);
    }

#pragma warning disable CA1822 // Mark members as static
    // RelayCommand's cannot be static 

#pragma warning restore CA1822
}
