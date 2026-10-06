namespace Lyt.PhotoPostPro.Model.Book;

// See:  https://github.com/GraphicMeat/PhotoBooks/blob/main/README.md 

public enum BookFormat
{
    Landscape,
    Square,
    Portrait,
}

public enum PageBackground
{
    White,
    Ivory,  // #FEFEF2
    Licorice, // #1A_11_10
    Black,
}

public sealed class Book
{
    public string UniqueName { get; set; } = string.Empty;

    public string FileName { get; set; } = string.Empty;

    public BookFormat Format { get; set; } = BookFormat.Landscape;

    public FrontCover FrontCover { get; set; } 

    public BackCover BackCover { get; set; } 

    public List<Page> Pages { get; } = [];

    public PageBackground DefaultPageBackground { get; set; } = PageBackground.Black;

    public Book()
    {
        this.FrontCover = new FrontCover(this);
        this.Pages.Add(new Page(this, 1));
        this.BackCover = new BackCover(this);
    }

    public void GeneratePdf()
    {
    }

} 

public sealed class FrontCover : PageBase
{
    public FrontCover(Book book) : base(book)
    {
    }

    public string Title { get; set; } = string.Empty;
}

public sealed class BackCover : PageBase
{
    public BackCover(Book book) : base(book)
    {
    }
}

public sealed class Page : PageBase
{
    public Page(Book book, int pageNumber) : base(book)
    {
        this.PageNumber = pageNumber;
        this.PageBackground = book.DefaultPageBackground;
    }

    public int PageNumber { get; }

    public PageBackground PageBackground { get; set; } 

}

public class PageBase
{
    public PageBase(Book book)
    {
        this.Book = book;
    }

    protected Book Book { get; }
}

public enum PageLayout
{
    Empty,
}

public class Layout
{
    public PageLayout PageLayout { get; set; } = PageLayout.Empty;

    public int ImageCount { get; set; }
}

public class ImagePosition
{
    public int X { get; set; }

    public int Y { get; set; }

    public int Dx { get; set; }

    public int Dy { get; set; }
}

public class Image
{
    public string Accessor { get; set; } = string.Empty;
}