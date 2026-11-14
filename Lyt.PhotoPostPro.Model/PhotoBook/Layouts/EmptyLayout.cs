namespace Lyt.PhotoPostPro.Model.PhotoBook.Layouts;

public sealed class EmptyLayout : Layout
{
    public EmptyLayout()
    {
        this.ImageCount = 0;
        this.TextCount = 0;
    }

    // Implementation for creating an empty layout does not require any slots or content,
    // so this method can remains empty.
    public override void Create(Page page) { }
}
