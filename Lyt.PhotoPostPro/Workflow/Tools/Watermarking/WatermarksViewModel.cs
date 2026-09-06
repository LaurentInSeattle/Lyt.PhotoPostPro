namespace Lyt.PhotoPostPro.Workflow.Tools.Watermarking;

// Do not add those ImageSharp namespaces to global using as some class definitions conflict
// with the ones from Avalonia. (Point, Rectangle, etc.) 
//using SixLabors.ImageSharp;
//using SixLabors.ImageSharp.PixelFormats;

public sealed partial class WatermarksViewModel : ViewModel<WatermarksView>, IEditorDataProvider
{
    private readonly PhotoPostProModel model;
    private readonly EditorViewModel editorViewModel;

    public WatermarksViewModel(PhotoPostProModel model)
    {
        this.model = model;
        this.editorViewModel = new EditorViewModel(this, this.model);
    }
    public override void OnViewLoaded()
    {
        base.OnViewLoaded();
        this.View.EditorView.DataContext = this.editorViewModel;
    }

    public override void Activate(object? activationParameters)
    {
        base.Activate(activationParameters);
        var vm = new WatermarkEditViewModel(this.model, this.editorViewModel);
        var editingForm = vm.CreateViewAndBind();
        this.editorViewModel.Populate(this.model.Watermarks.AvailableWatermarks, editingForm);
    }

    public void Refresh()
    {
        this.editorViewModel.Refresh(this.model.Watermarks.AvailableWatermarks);
    }
}
