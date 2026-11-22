namespace Lyt.PhotoPostPro.Model.PhotoBook.Layouts;

public sealed class SingleImageLayout : Layout
{
    public static void Create(Book book, Page page, Guid bookImageId)
    {
        var layout = new SingleImageLayout(); 
        layout.Create(book, page);
        var slot = (ImageSlot)page.Slots[0];
        slot.BookImageId = bookImageId;
    }

    public SingleImageLayout() : base(PageLayout.SingleImage)
        => base.ImageSlots.Add(new SlotPosition(0.08f, 0.08f, 0.82f, 0.82f));
}

public sealed class SingleImageLayoutWithLegend : Layout
{
    public static void Create(Book book, Page page, Guid bookImageId, string legend)
    {
        var layout = new SingleImageLayoutWithLegend();
        layout.Create(book, page);
        var imageSlot = (ImageSlot)page.Slots[0];
        imageSlot.BookImageId = bookImageId;
        var textSlot = (TextSlot)page.Slots[1];
        textSlot.Text = legend;
        textSlot.Style = SlotTextStyle.LargeBodyText; 
        textSlot.Style.TextAlignment = BookTextAlignment.Center;
    }

    public SingleImageLayoutWithLegend() : base(PageLayout.SingleImageWithLegend)
    {
        base.ImageSlots.Add(new SlotPosition(0.08f, 0.08f, 0.84f, 0.84f));
        base.TextSlots.Add(new SlotPosition(0.0f, 0.9f, 1.0f, 0.1f));
    }
}
