namespace Lyt.PhotoPostPro.Workflow.Book.Edit;

using QuestPDF.Infrastructure;

using Books = Model.PhotoBook;
using Pages = Model.PhotoBook.Pages;
using Slots = Model.PhotoBook.Slots;

public partial class SlotViewModel : ViewModel<SlotView>
// , IRecipient<DocPageNavigateMessage>
{
    public static FontFamily DefaultFontFamily = new("Arial"); 

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
    public partial FontFamily SlotFontFamily { get; set; } = DefaultFontFamily; 

    [ObservableProperty]
    public partial double SlotFontSize { get; set; }

    [ObservableProperty]
    public partial FontStyle SlotFontStyle { get; set; }

    [ObservableProperty]
    public partial FontWeight SlotFontWeight { get; set; }

    [ObservableProperty]
    public partial SolidColorBrush SlotForegroundColor { get; set; } = new(Colors.White);

    [ObservableProperty]
    public partial TextAlignment SlotTextAlignment { get; set; }

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
        => this.model.BookManager.CurrentBook ?? throw new Exception("No Bok ???");

    public void ClearPosition()
    {
        this.SlotWidth = 0;
        this.SlotHeight = 0;
        this.SlotX = -1;
        this.SlotY = -1;
    }

    public void SetPosition()
    {
        var position = this.slot.Position;
        this.SlotWidth = position.Dx;
        this.SlotHeight = position.Dy;
        this.SlotX = position.X;
        this.SlotY = position.Y;
    }

    public void SetText()
    {
        if (this.IsImageSlot)
        {
            return;
        }

        // ! Verified by IsImageSlot
        this.SlotText = this.textSlot!.Text;

        // Styling the text slot 
        var style = this.textSlot.Style;
        this.SlotFontFamily = new FontFamily(style.FontFamily);
        this.SlotFontSize = style.FontSize;
        this.SlotForegroundColor = FromTextStyle(style);
        var fontStyles = FromTextStyleFontStyle(style);
        this.SlotFontStyle = fontStyles.Item1;
        this.SlotFontWeight = fontStyles.Item2;
        this.SlotTextAlignment = FromTextStyleTextAlignment(style); 
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
        if (string.IsNullOrWhiteSpace(imagePath))
        {
            this.SlotImage = null;
        }
        else
        {
            var bitmap = new Bitmap(imagePath);
            this.SlotImage = bitmap;
        }
    }

    public static SolidColorBrush FromTextStyle(Slots.SlotTextStyle textStyle)
        => new(global::Avalonia.Media.Color.FromRgb(textStyle.Red, textStyle.Green, textStyle.Blue));

    public static TextAlignment FromTextStyleTextAlignment(Slots.SlotTextStyle textStyle)
        => textStyle.TextAlignment switch
        {
            BookTextAlignment.Center => TextAlignment.Center,
            BookTextAlignment.Right => TextAlignment.Right,
            _ => TextAlignment.Left,
        };

    public static (FontStyle, FontWeight) FromTextStyleFontStyle(Slots.SlotTextStyle textStyle)
    {
        var fontStyle = textStyle.FontStyle switch
        {
            BookFontStyle.BoldItalic or BookFontStyle.Italic => FontStyle.Italic,
            _ => FontStyle.Normal,
        };

        var fontWeight = textStyle.FontStyle switch
        {
            BookFontStyle.BoldItalic or BookFontStyle.Bold => FontWeight.Bold,
            _ => FontWeight.Normal,
        };

        return (fontStyle, fontWeight);
    }
}
