namespace Lyt.PhotoPostPro.Workflow.Documentation;

public sealed partial class DocumentationViewModel : 
    ViewModel<DocumentationView>,
    IRecipient<PdfPageLoadedMessage>,
    IRecipient<PdfLoadedStatusMessage>
{
    public sealed record class PageThumbnail(int PageNumber,  Bitmap Bitmap);

    private readonly PhotoPostProModel model;
    private readonly IToaster toaster;

    // The collection of pages in the film strip 
    [ObservableProperty]
    public partial ObservableCollection<PageThumbnail> PageThumbnails { get; set; } = [];

    // The collection of full pages in the main area
    [ObservableProperty]
    public partial ObservableCollection<Bitmap> Pages { get; set; } = [];

    [ObservableProperty]
    // SelectedIndex in the film strip 
    public partial int SelectedThumbnailIndex { get; set; }

    public DocumentationViewModel(PhotoPostProModel model, IToaster toaster)
    {
        this.model = model;
        this.toaster = toaster;
    }

    public override async void Activate(object? activationParameters)
    {
        base.Activate(activationParameters);

        this.Subscribe<PdfPageLoadedMessage>();
        this.Subscribe<PdfLoadedStatusMessage>();

        PdfLoader.BeginLoadDocumentation();
    }

    public override void Deactivate()
    {
        PdfLoader.Unload();

        this.Pages.Clear();
        this.PageThumbnails.Clear();

        this.Unregister<PdfPageLoadedMessage>();
        this.Unregister<PdfLoadedStatusMessage>();

        base.Deactivate(); 
    }

    public void Receive(PdfPageLoadedMessage message)
        => Dispatch.OnUiThread(() => { this.ReceiveOnUiThread(message); }, DispatcherPriority.Background); 
    
    public void Receive(PdfLoadedStatusMessage message) 
        => Dispatch.OnUiThread(() => { this.ReceiveOnUiThread(message); }, DispatcherPriority.Background); 

    public void ReceiveOnUiThread(PdfPageLoadedMessage message)
    {
        var page = message.PdfPage;
        var pageThumbnail = new PageThumbnail(page.PageNumber, page.Thumbnail);
        this.PageThumbnails.Add(pageThumbnail);
        this.Pages.Add(page.Page); 
    }

    public void ReceiveOnUiThread(PdfLoadedStatusMessage message)
    {

    }

    partial void OnSelectedThumbnailIndexChanged(int value)
    {
        if (!this.IsActivated )
        {
            return;
        }

        this.View.PagesItemControl.ScrollIntoView(this.SelectedThumbnailIndex) ;
    }
}
