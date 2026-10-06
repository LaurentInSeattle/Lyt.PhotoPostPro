namespace Lyt.PhotoPostPro.Model.Book;

public enum BookFormat
{
    Landscape,
    Portrait
}

public sealed class Book
{
    public string UniqueName { get; set; } = string.Empty;

    public string FileName { get; set; } = string.Empty;

    public BookFormat Format { get; set; } = BookFormat.Landscape;

    public FrontCover FrontCover { get; set; } 

    public BackCover BackCover { get; set; } 

    public List<Page> Pages { get; } = [];

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
    }

    public int PageNumber { get; }
}

public class PageBase
{
    public PageBase(Book book)
    {
        this.Book = book;
    }

    protected Book Book { get; }
}

public class PageLayout
{
}