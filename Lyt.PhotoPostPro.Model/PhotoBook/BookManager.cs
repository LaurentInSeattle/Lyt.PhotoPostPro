namespace Lyt.PhotoPostPro.Model.PhotoBook;

using QuestPDF.Infrastructure;

using System.IO;

public sealed partial class BookManager
{
    static BookManager()
    {
        QuestPDF.Settings.License = LicenseType.Community;
        QuestPDF.Settings.EnableCaching = true;
        QuestPDF.Settings.UseSystemFonts = true;
    }

    public const string PhotoBooksFolderName = "PhotoBooks";
    public const string PhotoBookJSonFileName = "Photobook.json";
    public const string PhotoBookPdfFileName = "Photobook.pdf";

    private readonly PhotoPostProModel model;
    private readonly FileManagerModel fileManager;
    private readonly ILogger logger;
    private readonly string photobooksFolderPath = string.Empty;

    public BookManager(PhotoPostProModel model, FileManagerModel fileManager, ILogger logger)
    {
        this.model = model;
        this.fileManager = fileManager;
        this.logger = logger;

        // Setup the photobook folder path based on the Pictures folder and the application name.
        string pictures = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);
        this.photobooksFolderPath = Path.Combine(pictures, PhotoPostProModel.PhotoPostProAppName, PhotoBooksFolderName);
        if (!Directory.Exists(this.photobooksFolderPath))
        {
            Directory.CreateDirectory(this.photobooksFolderPath);
        }

        this.LoadBooks();
    }

    public List<Book> Books { get; } = [];

    public bool IsEditing { get; set; }

    public Book? CurrentBook { get; set; }

    public Book? CreateBook(
        string uniqueName,
        string description,
        BookPage bookPage,
        BookFormat format,
        PageBackground defaultPageBackground,
        bool withFrontCover,
        bool withBackCover,
        out string message)
    {
        message = string.Empty;

        var book = this.GetBook(uniqueName);
        if (book is not null)
        {
            message = "Book.Create.AlreadyExist";
            return null;
        }

        const string FallbackFolderName = "_F_a_l_l_b_a_c_k_";
        string folderName = FileSystemExtensions.SanitizeFolderName(uniqueName, FallbackFolderName);
        if (folderName == FallbackFolderName)
        {
            message = "Book.Create.UniqueName";
            return null;
        }

        book = new Book
        {
            MaybeLogger = this.logger,

            UniqueName = uniqueName,
            FolderName = folderName,
            Description = description,
            // Will be finalized on first save 
            OutputPdfFileName = string.Empty,
            BookPage = bookPage,
            Format = format,
            DefaultPageBackground = defaultPageBackground,
            WithFrontCover = withFrontCover,
            WithBackCover = withBackCover,
        };

        book.Initialize();

        if (this.SaveBook(book))
        {
            this.Books.Add(book);
            return book;
        }
        else
        {
            message = "Book.Create.CantSave";
            return null;
        }
    }

    public bool DeleteBook(Book book, out string message)
    {
        message = string.Empty;
        try
        {
            var stillBook = this.GetBook(book.UniqueName);
            if (stillBook is null)
            {
                message = "Book.Delete.NotExist";
                return false;
            }

            string photobookFolderPath = Path.Combine(this.photobooksFolderPath, book.FolderName);
            if (Directory.Exists(photobookFolderPath))
            {
                // Consider moving stuff to Recycle bin
                Directory.Delete(photobookFolderPath, recursive: true);
            }

            this.Books.Remove(book);
            return true;
        }
        catch (Exception ex)
        {
            string msg = "Exception thrown when saving book. \n" + ex.ToString();
            Debug.WriteLine(msg);
            this.logger.Error(msg);
            return false;
        }
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

    public bool SaveCurrentBook()
    {
        if (this.CurrentBook is null)
        {
            return false;
        }

        return this.SaveBook(this.CurrentBook);
    }

    public bool SaveBook(Book book)
    {
        try
        {
            string photobookFolderPath = Path.Combine(this.photobooksFolderPath, book.FolderName);
            if (!Directory.Exists(photobookFolderPath))
            {
                Directory.CreateDirectory(photobookFolderPath);
            }

            string photoBookPdfPathName = Path.Combine(photobookFolderPath, PhotoBookPdfFileName);
            book.OutputPdfFileName = photoBookPdfPathName;

            string photobookPath = Path.Combine(photobookFolderPath, PhotoBookJSonFileName);
            var jsonTypeInfo = AppJsonContext.Default.Book;
            string serialized = fileManager.Serialize(book, jsonTypeInfo);
            File.WriteAllText(photobookPath, serialized);
            return true;
        }
        catch (Exception ex)
        {
            string msg = "Exception thrown when saving book. \n" + ex.ToString();
            Debug.WriteLine(msg);
            this.logger.Error(msg);
            return false;
        }
    }

    public bool LoadBooks()
    {
        try
        {
            List<string> directories = FileSystemExtensions.EnumerateDirectories(this.photobooksFolderPath);
            if (directories.Count == 0)
            {
                Debug.WriteLine("No books yet.");
                return true;
            }

            foreach (string directory in directories)
            {
                string photobookPath = Path.Combine(directory, PhotoBookJSonFileName);
                if (!File.Exists(photobookPath))
                {
                    continue;
                }

                try
                {
                    string serialized = File.ReadAllText(photobookPath);
                    var maybeBook = fileManager.Deserialize<Book>(serialized, AppJsonContext.Default.Book);
                    if (maybeBook is Book book)
                    {
                        this.Books.Add(book);
                    }
                }
                catch (Exception ex)
                {
                    string msg = "Exception thrown when loading book: " + photobookPath + " \n" + ex.ToString();
                    Debug.WriteLine(msg);
                    this.logger.Error(msg);

                    // Try to load other books 
                    continue;
                }
            }

            return true;
        }
        catch (Exception ex)
        {
            string msg = "Exception thrown when loading books. \n" + ex.ToString();
            Debug.WriteLine(msg);
            this.logger.Error(msg);
            return false;
        }
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
