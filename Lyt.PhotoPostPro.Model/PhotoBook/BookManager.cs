namespace Lyt.PhotoPostPro.Model.PhotoBook;

using QuestPDF.Infrastructure;

public sealed class BookManager
{
    static BookManager()
    {
        QuestPDF.Settings.License = LicenseType.Community;
        QuestPDF.Settings.EnableCaching = true;
        QuestPDF.Settings.UseSystemFonts = true; 
    }

    public BookManager()
    {
    }

    public List<Book> Books { get; } = [];

    public bool IsEditing { get; set; }

    public Book? CurrentBook { get; set; }

    public static void Test()
    {
        const string VeneziaImagePath = "C:\\Users\\Laurent\\Desktop\\TestImages\\Saved Pictures\\Venezia.jpg";
        const string ToscanaImagePath = "C:\\Users\\Laurent\\Desktop\\TestImages\\Saved Pictures\\Toscana.jpg";
        const string CapriImagePath = "C:\\Users\\Laurent\\Desktop\\TestImages\\Saved Pictures\\Capri3.jpg";
        const string VeronaImagePath = "C:\\Users\\Laurent\\Desktop\\TestImages\\Saved Pictures\\Verona.jpg";

        var book = new Book
        {
            UniqueName = "TestBook",
            OutputPdfFileName = "C:\\Users\\Laurent\\Desktop\\PhotoRebel\\TestBook.pdf",
            BookPage = BookPage.UsLetter,
            Format = BookFormat.Landscape,
            WithFrontCover = true,
            WithBackCover = false,
        };

        book.Initialize();

        // ! ok
        var front = book.FrontCover!.FrontPage;
        new FullPageImageLayout().Create(front, VeneziaImagePath);
        // ! ok
        var back = book.FrontCover!.BackPage;
        new EmptyLayout().Create(back);

        var page1 = book.Pages.FirstOrDefault(p => p.PageNumber == 1);
        // ! ok
        page1!.Slots.Add(
            new ImageSlot()
            {
                Position = new SlotPosition(x: 100, y: 100, dx: 400, dy: 300),
                ImageFileName = ToscanaImagePath,
            });

        var page2 = book.Pages.FirstOrDefault(p => p.PageNumber == 2);
        // ! ok
        page2!.Slots.Add(
            new ImageSlot()
            {
                Position = new SlotPosition(x: 100, y: 200, dx: 400, dy: 300),
                ImageFileName = CapriImagePath,
            });

        book.GeneratePdf();
    }

    public Book CreateBook(string uniqueName, string outputPdfFileName, BookFormat format)
    {
        var book = new Book
        {
            UniqueName = uniqueName,
            OutputPdfFileName = outputPdfFileName,
            Format = format,
        };

        this.Books.Add(book);
        return book;
    }

    public void DeleteBook(Book book)
    {
        this.Books.Remove(book);
    }

    public Book? GetBook(string uniqueName)
    {
        return this.Books.FirstOrDefault(b => b.UniqueName == uniqueName);
    }

    public void BeginEditBook(Book book)
    {
        this.IsEditing = true;
        this.CurrentBook = book;
    }

    public void EndEditBook()
    {
        this.IsEditing = false;
        this.CurrentBook = null;
    }

    public void SaveBook()
    {
    }

    #region Page Management

    // Insert a single page at the provided page number in the current book, shifting existing pages
    // and leaves forward.
    // Leaves are always two pages, so if the page number is odd, the new page will be inserted as the
    // front page of a new leaf, and the existing page will become the back page of that leaf.
    // If the page number is even, the new page will be inserted as the back page of a new leaf,
    // and the existing page will become the front page of that leaf.
    public void InsertPage(int pageNumber)
    {
        if (this.CurrentBook == null)
        {
            throw new InvalidOperationException("No current book to add a leaf to.");
        }

        //var newLeaf = new Leaf(new Page(this.Current, pageNumber), new Page(this.Current, pageNumber + 1));
        //this.Current.Leaves.Add(newLeaf);
    }

    public void RemovePage(int pageNumber)
    {
        if (this.CurrentBook == null)
        {
            throw new InvalidOperationException("No current book to remove a leaf from.");
        }
        //// Find the leaf that contains the page number
        //var leafToRemove = this.Current.Leaves.FirstOrDefault(leaf =>
        //    leaf.FrontPage.PageNumber == pageNumber || leaf.BackPage.PageNumber == pageNumber);
        //if (leafToRemove != null)
        //{
        //    this.Current.Leaves.Remove(leafToRemove);
        //}
    }

