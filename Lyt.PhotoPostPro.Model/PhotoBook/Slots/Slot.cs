namespace Lyt.PhotoPostPro.Model.PhotoBook.Slots;

public class Slot
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public SlotPosition Position { get; set; } = new SlotPosition(0, 0, 0, 0);
}

