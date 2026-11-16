namespace Lyt.PhotoPostPro.Model.PhotoBook.Pages;

public sealed class Page : PageBase
{
    public Page() : base() { }

    public Page(Book book, int pageNumber) : base(pageNumber)
    {
        this.PageBackground = book.DefaultPageBackground;
    }

    public PageBackground PageBackground { get; set; }

    public List<Slot> Slots { get; set; } = [];
}

public class PageBase
{
    public PageBase() { } 

    public PageBase(int pageNumber)
    {
        this.PageNumber = pageNumber;
    }

    public int PageNumber { get; set; }

    public bool IsFrontPage => this.PageNumber % 2 == 0;
}

