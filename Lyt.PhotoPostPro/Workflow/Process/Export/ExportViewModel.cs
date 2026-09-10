namespace Lyt.PhotoPostPro.Workflow.Process.Export;

public sealed partial class ExportViewModel : StepViewModel<ExportView>
{
    [ObservableProperty]
    public partial ObservableCollection<ImageExportViewModel> ImageExports { get; set; } = [];

    public List<ImageExportViewModel> SelectedImageExports { get; set; } = [];

    public ExportViewModel() 
    {
    }

    internal void OnExportSelectionChanged()
    {
        this.SelectedImageExports.Clear();
        foreach (var imageExportViewModel in this.ImageExports)
        {
            if (imageExportViewModel.IsExportIncluded)
            {
                this.SelectedImageExports.Add(imageExportViewModel);
            } 
        }
    }

    public override void Activate(object? activationParameters)
    {
        base.Activate(activationParameters);

        var list = new List<ImageExportViewModel>();

        foreach (var imageExport in this.model.ImageExports.AvailableImageExports)
        {
            list.Add(new ImageExportViewModel(this, imageExport)); 
        }

        this.ImageExports = new(list); 
    }
}
