namespace Lyt.PhotoPostPro.Model.PhotoBook.Pages;

public class Leaf
{
    public Leaf() { } 

    public Leaf(Page frontPage, Page backPage)
    {
        this.FrontPage = frontPage;
        this.BackPage = backPage;
    }

    public required Page FrontPage { get; set; }

    public required Page BackPage { get; set; }
}

public sealed class FrontCover : Leaf
{
    public FrontCover() { } 

    public FrontCover(Book book) : base(new Page(book, 0), new Page(book, 1))
    {
    }
}

public sealed class BackCover : Leaf
{
    public BackCover() : base() { }

    public BackCover(Book book) : base(new Page(book, 0), new Page(book, 1))
    {
    }
}

