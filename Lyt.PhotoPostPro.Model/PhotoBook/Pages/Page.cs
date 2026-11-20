namespace Lyt.PhotoPostPro.Model.PhotoBook.Pages;

public sealed class Page
{
    public Page() { }

    public Page(Book book)
    {
        this.PageBackground = book.DefaultPageBackground;
    }

    public PageBackground PageBackground { get; set; }

    public List<Slot> Slots { get; set; } = [];
}
