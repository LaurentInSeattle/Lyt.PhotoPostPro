namespace Lyt.PhotoPostPro.Workflow.Book.Edit;

using Books = Model.PhotoBook;
using Pages = Model.PhotoBook.Pages;
using Slots = Model.PhotoBook.Slots;

public partial class SlotViewModel : ViewModel<SlotView>
// , IRecipient<DocPageNavigateMessage>
{
    private readonly PhotoPostProModel model;

    private readonly Pages.Page page;
    private readonly Slots.Slot slot;
    private readonly Slots.ImageSlot? imageSlot;
    private readonly Slots.TextSlot? textSlot;

    [ObservableProperty]
    public partial bool IsImageSlot { get; set; }

    [ObservableProperty]
    public partial double SlotWidth { get; set; }

    [ObservableProperty]
    public partial double SlotHeight { get; set; }

    [ObservableProperty]
    public partial double SlotX { get; set; }

    [ObservableProperty]
    public partial double SlotY { get; set; }

    [ObservableProperty]
    public partial string SlotText { get; set; } = string.Empty;

    [ObservableProperty]
    public partial Bitmap? SlotImage { get; set; } 

    public SlotViewModel(PhotoPostProModel model, Pages.Page page, Slots.Slot slot)
    {
        this.model = model;
        this.page = page;
        this.slot = slot;

        // To enforce property changed ?
        // this.ClearPosition();
        if (slot is Slots.ImageSlot imageSlot)
        {
            this.IsImageSlot = true;
            this.imageSlot = imageSlot;
            this.SetImage(); 
        }
        else if (slot is Slots.TextSlot textSlot)
        {
            this.IsImageSlot = false;
            this.textSlot = textSlot;
            this.SetText(); 
        }
        else
        {
            throw new NotSupportedException("Slot type not supported.");
        }

        this.SetPosition();
    }

    public Books.Book Book 
        => this.model.BookManager.CurrentBook ?? throw new Exception ("No Bok ???") ;

    public void ClearPosition()
    {
        this.SlotWidth = 0;
        this.SlotHeight = 0;
        this.SlotX = -1;
        this.SlotY = -1;
    }

    public void SetPosition ()
    {
        var position = this.slot.Position;
        this.SlotWidth = position.Dx;
        this.SlotHeight = position.Dy;
        this.SlotX = position.X;
        this.SlotY = position.Y;
    }

    public void SetText ()
    {
        if ( this.IsImageSlot)
        {
            return; 
        }

        // ! Verified by IsImageSlot
        this.SlotText = this.textSlot!.Text; 
    }

    public void SetImage()
    {
        if (!this.IsImageSlot)
        {
            return;
        }

        if (this.SlotImage is not null)
        {
            this.SlotImage.Dispose();
        }

        // ! Verified by IsImageSlot
        string imagePath = this.imageSlot!.ImagePath(this.Book, BookResolution.Display); 
        if ( string.IsNullOrWhiteSpace(imagePath))
        {
            this.SlotImage = null;
        }
        else
        {
            var bitmap = new Bitmap(imagePath); 
            this.SlotImage = bitmap;
        }
    }
}
