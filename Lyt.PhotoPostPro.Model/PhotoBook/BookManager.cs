namespace Lyt.PhotoPostPro.Model.PhotoBook;

using QuestPDF.Infrastructure;

public sealed partial class BookManager
{
    static BookManager()
    {
        QuestPDF.Settings.License = LicenseType.Community;
        QuestPDF.Settings.EnableCaching = true;
        QuestPDF.Settings.UseSystemFonts = true; 
    }

    private readonly FileManagerModel fileManager; 

    public BookManager(FileManagerModel fileManager)
    {
        this.fileManager = fileManager; 
    }

    public List<Book> Books { get; } = [];

    public bool IsEditing { get; set; }

    public Book? CurrentBook { get; set; }

    public Book CreateBook(
        string uniqueName,
        string folderName , 
        string description, 
        string outputPdfFileName,
        BookPage bookPage, 
        BookFormat format,
        PageBackground defaultPageBackground)
    {
        var book = new Book
        {
            UniqueName = uniqueName,
            FolderName = folderName, 
            Description = description,
            OutputPdfFileName = outputPdfFileName,
            BookPage = bookPage,
            Format = format,
            DefaultPageBackground = defaultPageBackground,
        };

        this.Books.Add(book);
        return book;
    }

    public void DeleteBook(Book book)
    {
        this.Books.Remove(book);
    }

    public Book? GetBook(string uniqueName) => this.Books.FirstOrDefault(b => b.UniqueName == uniqueName);

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
