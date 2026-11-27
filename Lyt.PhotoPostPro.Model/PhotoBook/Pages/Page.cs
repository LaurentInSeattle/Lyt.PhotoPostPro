namespace Lyt.PhotoPostPro.Model.PhotoBook.Pages;

public sealed class Page
{
    public Page() { }

    public Page(Book book, bool isFront = true )
    {
        this.PageBackground = book.DefaultPageBackground;
        this.IsFront = isFront;
    }

    public PageBackground PageBackground { get; set; }

    public List<Slot> Slots { get; set; } = [];

    [JsonIgnore]
    public bool IsFront { get; set; } = true;

    [JsonIgnore]
    public bool IsBack => ! this.IsFront;
}
