namespace Lyt.PhotoPostPro.Model.PhotoBook.Layouts;

public abstract class Layout
{
    public PageLayout PageLayout { get; set; } = PageLayout.Empty;

    public int ImageCount { get; set; } = 0;

    public int TextCount { get; set; } = 0;

    public abstract void Create( Page page);
}
