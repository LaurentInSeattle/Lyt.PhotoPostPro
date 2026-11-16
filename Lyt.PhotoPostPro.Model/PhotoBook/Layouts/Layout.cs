namespace Lyt.PhotoPostPro.Model.PhotoBook.Layouts;

public abstract class Layout
{
    protected Layout(PageLayout pageLayout) => this.PageLayout = pageLayout;

    public PageLayout PageLayout { get; private set; } 

    public List<SlotPosition> ImageSlots = [];

    public List<SlotPosition> TextSlots = [];

    public int ImageCount => this.ImageSlots.Count;

    public int TextCount => this.TextSlots.Count;

    public virtual void Create(Book book, Page page)
    {
        page.Slots.Clear();
        var pageSize = book.PageSize;
        foreach(var position in this.ImageSlots)
        {
            var slot = new ImageSlot()
            {
                Id = Guid.NewGuid(),
                Position = 
                    new SlotPosition(
                        position.X * pageSize.Width,
                        position.Y * pageSize.Height,
                        position.Dx * pageSize.Width,
                        position.Dy * pageSize.Height),
                ImageStretch = BookImageStretch.Fit,
            };

            page.Slots.Add(slot);
        }

        foreach (var position in this.TextSlots)
        {
            var slot = new TextSlot()
            {
                Id = Guid.NewGuid(),
                Position =
                    new SlotPosition(
                        position.X * pageSize.Width,
                        position.Y * pageSize.Height,
                        position.Dx * pageSize.Width,
                        position.Dy * pageSize.Height),
                Text = string.Empty,
                Style = new SlotTextStyle()
            };

            page.Slots.Add(slot);
        }
    }
}
