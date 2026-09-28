namespace Lyt.PhotoPostPro.Workflow.Documentation;

public sealed partial class DocumentationViewModel : ViewModel<DocumentationView>
{
    private readonly PhotoPostProModel model;
    private readonly IToaster toaster;

    [ObservableProperty]
    public partial bool DocumentIsOpened { get; set; }

    [ObservableProperty]
    public partial DocumentViewModel DocumentViewModel { get; set; }

    public DocumentationViewModel(PhotoPostProModel model, IToaster toaster)
    {
        this.model = model;
        this.toaster = toaster;
        this.DocumentViewModel = new(); 
    }

    public override async void Activate(object? activationParameters)
    {
        base.Activate(activationParameters);

        foreach ( var document in Documents.List)
        {
            PdfLoader.LoadFirstPage(document);
        }

        // Hide navigation toolbar
        new PdfLoadedStatusMessage(Complete: false).Publish();

        this.View.OpenButtonImage.Source = Documents.List[0].FirstPage; 
    }

    public override void Deactivate()
    {
        base.Deactivate();
        this.DocumentViewModel.Close();
        this.DocumentIsOpened = false;
    }


    [RelayCommand]
    public void OnOpen()
    {
        Document document = Documents.List[0]; 
        this.DocumentViewModel.Open(document);
        this.DocumentIsOpened = true;
    }
}
