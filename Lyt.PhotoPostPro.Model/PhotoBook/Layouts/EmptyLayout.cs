namespace Lyt.PhotoPostPro.Model.PhotoBook.Layouts;

public sealed class EmptyLayout : Layout
{
    public EmptyLayout() : base(PageLayout.Empty) { }

    // Implementation for creating an empty layout does not require any slots or content,
    // so this method can remains empty.
    public static new void Create(Page _) { }
}
