namespace Lyt.PhotoPostPro.Workflow.Book.Edit;

using Books = Model.PhotoBook;
using Pages = Model.PhotoBook.Pages;
using Slots = Model.PhotoBook.Slots;

public partial class BookEditViewModel :
    ViewModel<BookEditView> // ,
                            //IRecipient<>
{
    public sealed record class UiThumbnail(string Key, Bitmap Bitmap);

    private readonly PhotoPostProModel model;
    private readonly IToaster toaster;

    [ObservableProperty]
    public partial ObservableCollection<BookDoublePageViewModel> DoublePages { get; set; } = [];

    [ObservableProperty]
    public partial int DoublePagesIndex { get; set; }

    [ObservableProperty]
    // The collection of images in the film strip 
    public partial ObservableCollection<UiThumbnail> ImageThumbnails { get; set; } = [];

    [ObservableProperty]
    // the Selected Thumbnails in the film strip 
    public partial ObservableCollection<UiThumbnail> SelectedThumbnails { get; set; } = [];

    [ObservableProperty]
    // SelectedIndex in the film strip 
    public partial int SelectedThumbnailIndex { get; set; }

    public BookEditViewModel(PhotoPostProModel model, IToaster toaster)
    {
        this.model = model;
        this.toaster = toaster;
    }

    public override void Activate(object? activationParameters)
    {
        base.Activate(activationParameters);

        this.LoadDoublePages();
        this.LoadThumbnails();

        // We are potentially about to launch heavy stuff, so clean up while we still can
        // We have about at least one second for Drag and drop to happen 
        this.Dispatcher.OnIdle(() => GC.Collect());

        // Subscribe to book updates 
    }

    public override void Deactivate()
    {
        base.Deactivate();

        // Cleanup 
        this.DoublePages.Clear();

        // UNsubscribe to book updates 
    }

    private void LoadDoublePages()
    {
        var book = this.model.BookManager.CurrentBook;
        if (book is null)
        {
            return;
        }

        this.DoublePages.Clear();
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

    private void LoadThumbnails ()
    {
        // ! We have a book
        Books.Book book = this.model.BookManager.CurrentBook!;
        var images = book.ImageCandidates.Values.ToList();

        var list = new List<UiThumbnail>(images.Count); 
        Parallel.For(0, images.Count, index =>
        {
            string file = images[index].LowResPath;
            if (File.Exists(file))
            {
                var bitmap = new Bitmap(file);
                lock (list)
                {
                    list.Add(new UiThumbnail(file, bitmap));
                } 
            }
        });

        this.ImageThumbnails = [with(list)];
    }
}
