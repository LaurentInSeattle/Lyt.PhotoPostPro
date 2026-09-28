namespace Lyt.PhotoPostPro.Workflow.Documentation;

public sealed partial class DocumentationViewModel : 
    ViewModel<DocumentationView>, IRecipient<DocPageNavigateMessage>
{
    private readonly PhotoPostProModel model;
    private readonly IToaster toaster;

    [ObservableProperty]
    public partial bool DocumentIsOpened { get; set; }

    [ObservableProperty]
    public partial DocumentViewModel DocumentViewModel { get; set; }

    [ObservableProperty]
    public partial ObservableCollection<DocumentTileViewModel> Tiles { get; set; } = [];

    public DocumentationViewModel(PhotoPostProModel model, IToaster toaster)
    {
        this.model = model;
        this.toaster = toaster;
        this.DocumentViewModel = new();
    }

    public override async void Activate(object? activationParameters)
    {
        base.Activate(activationParameters);

        this.Subscribe<DocPageNavigateMessage>(); 
        foreach (var document in Documents.List)
        {
            if (document.FirstPage is null)
            {
                PdfLoader.LoadFirstPage(document);
            }

            if (document.FirstPage is not null)
            {
                var tile = new DocumentTileViewModel(this, document);
                this.Tiles.Add(tile);
            }
        }

        // Hide navigation toolbar
        new PdfLoadedStatusMessage(Complete: false).Publish();
    }

    public override void Deactivate()
    {
        base.Deactivate();

        this.Unregister<DocPageNavigateMessage>();
        this.Tiles.Clear();
        this.Close();
    }

    [RelayCommand]
    public void OnOpen(Document document)
    {
        this.DocumentViewModel.Open(document);
        this.DocumentIsOpened = true;
    }

    public void Receive(DocPageNavigateMessage message)
    {
        if ( message.Navigate == DocPageNavigateMessage.NavigateTo.Close)
        {
            this.Close();
        }
    }

    private void Close()
    {
        this.DocumentViewModel.Close();
        this.DocumentIsOpened = false;
    }
}
