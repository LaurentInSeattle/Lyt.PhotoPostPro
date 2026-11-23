namespace Lyt.PhotoPostPro.Model.PhotoBook.Slots;

[JsonPolymorphic(TypeDiscriminatorPropertyName = "$type")]
[JsonDerivedType(typeof(ImageSlot), typeDiscriminator: "ImageSlot")]
[JsonDerivedType(typeof(TextSlot), typeDiscriminator: "TextSlot")]
public class Slot
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public SlotPosition Position { get; set; } = new SlotPosition(0.1f, 0.1f, 0.8f, 0.8f);

    public void AdjustToPage (Book book )
    {
        float width = book.PageSize.Width;
        float height = book.PageSize.Height;
        SlotPosition newSlotPosition =
            new ()
            {
                X = this.Position.X * width,
                Y = this.Position.Y * height,
                Dx = this.Position.Dx * width,
                Dy = this.Position.Dy * height,
            };
        this.Position = newSlotPosition; 
    }
}
