namespace Lyt.PhotoPostPro.Model.PhotoBook.Pages;

public class Leaf
{
    public Leaf(Page frontPage, Page backPage)
    {
        this.FrontPage = frontPage;
        this.BackPage = backPage;
    }

    public Page FrontPage { get; }

    public Page BackPage { get; }
}

public sealed class FrontCover : Leaf
{
    public FrontCover(Book book) : base(new Page(book, 0), new Page(book, 1))
    {
    }
}

public sealed class BackCover : Leaf
{
    public BackCover(Book book) : base(new Page(book, 0), new Page(book, 1))
    {
    }
}

