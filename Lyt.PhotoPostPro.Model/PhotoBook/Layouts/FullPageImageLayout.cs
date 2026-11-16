namespace Lyt.PhotoPostPro.Model.PhotoBook.Layouts;

public sealed class FullPageImageLayout : Layout
{
    public FullPageImageLayout() : base(PageLayout.FullPageImage) 
        => base.ImageSlots.Add(new SlotPosition(0, 0, 1, 1));

    public static void Create(Book book, Page page, Guid bookImageId)
    {
        var layout = new FullPageImageLayout(); 
        layout.Create(book, page);
        var slot = (ImageSlot)page.Slots[0];
        slot.BookImageId = bookImageId;
        slot.ImageStretch = BookImageStretch.Stretch;
    }
}
