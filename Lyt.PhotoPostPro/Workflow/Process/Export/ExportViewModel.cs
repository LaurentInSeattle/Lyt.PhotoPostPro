namespace Lyt.PhotoPostPro.Workflow.Process.Export;

public sealed partial class ExportViewModel : StepViewModel<ExportView>
{
    [ObservableProperty]
    public partial ObservableCollection<ImageExportViewModel> ImageExports { get; set; } = [];

    public List<ImageExportViewModel> SelectedImageExports { get; set; } = [];

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

        // Deliberately making a copy: see Remove below 
        var copy = this.model.ImageExports.AvailableImageExports.ToList();

        var list = new List<ImageExportViewModel>();

        // galleryExport on top 
        var galleryExport = (from export in copy where export.IsGalleryFormat select export).FirstOrDefault(); 
        if ( galleryExport is not null)
        {
            list.Add(new ImageExportViewModel(this, galleryExport));
            copy.Remove(galleryExport); 
        }

        // All other in alpha order ascending 
        var sorted = from export in copy orderby export.FriendlyName ascending select export ; 
        foreach (var imageExport in sorted)
        {
            list.Add(new ImageExportViewModel(this, imageExport)); 
        }

        this.ImageExports = new(list); 
    }
}
