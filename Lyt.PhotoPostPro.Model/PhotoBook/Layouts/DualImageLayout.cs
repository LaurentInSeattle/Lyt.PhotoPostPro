namespace Lyt.PhotoPostPro.Model.PhotoBook.Layouts;

public sealed class DualImageLayout : Layout
{
    public static void Create(Book book, Page page, Guid bookImageIdTop, Guid bookImageIdBottom)
    {
        var layout = new DualImageLayout();
        layout.Create(book, page);
        var slot0 = (ImageSlot)page.Slots[0];
        slot0.BookImageId = bookImageIdTop;
        var slot1 = (ImageSlot)page.Slots[1];
        slot1.BookImageId = bookImageIdBottom;
    }

    public DualImageLayout() : base(PageLayout.SingleImage)
    {
        base.ImageSlots.Add(new SlotPosition(0.08f, 0.08f, 0.52f, 0.4f));
        base.ImageSlots.Add(new SlotPosition(0.4f, 0.42f, 0.52f, 0.4f));
    }
}

public sealed class DualImageLayoutWithLegend : Layout
{
    public static void Create(
        Book book, Page page, 
        Guid bookImageIdTop, string textTop, 
        Guid bookImageIdBottom, string textBottom)
    {
        var layout = new DualImageLayoutWithLegend();
        layout.Create(book, page);
        var slot0 = (ImageSlot)page.Slots[0];
        slot0.BookImageId = bookImageIdTop;
        var slot1 = (ImageSlot)page.Slots[1];
        slot1.BookImageId = bookImageIdBottom;
        var slot2 = (TextSlot)page.Slots[2];
        slot2.Text = textTop;
        var slot3 = (TextSlot)page.Slots[3];
        slot3.Text = textBottom;
    }

    public DualImageLayoutWithLegend() : base(PageLayout.SingleImage)
    {
        base.ImageSlots.Add(new SlotPosition(0.08f, 0.08f, 0.48f, 0.4f));
        base.ImageSlots.Add(new SlotPosition(0.48f, 0.52f, 0.48f, 0.4f));
        base.TextSlots.Add(new SlotPosition(0.6f, 0.08f, 0.3f, 0.3f));
        base.TextSlots.Add(new SlotPosition(0.08f, 0.52f, 0.3f, 0.3f));
    }
}
