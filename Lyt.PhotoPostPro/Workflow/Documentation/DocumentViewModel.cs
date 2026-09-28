namespace Lyt.PhotoPostPro.Workflow.Documentation;

using global::Avalonia.Controls.Presenters;

public sealed partial class DocumentViewModel :
    ViewModel<DocumentView>,
    IRecipient<PdfPageLoadedMessage>,
    IRecipient<PdfLoadedStatusMessage>,
    IRecipient<PdfPageInViewMessage>,
    IRecipient<DocPageNavigateMessage>,
    IRecipient<HotKeyMessage>
{
    public sealed record class PageThumbnail(int PageNumber, Bitmap Bitmap);

    private int currentPageIndex;

    // The collection of pages in the film strip 
    [ObservableProperty]
    public partial ObservableCollection<PageThumbnail> PageThumbnails { get; set; } = [];

    // The collection of full pages in the main area
    [ObservableProperty]
    public partial ObservableCollection<DocPageViewModel> Pages { get; set; } = [];

    [ObservableProperty]
    // SelectedIndex in the film strip 
    public partial int SelectedThumbnailIndex { get; set; }

    public DocumentViewModel() => this.currentPageIndex = -1;

    internal void Open(Document document)
    {
        this.Subscribe<PdfPageLoadedMessage>();
        this.Subscribe<PdfLoadedStatusMessage>();
        this.Subscribe<PdfPageInViewMessage>();
        this.Subscribe<DocPageNavigateMessage>();
        this.Subscribe<HotKeyMessage>();

        PdfLoader.BeginLoadDocument(document);
        this.currentPageIndex = -1;
    }

    internal void Close()
    {
        PdfLoader.Unload();

        this.Pages.Clear();
        this.PageThumbnails.Clear();

        this.Unregister<PdfPageLoadedMessage>();
        this.Unregister<PdfLoadedStatusMessage>();
        this.Unregister<PdfPageInViewMessage>();
        this.Unregister<DocPageNavigateMessage>();
        this.Unregister<HotKeyMessage>();
    }

    public void Receive(HotKeyMessage message)
    {
        if (message.Key == Key.PageDown)
        {
            if (this.SelectedThumbnailIndex < this.Pages.Count - 1)
            {
                ++this.SelectedThumbnailIndex;
            }
        }
        else if (message.Key == Key.PageUp)
        {
            if (this.SelectedThumbnailIndex > 0)
            {
                --this.SelectedThumbnailIndex;
            }
        }
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
        new PdfPageInViewMessage(1 + this.currentPageIndex, this.Pages.Count).Publish();
    }

    public void ReceiveOnUiThread(PdfLoadedStatusMessage message)
    {
        if (message.Complete)
        {
            new PdfPageInViewMessage(1 + this.currentPageIndex, this.Pages.Count).Publish();
        }

    }

    public void Receive(PdfPageInViewMessage message)
    {
        Debug.WriteLine(" Page in view: " + message.PageNumber.ToString());
        this.currentPageIndex = message.PageNumber - 1;
    }

    partial void OnSelectedThumbnailIndexChanged(int value)
    {
        this.currentPageIndex = this.SelectedThumbnailIndex;
        this.NavigateTo(this.SelectedThumbnailIndex);
    }

    public void Receive(DocPageNavigateMessage message)
    {
        int pageCount = this.Pages.Count;
        int newPageIndex = -1;
        switch (message.Navigate)
        {
            case DocPageNavigateMessage.NavigateTo.First:
                newPageIndex = 0;
                break;

            case DocPageNavigateMessage.NavigateTo.Previous:
                newPageIndex = this.currentPageIndex - 1;
                if (newPageIndex < 0)
                {
                    newPageIndex = 0;
                }

                break;

            case DocPageNavigateMessage.NavigateTo.Next:
                newPageIndex = this.currentPageIndex + 1;
                if (newPageIndex >= pageCount)
                {
                    newPageIndex = pageCount - 1;
                }

                break;

            case DocPageNavigateMessage.NavigateTo.Last:
                newPageIndex = pageCount - 1;
                break;

            default:
            case DocPageNavigateMessage.NavigateTo.PageNumber:
                break;
        }

        if (newPageIndex >= 0 && newPageIndex < pageCount)
        {
            this.NavigateTo(newPageIndex);
            this.currentPageIndex = newPageIndex;
        }
    }

    private void NavigateTo(int pageIndex)
    {
        // First we scroll into view so that the page gets realized 
        this.View.PagesItemControl.ScrollIntoView(pageIndex);
        Schedule.OnUiThread(30, () =>
        {
            // Then we bring into view the page once it gets realized 
            var container = this.View.PagesItemControl.ContainerFromIndex(pageIndex);
            if (container is ContentPresenter contentPresenter)
            {
                // Now realized 
                object? content = contentPresenter.Content;
                if (content is DocPageViewModel pageVm && pageVm.IsBound)
                {
                    // Bring only the top pixels of the page into view                        
                    var view = pageVm.View;
                    var topRegion = new Rect(0, 0, view.Bounds.Width, 2);
                    view?.BringIntoView(topRegion);
                }
            }
        }, DispatcherPriority.Background);
    }
}
