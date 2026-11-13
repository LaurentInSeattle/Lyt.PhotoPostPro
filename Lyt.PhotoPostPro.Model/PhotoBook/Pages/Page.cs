namespace Lyt.PhotoPostPro.Model.PhotoBook.Pages;

public sealed class Page : PageBase
{
    public Page(Book book, int pageNumber) : base(book, pageNumber)
    {
        this.PageBackground = book.DefaultPageBackground;
    }

    public PageBackground PageBackground { get; set; }

    public List<Slot> Slots { get; } = [];
}

public class PageBase
{
    public PageBase(Book book, int pageNumber)
    {
        this.Book = book;
        this.PageNumber = pageNumber;
    }

    protected Book Book { get; }

    public int PageNumber { get; }

    public bool IsFrontPage => this.PageNumber % 2 == 0;

}