    public void MovePage(int fromPageNumber, int toPageNumber)
    {
        if (this.CurrentBook == null)
        {
            throw new InvalidOperationException("No current book to move a leaf in.");
        }
        //// Find the leaf that contains the fromPageNumber
        //var leafToMove = this.Current.Leaves.FirstOrDefault(leaf =>
        //    leaf.FrontPage.PageNumber == fromPageNumber || leaf.BackPage.PageNumber == fromPageNumber);
        //if (leafToMove != null)
        //{
        //    this.Current.Leaves.Remove(leafToMove);
        //    this.Current.Leaves.Insert(toPageNumber / 2, leafToMove);
        //}
    }

    #endregion Page Management

    #region Page Editing

    public void AddSlotToPage(int pageNumber, Slot newSlot)
    {
        if (this.CurrentBook == null)
        {
            throw new InvalidOperationException("No current book to add a slot to.");
        }

        var page = this.CurrentBook.FromPageNumber(pageNumber);
        if (page is null)
        {
            return;
        }

        page.Slots.Add(newSlot);
    }

    public void RemoveSlotFromPage(int pageNumber, Slot slotToRemove)
    {
        if (this.CurrentBook == null)
        {
            throw new InvalidOperationException("No current book to remove a slot from.");
        }

        var page = this.CurrentBook.FromPageNumber(pageNumber);
        if (page is null)
        {
            return;
        }

        page.Slots.Remove(slotToRemove);
    }

    public void MoveSlot(int fromPageNumber, int toPageNumber, Slot slotToRemove)
    {
        if (this.CurrentBook == null)
        {
            throw new InvalidOperationException("No current book to remove a slot from.");
        }

        var fromPage = this.CurrentBook.FromPageNumber(fromPageNumber);
        var toPage = this.CurrentBook.FromPageNumber(toPageNumber);
        if (fromPage is null || toPage is null)
        {
            return;
        }

        fromPage.Slots.Remove(slotToRemove);

    }

    public void RepositionSlot(int pageNumber, Slot slotToRemove, SlotPosition newPosition)
    {
        if (this.CurrentBook == null)
        {
            throw new InvalidOperationException("No current book to remove a slot from.");
        }

        var page = this.CurrentBook.FromPageNumber(pageNumber);
        if (page is null)
        {
            return;
        }

        page.Slots.Remove(slotToRemove);
    }

    public void UpdatePageBackground(int pageNumber, PageBackground newBackground)
    {
        if (this.CurrentBook == null)
        {
            throw new InvalidOperationException("No current book to update a page background in.");
        }

        var page = this.CurrentBook.FromPageNumber(pageNumber);
        if (page is null)
        {
            return;
        }

        page.PageBackground = newBackground;
    }

    public void PopulateImageSlot(int pageNumber, Guid slotId, string imagePath)
    {
        if (this.CurrentBook == null)
        {
            throw new InvalidOperationException("No current book to update a page layout in.");
        }
        var page = this.CurrentBook.FromPageNumber(pageNumber);
        if (page is null)
        {
            return;
        }
    }

    public void PopulateTextSlot(int pageNumber, Guid slotId, string text)
    {
        if (this.CurrentBook == null)
        {
            throw new InvalidOperationException("No current book to update a page layout in.");
        }

        var page = this.CurrentBook.FromPageNumber(pageNumber);
        if (page is null)
        {
            return;
        }
    }

    #endregion Page Editing

    #region Text Styles

    public void CreateTextStyle(SlotTextStyle newStyle)
    {
        if (this.CurrentBook == null)
        {
            throw new InvalidOperationException("No current book to update a text style in.");
        }

        this.CurrentBook.TextStyles.Add(newStyle);
    }

    public void DeleteTextStyle(SlotTextStyle newStyle)
    {
        if (this.CurrentBook == null)
        {
            throw new InvalidOperationException("No current book to update a text style in.");
        }

        this.CurrentBook.TextStyles.Remove(newStyle);
    }

    public void UpdateTextStyle(string styleKey, SlotTextStyle newStyle)
    {
        if (this.CurrentBook == null)
        {
            throw new InvalidOperationException("No current book to update a text style in.");
        }
        var style = this.CurrentBook.TextStyles.FirstOrDefault(s => s.Key == styleKey);
        if (style != null)
        {
            this.CurrentBook.TextStyles[this.CurrentBook.TextStyles.IndexOf(style)] = newStyle;
        }
    }

    #endregion Text Styles
}
