namespace Lyt.PhotoPostPro.Workflow.Book.Edit;

using Lyt.PhotoPostPro.Workflow.Book.Messages;

using Books = Model.PhotoBook;
using Pages = Model.PhotoBook.Pages;
using Slots = Model.PhotoBook.Slots;

public partial class BookEditViewModel :
    ViewModel<BookEditView>,
    IRecipient<BookPagesChangedMessage>,
    IRecipient<BookPageEditedMessage>,
    IRecipient<DocPageNavigateMessage>
{
    public sealed record class PdfThumbnail(int Page, Bitmap Bitmap, int Width, int Height);

    private readonly PhotoPostProModel model;
    private readonly IToaster toaster;

    [ObservableProperty]
    public partial bool DocumentIsOpened { get; set; }

    [ObservableProperty]
    public partial DocumentViewModel DocumentViewModel { get; set; } = new();

    [ObservableProperty]
    public partial bool IsEditing { get; set; } = true;

    [ObservableProperty]
    public partial bool IsSingleImageMode { get; set; } = false;

    [ObservableProperty]
    public partial bool ShowGridLines { get; set; } = true;

    [ObservableProperty]
    public partial bool DoublePageMode { get; set; }

    [ObservableProperty]
    public partial ObservableCollection<BookPageViewModel> SinglePages { get; set; } = [];

    [ObservableProperty]
    public partial int SinglePagesIndex { get; set; }

    [ObservableProperty]
    public partial ObservableCollection<BookDoublePageViewModel> DoublePages { get; set; } = [];

    [ObservableProperty]
    public partial int DoublePagesIndex { get; set; }

    [ObservableProperty]
    // The collection of images in the film strip 
    public partial ObservableCollection<BookImageViewModel> ImageThumbnails { get; set; } = [];

    [ObservableProperty]
    // SelectedIndex in the film strip 
    public partial int SelectedThumbnailIndex { get; set; }

    [ObservableProperty]
    // The collection of PDF images at the top
    public partial ObservableCollection<PdfThumbnail> PdfThumbnails { get; set; } = [];

    [ObservableProperty]
    // SelectedIndex of the selected PDF Image
    public partial int SelectedPdfThumbnailIndex { get; set; }

    public BookEditViewModel(PhotoPostProModel model, IToaster toaster)
    {
        this.model = model;
        this.toaster = toaster;
    }

    public override void Activate(object? activationParameters)
    {
        base.Activate(activationParameters);

        this.LoadBook();
        this.LoadImageCandidates();

        // We are potentially about to launch heavy stuff, so clean up while we still can
        // We have about at least one second for Drag and drop to happen 
        this.Dispatcher.OnIdle(() => GC.Collect());

        // Subscribe to book updates 
        this.Subscribe<BookPagesChangedMessage>();
        this.Subscribe<BookPageEditedMessage>();
        this.Subscribe<DocPageNavigateMessage>();
    }

    public override void Deactivate()
    {
        base.Deactivate();

        // Cleanup 
        this.DoublePages.Clear();
        this.SinglePages.Clear();
        this.PdfThumbnails.Clear();
        // this.model.BookManager.Clear();

        // UNsubscribe to book updates 
        this.Unregister<BookPagesChangedMessage>();
        this.Unregister<BookPageEditedMessage>();
    }

    public override void OnViewLoaded()
    {
        base.OnViewLoaded();

        Schedule.OnUiThread(
            90, () =>
            {
                var canvas = this.View.BookEditDragCanvas;
                var listBox = this.View.StripListBox;
                foreach (var imageThumbnail in this.ImageThumbnails)
                {
                    imageThumbnail.AttachDraggable(listBox, canvas);
                }
            }, DispatcherPriority.ApplicationIdle);
    }

    partial void OnShowGridLinesChanged(bool value)
        => new BookGridLinesMessage(value).Publish();

    partial void OnIsSingleImageModeChanged(bool value) => this.LoadPages();

    partial void OnSelectedPdfThumbnailIndexChanged(int value)
    {
        if (this.IsSingleImageMode)
        {
            this.SinglePagesIndex = value;
        }
        else
        {
            this.DoublePagesIndex = (1 + value) / 2;
        }
    }

    private void LoadBook()
    {
        this.IsSingleImageMode = false;

        this.LoadPages();
        this.LoadPdfThumbnails();

        // Enforce property changed 
        this.SelectedPdfThumbnailIndex = -1;
        this.SelectedPdfThumbnailIndex = 0;
    }

    private void LoadPages()
    {
        if (this.IsSingleImageMode)
        {
            // was in dual mode 
            int index = this.DoublePagesIndex;
            this.LoadSinglePages();
            this.SinglePagesIndex = 2 * index - 1;
        }
        else
        {
            // was in single mode 
            int index = this.SinglePagesIndex;
            this.LoadDoublePages();
            this.DoublePagesIndex = (1 + index) / 2;
        }

        new BookGridLinesMessage(this.ShowGridLines).Publish();
    }

    private void LoadSinglePages()
    {
        var book = this.model.BookManager.CurrentBook;
        if (book is null)
        {
            return;
        }

        this.DoublePageMode = false;
        this.DoublePages.Clear();
        this.SinglePages.Clear();
        if (book.WithFrontCover)
        {
            // ! We have a front cover 
            var frontCoverFront = new BookPageViewModel(this.model, book.FrontCover!.FrontPage);
            this.SinglePages.Add(frontCoverFront);

            // ! We have a front cover 
            var frontCoverBack = new BookPageViewModel(this.model, book.FrontCover!.BackPage);
            this.SinglePages.Add(frontCoverBack);
        }

        for (int pageIndex = 0; pageIndex < book.Pages.Count; ++pageIndex)
        {
            var page = new BookPageViewModel(this.model, book.Pages[pageIndex]);
            this.SinglePages.Add(page);
        }

        if (book.WithBackCover)
        {
            // ! We have a back cover 
            var backCoverFront = new BookPageViewModel(this.model, book.BackCover!.FrontPage);
            this.SinglePages.Add(backCoverFront);

            // ! We have a back cover 
            var backCoverBack = new BookPageViewModel(this.model, book.BackCover!.BackPage);
            this.SinglePages.Add(backCoverBack);
        }

        this.SinglePagesIndex = -1;
        this.SinglePagesIndex = 0;
    }

    private void LoadDoublePages()
    {
        var book = this.model.BookManager.CurrentBook;
        if (book is null)
        {
            return;
        }

        this.DoublePageMode = true;
        this.DoublePages.Clear();
        this.SinglePages.Clear();
        if (book.WithFrontCover)
        {
            // ! We have a front cover 
            var frontCoverFront = new BookPageViewModel(this.model, book.FrontCover!.FrontPage);
            var dummy = new BookPageViewModel(this.model, null);
            var firstDoublePage = new BookDoublePageViewModel(this.model, dummy, frontCoverFront);
            this.DoublePages.Add(firstDoublePage);

            // ! We have a front cover 
            var frontCoverBack = new BookPageViewModel(this.model, book.FrontCover!.BackPage);
            var firstPage = new BookPageViewModel(this.model, book.Pages[0]);
            var secondDoublePage = new BookDoublePageViewModel(this.model, frontCoverBack, firstPage);
            this.DoublePages.Add(secondDoublePage);
        }
        else
        {
            var dummy = new BookPageViewModel(this.model, null);
            var firstPage = new BookPageViewModel(this.model, book.Pages[0]);
            var firstDoublePage = new BookDoublePageViewModel(this.model, dummy, firstPage);
            this.DoublePages.Add(firstDoublePage);
        }

        for (int pageIndex = 1; pageIndex < book.Pages.Count - 1; pageIndex += 2)
        {
            var leftPage = new BookPageViewModel(this.model, book.Pages[pageIndex]);
            var rightPage = new BookPageViewModel(this.model, book.Pages[pageIndex + 1]);
            var doublePage = new BookDoublePageViewModel(this.model, leftPage, rightPage);
            this.DoublePages.Add(doublePage);
        }

        if (book.WithBackCover)
        {
            // ! We have a back cover 
            var backCoverFront = new BookPageViewModel(this.model, book.BackCover!.FrontPage);
            var lastPage = new BookPageViewModel(this.model, book.Pages[^1]);
            var beforeLastDoublePage = new BookDoublePageViewModel(this.model, lastPage, backCoverFront);
            this.DoublePages.Add(beforeLastDoublePage);

            // ! We have a back cover 
            var backCoverBack = new BookPageViewModel(this.model, book.BackCover!.BackPage);
            var dummy = new BookPageViewModel(this.model, null);
            var lastDoublePage = new BookDoublePageViewModel(this.model, backCoverBack, dummy);
            this.DoublePages.Add(lastDoublePage);
        }
        else
        {
            var lastPage = new BookPageViewModel(this.model, book.Pages[^1]);
            var dummy = new BookPageViewModel(this.model, null);
            var lastDoublePage = new BookDoublePageViewModel(this.model, lastPage, dummy);
            this.DoublePages.Add(lastDoublePage);
        }

        this.DoublePagesIndex = -1;
        this.DoublePagesIndex = 0;
    }

    private void LoadImageCandidates()
    {
        // BookManager is caching the thumbnails of image candidates 
        var images = this.model.BookManager.CurrentBookThumbnails;
        var list = new List<BookImageViewModel>(images.Count);
        Parallel.ForEach(images.Keys, imageId =>
        {
            var imageBytes = images[imageId];
            using var stream = new MemoryStream(imageBytes);
            var bitmap = new Bitmap(stream);
            lock (list)
            {
                list.Add(new BookImageViewModel(imageId, bitmap));
            }
        });

        this.ImageThumbnails = [with(list)];
    }

    private void LoadPdfThumbnails()
    {
        // ! We have a book
        Books.Book book = this.model.BookManager.CurrentBook!;
        float bookWidth = book.PageWidth;
        float bookHeight = book.PageHeight;
        const int desiredHeight = 112;
        float ratio = bookHeight / desiredHeight;
        int width = (int)MathF.Round(bookWidth / ratio);
        using MemoryStream stream = new(4 * 1024 * 1024);
        book.GeneratePdf(BookResolution.Thumbnails, stream);
        stream.Seek(0, SeekOrigin.Begin);
        List<Bitmap> pdfBitmaps = PdfLoader.LoadThumnailImages(stream, width, desiredHeight);
        List<PdfThumbnail> pdfThumbnails = new(pdfBitmaps.Count);
        for (int page = 0; page < pdfBitmaps.Count; ++page)
        {
            pdfThumbnails.Add(new PdfThumbnail(page, pdfBitmaps[page], width, desiredHeight));
        }

        this.PdfThumbnails = new(pdfThumbnails);
    }

    [RelayCommand]
    public void OnAddPageBefore()
    {
        var bookMgr = this.model.BookManager;
        bookMgr.InsertPage(this.SelectedPdfThumbnailIndex, out string message);
    }

    [RelayCommand]
    public void OnAddPageAfter()
    {
        var bookMgr = this.model.BookManager;
        bookMgr.InsertPage(1 + this.SelectedPdfThumbnailIndex, out string message);
    }

    [RelayCommand]
    public void OnDeletePage()
    {
        var bookMgr = this.model.BookManager;
        bookMgr.RemovePage(this.SelectedPdfThumbnailIndex, out string message);
    }

    [RelayCommand]
    public void OnDeleteSlot()
    {
        var slot = SlotViewModel.SelectedSlot; 
        if ( slot is null)
        {
            // nothing to process
            return; 
        }

        var bookMgr = this.model.BookManager;
        bookMgr.DeleteSlot(slot.Slot, slot.Page, out string message);
    }

    [RelayCommand]
    public void OnEditSlot()
    {
        var slot = SlotViewModel.SelectedSlot;
        if (slot is null)
        {
            // nothing to process
            return;
        }

        slot.BeginEdit(); 
    }

    [RelayCommand]
    public void OnAddSlot()
    {
        var bookMgr = this.model.BookManager;
        bookMgr.AddImageSlot(this.SelectedPdfThumbnailIndex);
    }

    [RelayCommand]
    public void OnPreview()
    {
        var bookMgr = this.model.BookManager;
        // ! we have a book 
        string documentPath = bookMgr.CurrentBook!.GeneratePdf(BookResolution.Display);
        this.IsEditing = false; 
        this.DocumentViewModel.Open(documentPath); 
    }

    public void Receive(DocPageNavigateMessage message)
    {
        if (message.Navigate == DocPageNavigateMessage.NavigateTo.Close)
        {
            this.DocumentViewModel.Close();
            this.IsEditing = true;
        }
    }

    public void Receive(BookPagesChangedMessage message)
    {
        this.LoadBook();

        // Enforce property changed 
        this.SelectedPdfThumbnailIndex = -1;
        int pageToShow = message.PageToShow;
        if (pageToShow < 0)
        {
            pageToShow = 0;
        }
        else if (pageToShow >= this.PdfThumbnails.Count)
        {
            pageToShow = this.PdfThumbnails.Count - 1;
        }

        this.SelectedPdfThumbnailIndex = pageToShow;
    }

    public void Receive(BookPageEditedMessage message)
        // We need to wait for the page itself to re-create its slots, then we can update the PDF 
        => Schedule.OnUiThread(60,()=>
            { 
                // Relaod the thumbnails and then adjust position 
                this.LoadPdfThumbnails() ;

                // ! we have a book 
                var book = this.model.BookManager.CurrentBook!;
                int pdfPageIndex = book.PageIndexFromPage(message.Page);
                this.SelectedPdfThumbnailIndex = pdfPageIndex;                 
            }, DispatcherPriority.Background); 
}
