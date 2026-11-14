namespace Lyt.PhotoPostPro.Model.PhotoBook.Layouts;

public sealed class FullPageImageLayout : Layout
{
    public FullPageImageLayout()
    {
        this.ImageCount = 1;
        this.TextCount = 0;
    }

    public void Create(Page page, string imageFileName)
    {
        this.Create(page);
        ((ImageSlot )page.Slots[0]).ImageFileName = imageFileName;
    } 

    public override void Create(Page page)
    {
        var pageSize = page.Book.PageSize;
        var slot = new ImageSlot()
        {
            Id = Guid.NewGuid(),
            Position = new SlotPosition(0, 0, pageSize.Width, pageSize.Height),
            ImageFileName = string.Empty,
        };

        page.Slots.Clear();
        page.Slots.Add(slot);
    }
}
