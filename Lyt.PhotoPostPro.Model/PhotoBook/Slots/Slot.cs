namespace Lyt.PhotoPostPro.Model.PhotoBook.Slots;

[JsonPolymorphic(TypeDiscriminatorPropertyName = "$type")]
[JsonDerivedType(typeof(ImageSlot), typeDiscriminator: "ImageSlot")]
[JsonDerivedType(typeof(TextSlot), typeDiscriminator: "TextSlot")]
public class Slot
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public SlotPosition Position { get; set; } = new SlotPosition(0, 0, 0, 0);
}
