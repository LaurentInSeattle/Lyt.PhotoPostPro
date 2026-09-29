namespace Lyt.PhotoPostPro.Workflow.Documentation;

public sealed partial class DocumentTileViewModel : ViewModel<DocumentTileView>
{
    private readonly DocumentationViewModel parent;
    private readonly Document document;

    [ObservableProperty]
    public partial string Title{ get; set; }

    [ObservableProperty]
    public partial string Summary { get; set; }

    [ObservableProperty]
    public partial Bitmap? ImageSource { get; set; } = null;

    public DocumentTileViewModel(DocumentationViewModel parent, Document document)
    {
        this.parent = parent;
        this.document = document;

        this.Title = this.document.Title;
        this.Summary = this.document.Summary; 

        if (this.document.FirstPage is not null)
        {
            this.ImageSource = this.document.FirstPage;
        }
    }

    [RelayCommand]
    public void OnOpen() => this.parent.OnOpen(this.document); 
}
