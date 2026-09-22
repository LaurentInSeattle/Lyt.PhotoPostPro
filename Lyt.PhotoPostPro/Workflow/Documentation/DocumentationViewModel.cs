namespace Lyt.PhotoPostPro.Workflow.Documentation;

public sealed partial class DocumentationViewModel : 
    ViewModel<DocumentationView>,
    IRecipient<PdfPageLoadedMessage>,
    IRecipient<PdfLoadedStatusMessage>,
    IRecipient<PdfPageInViewMessage>
{
    public sealed record class PageThumbnail(int PageNumber,  Bitmap Bitmap);

    private readonly PhotoPostProModel model;
    private readonly IToaster toaster;

    // The collection of pages in the film strip 
    [ObservableProperty]
    public partial ObservableCollection<PageThumbnail> PageThumbnails { get; set; } = [];

    // The collection of full pages in the main area
    [ObservableProperty]
    public partial ObservableCollection<DocPageViewModel> Pages { get; set; } = [];

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
        this.Subscribe<PdfPageInViewMessage>();

        PdfLoader.BeginLoadDocumentation();
    }

    public override void Deactivate()
    {
        PdfLoader.Unload();

        this.Pages.Clear();
        this.PageThumbnails.Clear();

        this.Unregister<PdfPageLoadedMessage>();
        this.Unregister<PdfLoadedStatusMessage>();
        this.Unregister<PdfPageInViewMessage>();

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
        this.Pages.Add(new DocPageViewModel(page.PageNumber, page.Page)); 
    }

    public void ReceiveOnUiThread(PdfLoadedStatusMessage message)
    {

    }

    public void Receive(PdfPageInViewMessage message)
    {
        Debug.WriteLine(" Page in view: " + message.PageNumber.ToString()); 
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
