namespace Lyt.PhotoPostPro.Workflow.Book.Edit;

using Lyt.PhotoPostPro.Workflow.Book.Messages;

using Books = Model.PhotoBook;
using Pages = Model.PhotoBook.Pages;
using Slots = Model.PhotoBook.Slots;

public partial class BookEditViewModel :
    ViewModel<BookEditView>,
    IRecipient<BookPagesChangedMessage>,
    IRecipient<BookPageEditedMessage>,
    IRecipient<DocPageNavigateMessage>,
    IRecipient<BookEditTextSlotMessage>
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
    public partial bool PdfQualityIsDisplay { get; set; } = true;

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
    public partial bool ShowAllImages { get; set; } = true;

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

    [ObservableProperty]
    public partial bool IsSplitViewLeftOpen { get; set; }

    [ObservableProperty]
    public partial bool IsSplitViewRightOpen { get; set; }

    [ObservableProperty]
    public partial StylingPanelViewModel StylingPanelViewModel { get; set; }

    public BookEditViewModel(PhotoPostProModel model, IToaster toaster)
    {
        this.model = model;
        this.toaster = toaster;
        this.IsSplitViewLeftOpen = true;
        this.IsSplitViewRightOpen = true;
        this.StylingPanelViewModel = new(this.model);
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
        this.Subscribe<BookEditTextSlotMessage>();
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
        this.Unregister<DocPageNavigateMessage>();
        this.Unregister<BookEditTextSlotMessage>();
    }

    public override void OnViewLoaded()
    {
        base.OnViewLoaded();
        this.AttachDragableToImages();
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

    partial void OnShowAllImagesChanged(bool value)
    {
        this.LoadImageCandidates();
        this.AttachDragableToImages();
    }

    private void LoadImageCandidates()
    {
        // BookManager is caching the thumbnails of image candidates 
        var bookManager = this.model.BookManager;
        var images =
            this.ShowAllImages ?
                bookManager.CurrentBookThumbnails :
                bookManager.CurrentBookUnusedThumbnails();
        var list = new List<BookImageViewModel>(images.Count);
        Parallel.ForEach(images.Keys, imageId =>
        {
            byte[] imageBytes = images[imageId];
            using var stream = new MemoryStream(imageBytes);
            var bitmap = new Bitmap(stream);
            lock (list)
            {
                list.Add(new BookImageViewModel(imageId, bitmap));
            }
        });

        this.ImageThumbnails = [with(list)];
    }

    private void AttachDragableToImages()
    {
        Schedule.OnUiThread(90, () =>
        {
            var canvas = this.View.BookEditDragCanvas;
            var listBox = this.View.StripListBox;
            foreach (var imageThumbnail in this.ImageThumbnails)
            {
                imageThumbnail.AttachDraggable(listBox, canvas);
            }
        }, DispatcherPriority.ApplicationIdle);
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
    public void OnToggleOpenSplitViewLeft() => this.IsSplitViewLeftOpen = !this.IsSplitViewLeftOpen;

    [RelayCommand]
    public void OnToggleOpenSplitViewRight() => this.IsSplitViewRightOpen = !this.IsSplitViewRightOpen;

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
        if (slot is null)
        {
            // nothing to process
            return;
        }

        var bookMgr = this.model.BookManager;
        bookMgr.DeleteSlot(slot.Slot, slot.Page, out string message);
        SlotViewModel.ClearSelection();
    }

    public void Receive(BookEditTextSlotMessage message)
        => Schedule.OnUiThread(
            60,
            () => { this.BegitEditCreatedTextSlot(message); },
            DispatcherPriority.ApplicationIdle);

    private void BegitEditCreatedTextSlot(BookEditTextSlotMessage message)
    {
        var page = message.Page;
        var slot = message.Slot;

        bool FindSlot(BookPageViewModel pageViewModel)
        {
            if (pageViewModel.Page != page)
            {
                return false;
            }

            foreach (var slotVm in pageViewModel.Slots)
            {
                if (slotVm.Slot == slot)
                {
                    // Found : Begin edit 
                    slotVm.BeginEdit();
                    return true;
                }
            }

            return false;
        }

        if (this.IsSingleImageMode)
        {
            foreach (var singlePage in this.SinglePages)
            {
                if (FindSlot(singlePage))
                {
                    return;
                }
            }
        }
        else
        {
            foreach (var doublePage in this.DoublePages)
            {
                var left = doublePage.LeftPage;
                if (FindSlot(left))
                {
                    return;
                }

                var right = doublePage.RightPage;
                if (FindSlot(right))
                {
                    return;
                }
            }
        }
    }

    [RelayCommand]
    public void OnEditSlot()
    {
        var slotViewModel = SlotViewModel.SelectedSlot;
        if (slotViewModel is null)
        {
            // create a text slot and begin editing it 
            var bookMgr = this.model.BookManager;
            bookMgr.AddTextSlot(this.SelectedPdfThumbnailIndex);
            return;
        }

        if (slotViewModel.Slot is Slots.ImageSlot)
        {
            return;
        }

        slotViewModel.BeginEdit();
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
        // Clear selection so that the Adorner layer will not pollute the PDF display 
        SlotViewModel.ClearSelection();

        // Generate PDF document and launch the viewer 
        var bookMgr = this.model.BookManager;
        var bookResolution =
            this.PdfQualityIsDisplay ? BookResolution.Display : BookResolution.Print;
        // ! we have a book 
        string? documentPath = bookMgr.CurrentBook!.GeneratePdf(bookResolution);
        if (string.IsNullOrWhiteSpace(documentPath))
        {
            // failed :( 
            return;
        }

        if (!File.Exists(documentPath))
        {
            // failed :( 
            return;
        }

        this.IsEditing = false;
        this.DocumentViewModel.Open(documentPath);
    }

    [RelayCommand]
    public void OnNavigateToPdf()
    {
        // ! we have a book 
        var book = this.model.BookManager.CurrentBook!;
        string pdfPath = book.OutputPdfPathName;
        if ( string.IsNullOrWhiteSpace(pdfPath) || !File.Exists(pdfPath))
        {
            // No PDF book 
            return;
        }

        string? folderPath = System.IO.Path.GetDirectoryName(pdfPath);
        if (string.IsNullOrWhiteSpace(folderPath) || !Directory.Exists(folderPath))
        {
            // No book 
            return; 
        }

        PhotoPostProModel.NavigateTo(folderPath);
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
        => Schedule.OnUiThread(60, () =>
            {
                // Relaod the thumbnails and then adjust position 
                this.LoadPdfThumbnails();

                // ! we have a book 
                var book = this.model.BookManager.CurrentBook!;
                int pdfPageIndex = book.PageIndexFromPage(message.Page);
                this.SelectedPdfThumbnailIndex = pdfPageIndex;
            }, DispatcherPriority.Background);
}
