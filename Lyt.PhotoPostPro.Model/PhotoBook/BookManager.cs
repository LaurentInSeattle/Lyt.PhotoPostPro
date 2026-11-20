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

    public readonly string PhotobooksFolderPath = string.Empty;

    private readonly PhotoPostProModel model;
    private readonly FileManagerModel fileManager;
    private readonly ILogger logger;

    public BookManager(PhotoPostProModel model, FileManagerModel fileManager, ILogger logger)
    {
        this.model = model;
        this.fileManager = fileManager;
        this.logger = logger;

        // Setup the photobook folder path based on the Pictures folder and the application name.
        string pictures = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);
        this.PhotobooksFolderPath = Path.Combine(pictures, PhotoPostProModel.PhotoPostProAppName, PhotoBooksFolderName);
        if (!Directory.Exists(this.PhotobooksFolderPath))
        {
            Directory.CreateDirectory(this.PhotobooksFolderPath);
        }

        this.LoadBooks();

        if (string.IsNullOrWhiteSpace(this.model.PhotoBook) && this.Books.Count > 0)
        {
            this.model.PhotoBook = this.Books[0].FriendlyName;
        }
    }

    public List<Book> Books { get; } = [];

    public bool IsEditing { get; set; }

    public Book? CurrentBook { get; set; }

    #region Entire Book Management

    public Book? CreateBook(string friendlyName, string description, out string message)
        => this.CreateBook(
                friendlyName,
                description,
                BookPage.UsLetter,
                BookFormat.Landscape,
                PageBackground.Black,
                withFrontCover: true,
                withBackCover: false,
                out message);

    public Book? CreateBook(
        string friendlyName,
        string description,
        BookPage bookPage,
        BookFormat format,
        PageBackground defaultPageBackground,
        bool withFrontCover,
        bool withBackCover,
        out string message)
    {
        message = string.Empty;

        var book = this.GetBook(friendlyName);
        if (book is not null)
        {
            message = "Book.Create.AlreadyExist";
            return null;
        }

        if (friendlyName.Length < 2)
        {
            message = "Book.Create.UniqueName";
            return null;
        }

        const string FallbackFolderName = "_F_a_l_l_b_a_c_k_";
        string folderName = FileSystemExtensions.SanitizeFolderName(friendlyName, FallbackFolderName, minLength: 2);
        if (folderName == FallbackFolderName)
        {
            message = "Book.Create.UniqueName";
            return null;
        }

        book = new Book
        {
            MaybeLogger = this.logger,

            FriendlyName = friendlyName,
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
            this.BeginEditBook(book);
            new BookListChangedMessage().Publish();
            return book;
        }
        else
        {
            message = "Book.Create.CantSave";
            return null;
        }
    }

    public bool DeleteBook(string friendlyName, out string message)
    {
        var maybeBook = this.GetBook(friendlyName);
        if (maybeBook is not Book book)
        {
            message = "Book.Delete.NotExist";
            return false;
        }

        return this.DeleteBook(book, out message);
    }

    public bool DeleteBook(Book book, out string message)
    {
        message = string.Empty;
        try
        {
            var stillBook = this.GetBook(book.FriendlyName);
            if (stillBook is null)
            {
                message = "Book.Delete.NotExist";
                return false;
            }

            string photobookFolderPath = Path.Combine(this.PhotobooksFolderPath, book.FolderName);
            if (Directory.Exists(photobookFolderPath))
            {
                // Consider moving stuff to Recycle bin
                Directory.Delete(photobookFolderPath, recursive: true);
            }

            this.EndEditBook();
            this.Books.Remove(book);
            new BookListChangedMessage().Publish();
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

    public Book? GetBook(string friendlyName) => this.Books.FirstOrDefault(b => b.FriendlyName == friendlyName);

    public bool SelectBook(string friendlyName)
    {
        var maybeBook = this.GetBook(friendlyName);
        if (maybeBook is not Book book)
        {
            return false;
        }

        this.BeginEditBook(book);
        return true;
    }

    public void BeginEditBook(Book book)
    {
        this.IsEditing = true;
        this.CurrentBook = book;
        this.model.PhotoBook = book.FriendlyName;
        this.model.Save();
        new BookListChangedMessage().Publish();
    }

    public void EndEditBook()
    {
        if (this.CurrentBook is not null)
        {
            if (this.model.PhotoBook == this.CurrentBook.FriendlyName)
            {
                if (this.Books.Count > 0)
                {
                    this.BeginEditBook(this.Books[0]);
                }
                else
                {
                    this.IsEditing = false;
                    this.CurrentBook = null;
                    this.model.PhotoBook = string.Empty;
                }
            }
        }
        else
        {
            this.IsEditing = false;
            this.model.PhotoBook = string.Empty;
        }

        this.model.Save();
        new BookListChangedMessage().Publish();
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
            string photobookFolderPath = Path.Combine(this.PhotobooksFolderPath, book.FolderName);
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
            List<string> directories = FileSystemExtensions.EnumerateDirectories(this.PhotobooksFolderPath);
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
                        book.MaybeLogger = this.logger;
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

    #endregion Entire Book Management

    #region Image Management

    public bool ImportImage(string exportedImagePath, string exportedThumbnailPath = "")
    {
        if (this.CurrentBook is null)
        {
            return false;
        }

        Guid guid = this.CurrentBook.ImportImage(
            PhotobooksFolderPath,
            exportedImagePath, exportedThumbnailPath,
            out string message);
        if (guid == Guid.Empty)
        {
            Debug.WriteLine(message);
            this.logger.Error("Failed to import image: " + message);
            return false;
        }

        return this.SaveCurrentBook();
    }

    #endregion Image Management

    #region Page Management

    // Insert a single page at the provided page number in the current book, shifting existing pages and leaves forward.
    public bool InsertPage(int pageNumber, out string message)
    {
        message = string.Empty;
        if (this.CurrentBook == null)
        {
            throw new InvalidOperationException("No current book to add a leaf to.");
        }

        if ((pageNumber <= 1) && this.CurrentBook.WithFrontCover)
        {
            message = "Workflow.Book.Edit.CantChangeCover"; 
            return false;
        }

        var pages = this.CurrentBook.Pages;
        if ((pageNumber > pages.Count - 2) && this.CurrentBook.WithBackCover)
        {
            message = "Workflow.Book.Edit.CantChangeCover";
            return false;
        }

        // If we have a front cover, page number needs to be ajusted 
        // because we pass in the page number in the PDF preview
        if (this.CurrentBook.WithFrontCover)
        {
            pageNumber -= 2; 
        }

        // Create a page 
        var page = new Page(this.CurrentBook);

        // Insert in collection 
        if (pageNumber == pages.Count-1)
        {
            pages.Add(page);
        }
        else
        {
            pages.Insert(pageNumber, page);
        }

        // Save and Post message 
        this.SaveCurrentBook(); 
        new BookPagesChangedMessage().Publish();
        return true;
    }

    public bool RemovePage(int pageNumber, out string message)
    {
        message = string.Empty;
        if (this.CurrentBook == null)
        {
            throw new InvalidOperationException("No current book to remove a leaf from.");
        }

        message = string.Empty;
        if (this.CurrentBook == null)
        {
            throw new InvalidOperationException("No current book to add a leaf to.");
        }

        if ((pageNumber <= 1) && this.CurrentBook.WithFrontCover)
        {
            message = "Workflow.Book.Edit.CantChangeCover";
            return false;
        }

        var pages = this.CurrentBook.Pages;
        if ((pageNumber > pages.Count - 2) && this.CurrentBook.WithBackCover)
        {
            message = "Workflow.Book.Edit.CantChangeCover";
            return false;
        }

        // If we have a front cover, page number needs to be ajusted 
        // because we pass in the page number in the PDF preview
        if (this.CurrentBook.WithFrontCover)
        {
            pageNumber -= 2;
        }

        pages.RemoveAt(pageNumber);

        // Save and Post message 
        this.SaveCurrentBook();
        new BookPagesChangedMessage().Publish();
        return true;
    }

    //public void MovePage(int fromPageNumber, int toPageNumber)
    //{
    //    if (this.CurrentBook == null)
    //    {
    //        throw new InvalidOperationException("No current book to move a leaf in.");
    //    }
    //    //// Find the leaf that contains the fromPageNumber
    //    //var leafToMove = this.Current.Leaves.FirstOrDefault(leaf =>
    //    //    leaf.FrontPage.PageNumber == fromPageNumber || leaf.BackPage.PageNumber == fromPageNumber);
    //    //if (leafToMove != null)
    //    //{
    //    //    this.Current.Leaves.Remove(leafToMove);
    //    //    this.Current.Leaves.Insert(toPageNumber / 2, leafToMove);
    //    //}
    //}

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
