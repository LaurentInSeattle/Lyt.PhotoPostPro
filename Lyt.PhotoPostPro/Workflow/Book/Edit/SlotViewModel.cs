namespace Lyt.PhotoPostPro.Workflow.Book.Edit;

using Lyt.Avalonia.Mvvm.Behaviors.DragDrop;
using Lyt.PhotoPostPro.Model.PhotoBook.Slots;
using Lyt.PhotoPostPro.Model.PhotoBook.Typography;
using Lyt.Avalonia.Controls;

using Books = Model.PhotoBook;
using Pages = Model.PhotoBook.Pages;
using Slots = Model.PhotoBook.Slots;

using static TextStyleUtilities;

public partial class SlotViewModel :
    ViewModel<SlotView>,
    IDropTarget,
    IPositionable,
    IRecipient<BookApplyStyleMessage>
{
    public readonly static FontFamily DefaultFontFamily = new("Arial");

    private readonly static SolidColorBrush? selectedBrush;
    private readonly static SolidColorBrush? highlightedBrush;

    static SlotViewModel()
    {
        Utilities.TryFindResource<SolidColorBrush>("OrangePeel_1_100", out highlightedBrush);
        Utilities.TryFindResource<SolidColorBrush>("LightAqua_0_080", out selectedBrush);
    }

    public static void ClearSelection() => SlotViewModel.SelectedSlot?.Select(false); 

    public static SlotViewModel? SelectedSlot
    {
        get;
        private set
        {
            field = value;

            if (value is null)
            {
                new BookTextSlotSelectedMessage(null).Publish();
            }
            else if (value.Slot is TextSlot)
            {
                new BookTextSlotSelectedMessage(value).Publish();
            }
        }
    }

    public readonly Pages.Page Page;
    public readonly Slots.Slot Slot;

    private readonly PhotoPostProModel model;
    private readonly BookEditViewModel bookEditViewModel;
    private readonly Slots.ImageSlot? imageSlot;
    private readonly Slots.TextSlot? textSlot;

    private bool isSelected;
    private bool isHighlighted;

    [ObservableProperty]
    public partial bool IsSlotBorderVisible { get; set; }

    [ObservableProperty]
    public partial SolidColorBrush? SlotBorderBrush { get; set; }

    [ObservableProperty]
    public partial bool IsImageSlot { get; set; }

    [ObservableProperty]
    public partial bool IsNotSuitableForPrint { get; set; } = false;

    [ObservableProperty]
    public partial IImmutableSolidColorBrush WarningBrush { get; set; } = Brushes.DarkOrange;

    [ObservableProperty]
    public partial double Dx { get; set; }

    [ObservableProperty]
    public partial double Dy { get; set; }

    [ObservableProperty]
    public partial double X { get; set; }

    [ObservableProperty]
    public partial double Y { get; set; }

    [ObservableProperty]
    public partial bool IsTextBoxVisible { get; set; }

    [ObservableProperty]
    public partial bool IsTextBlockVisible { get; set; }

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

    [ObservableProperty]
    public partial Stretch ImageStretch { get; set; }

    public SlotViewModel(PhotoPostProModel model, Pages.Page page, Slots.Slot slot)
    {
        this.model = model;
        this.Page = page;
        this.Slot = slot;

        this.bookEditViewModel = App.GetRequiredService<BookEditViewModel>();

        this.isSelected = false;
        this.isHighlighted = false;
        this.UpdateBorderVisualState();

        // To enforce property changed ?
        this.ClearPosition();
        this.SetPosition(this.Slot.Position);
        if (slot is Slots.ImageSlot imageSlot)
        {
            this.IsTextBlockVisible = false;
            this.IsTextBoxVisible = false;
            this.IsImageSlot = true;
            this.imageSlot = imageSlot;
            this.SetImage();
        }
        else if (slot is Slots.TextSlot textSlot)
        {
            this.IsTextBlockVisible = true;
            this.IsTextBoxVisible = false;
            this.IsImageSlot = false;
            this.textSlot = textSlot;
            this.SetText();

            // Styles are only for Text Slots
            this.Subscribe<BookApplyStyleMessage>();
        }
        else
        {
            throw new NotSupportedException("Slot type not supported.");
        }
    }

    public Books.Book Book
        => this.model.BookManager.CurrentBook ?? throw new Exception("No Book ???");

    public void ClearPosition()
    {
        this.Dx = 0;
        this.Dy = 0;
        this.X = 0;
        this.Y = 0;
    }

    public void SetPosition(SlotPosition slotPosition)
    {
        this.X = slotPosition.X;
        this.Y = slotPosition.Y;
        this.Dx = slotPosition.Dx;
        this.Dy = slotPosition.Dy;
    }

    public void SetFinalPosition(SlotPosition slotPosition)
    {
        this.SetPosition(slotPosition);
        this.model.BookManager.RepositionSlot(this.Slot, this.Page, slotPosition);
    }

    public void SetText()
    {
        if (this.textSlot is null)
        {
            return;
        }

        this.SlotText = this.textSlot.Text;
        this.IsNotSuitableForPrint = false; 

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
        if (this.imageSlot is null)
        {
            return;
        }

        if (this.SlotImage is not null)
        {
            this.SlotImage.Dispose();
        }

        BookImage.SuitableForPrint suitableForPrint = 
            this.imageSlot.PrintSuitability(this.Book, (float) this.Dx, (float)this.Dy);
        switch (suitableForPrint)
        {
            default:
            case BookImage.SuitableForPrint.Unknown:
            case BookImage.SuitableForPrint.Optimal:
                this.IsNotSuitableForPrint = false; 
                break;

            case BookImage.SuitableForPrint.Minimal:
                this.IsNotSuitableForPrint = true;
                this.WarningBrush = Brushes.DarkOrange; 
                break;

            case BookImage.SuitableForPrint.Unsuitable:
                this.IsNotSuitableForPrint = true;
                this.WarningBrush = Brushes.Firebrick;
                break;
        }

        string imagePath = this.imageSlot.ImagePath(this.Book, BookResolution.Display);
        if (string.IsNullOrWhiteSpace(imagePath))
        {
            this.SlotImage = null;
        }
        else
        {
            var bitmap = new Bitmap(imagePath);
            this.SlotImage = bitmap;
            this.ImageStretch = StretchFromImageSlot(this.imageSlot);
        }
    }

    public void Receive(BookApplyStyleMessage message)
    {
        if (this != message.TextSlotViewModel)
        {
            return;
        }

        if (this.textSlot is null)
        {
            return;
        }

        var style = message.TextStyle;

        // This needs to be done in model 
        this.model.BookManager.UpdateTextSlotStyle(this.Page, textSlot, style);

        this.textSlot.Style = style;
        this.SetText();
    }

    public void OnClicked(bool isRightClick) => this.Select(!this.isSelected);

    public void Select(bool select = true)
    {
        this.isSelected = select;
        this.UpdateBorderVisualState();
        if (select)
        {
            SlotViewModel.ClearSelection();
            SlotViewModel.SelectedSlot = this;

            // ! we have a book  
            var pageSize = this.model.BookManager.CurrentBook!.PageSize;
            var adorner = new ResizeAdorner(this.View, pageSize.Width, pageSize.Height, snaps: 10);
            AdornerLayer.SetAdorner(this.View, adorner);
        }
        else
        {
            AdornerLayer.SetAdorner(this.View, null);
            SlotViewModel.SelectedSlot = null;
        }
    }

    internal void Highlight(bool highlight = true)
    {
        this.isHighlighted = highlight;
        this.UpdateBorderVisualState();
    }

    private void UpdateBorderVisualState()
    {
        this.IsSlotBorderVisible = this.isSelected || this.isHighlighted;
        if (this.isHighlighted)
        {
            this.SlotBorderBrush = highlightedBrush;
        }
        else if (this.isSelected)
        {
            this.SlotBorderBrush = selectedBrush;
        }
    }

    public bool CanDrop(Point point, object droppedObject)
    {
        if (this.IsImageSlot && this.isSelected)
        {
            if (!this.isHighlighted)
            {
                this.Highlight();
                Schedule.OnUiThread(1_500, () => { this.Highlight(false); }, DispatcherPriority.Background);
            }

            return true;
        }

        return false;
    }

    public void OnDrop(Point point, object droppedObject)
    {
        if (this.Slot is not ImageSlot imageSlot)
        {
            return;
        }

        if (droppedObject is not BookImageViewModel bookImageViewModel)
        {
            return;
        }

        Guid imageId = bookImageViewModel.Key;
        if (imageId == Guid.Empty)
        {
            return;
        }

        this.Highlight(false);

        // Ready for drop 
        Debug.WriteLine(" Dropped Guid : " + imageId.ToString());
        this.model.BookManager.ReplaceImage(this.Page, imageSlot, imageId);
    }

    internal void BeginEdit()
    {
        if (this.Slot is ImageSlot)
        {
            return;
        }

        if (this.IsTextBoxVisible)
        {
            // We are editing: do not edit again 
            this.EndEdit();
            return;
        }

        this.IsTextBoxVisible = true;
        this.IsTextBlockVisible = false;
        AdornerLayer.SetAdorner(this.View, null);

        // Focus and move caret to the very end
        TextBox textBox = this.View.SlotTextBox;
        textBox.Focus();
        textBox.CaretIndex = textBox.Text?.Length ?? 0;
    }

    internal void OnTextBoxLostFocus() => this.EndEdit();

    internal void EndEdit()
    {
        if (this.Slot is not TextSlot textSlot)
        {
            return;
        }

        this.IsTextBoxVisible = false;
        this.IsTextBlockVisible = true;
        this.model.BookManager.UpdateTextSlotText(this.Page, textSlot, this.SlotText);
    }
}
