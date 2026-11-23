namespace Lyt.PhotoPostPro.Workflow.Book.Edit;

using Lyt.Avalonia.Mvvm.Behaviors.DragDrop;
using Lyt.PhotoPostPro.Model.PhotoBook.Slots;
using Lyt.PhotoPostPro.Model.PhotoBook.Typography;
using Lyt.Avalonia.Controls;

using QuestPDF.Infrastructure;

using Books = Model.PhotoBook;
using Pages = Model.PhotoBook.Pages;
using Slots = Model.PhotoBook.Slots;
using Typography = Model.PhotoBook.Typography;

public partial class SlotViewModel : ViewModel<SlotView>, IDropTarget
// , IRecipient<>
{
    #region Static Utilities 

    public static FontFamily DefaultFontFamily = new("Arial");

    private static SolidColorBrush? selectedBrush;
    private static SolidColorBrush? highlightedBrush;

    static SlotViewModel()
    {
        Utilities.TryFindResource<SolidColorBrush>("OrangePeel_1_100", out highlightedBrush);
        Utilities.TryFindResource<SolidColorBrush>("LightAqua_0_080", out selectedBrush);
    }

    public static SolidColorBrush FromTextStyle(Typography.SlotTextStyle textStyle)
        => new(global::Avalonia.Media.Color.FromRgb(textStyle.Red, textStyle.Green, textStyle.Blue));

    public static TextAlignment FromTextStyleTextAlignment(Typography.SlotTextStyle textStyle)
        => textStyle.TextAlignment switch
        {
            BookTextAlignment.Center => TextAlignment.Center,
            BookTextAlignment.Right => TextAlignment.Right,
            _ => TextAlignment.Left,
        };

    public static (FontStyle, FontWeight) FromTextStyleFontStyle(Typography.SlotTextStyle textStyle)
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

    public static global::Avalonia.Media.Stretch StretchFromImageSlot(Slots.ImageSlot slot)
        => slot.ImageStretch switch
        {
            BookImageStretch.Stretch => Stretch.Fill,
            _ => Stretch.Uniform,
        };

    #endregion Static Utilities 

    public static SlotViewModel? SelectedSlot;

    public readonly Pages.Page Page;
    public readonly Slots.Slot Slot;

    private readonly PhotoPostProModel model;
    private readonly BookEditViewModel bookEditViewModel;
    private readonly Slots.ImageSlot? imageSlot;
    private readonly Slots.TextSlot? textSlot;

    private bool isSelected;
    private bool isHighlighted;
    private bool isEdited;

    [ObservableProperty]
    public partial bool IsSlotBorderVisible { get; set; }
    
    [ObservableProperty]
    public partial bool AreSlotHandlesVisible { get; set; }

    [ObservableProperty]
    public partial SolidColorBrush? SlotBorderBrush { get; set; }

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
        this.isEdited = false;
        this.UpdateBorderVisualState();

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
        => this.model.BookManager.CurrentBook ?? throw new Exception("No Book ???");

    public void ClearPosition()
    {
        this.SlotWidth = 0;
        this.SlotHeight = 0;
        this.SlotX = -1;
        this.SlotY = -1;
    }

    public void SetPosition()
    {
        var position = this.Slot.Position;
        this.SlotWidth = position.Dx;
        this.SlotHeight = position.Dy;
        this.SlotX = position.X;
        this.SlotY = position.Y;
    }

    public void SetText()
    {
        if (this.textSlot is null)
        {
            return;
        }

        this.SlotText = this.textSlot.Text;

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

    public void OnClicked(bool isRightClick)
    {
        this.Select(!this.isSelected);
    } 

    public void Select(bool select = true)
    {
        this.isSelected = select;
        this.UpdateBorderVisualState();
        if (select)
        {
            SlotViewModel.SelectedSlot?.Select(false);
            SlotViewModel.SelectedSlot = this;
        }
        else
        {
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

        this.AreSlotHandlesVisible = true; 
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

    }
}

/*
 * 
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Media;

public class ResizeAdorner : Control
{
    private readonly Thumb _topLeft;
    private readonly Thumb _bottomRight;
    private readonly Control _adornedElement;

    public ResizeAdorner(Control adornedElement)
    {
        _adornedElement = adornedElement;

        // Create corner handles
        _topLeft = CreateThumb(StandardCursorType.TopLeftCorner);
        _bottomRight = CreateThumb(StandardCursorType.BottomRightCorner);

        // Wire up drag events
        _bottomRight.DragDelta += OnBottomRightDrag;
        _topLeft.DragDelta += OnTopLeftDrag;

        // Add thumbs to the visual tree of the adorner
        VisualChildren.Add(_topLeft);
        VisualChildren.Add(_bottomRight);
    }

    private Thumb CreateThumb(StandardCursorType cursor)
    {
        return new Thumb
        {
            Width = 10,
            Height = 10,
            Background = Brushes.White,
            BorderBrush = Brushes.DodgerBlue,
            BorderThickness = new Thickness(1.5),
            Cursor = new Cursor(cursor)
        };
    }

    // Position the handles relative to the adorned rectangle
    protected override Size ArrangeOverride(Size finalSize)
    {
        double halfThumb = 5; // Half of thumb width/height to center on corners

        _topLeft.Arrange(new Rect(-halfThumb, -halfThumb, 10, 10));
        _bottomRight.Arrange(new Rect(finalSize.Width - halfThumb, finalSize.Height - halfThumb, 10, 10));

        return finalSize;
    }

    private void OnBottomRightDrag(object? sender, VectorEventArgs e)
    {
        // Prevent negative dimensions
        double newWidth = Math.Max(10, _adornedElement.Width + e.Vector.X);
        double newHeight = Math.Max(10, _adornedElement.Height + e.Vector.Y);

        _adornedElement.Width = newWidth;
        _adornedElement.Height = newHeight;
    }

    private void OnTopLeftDrag(object? sender, VectorEventArgs e)
    {
        double newWidth = Math.Max(10, _adornedElement.Width - e.Vector.X);
        double newHeight = Math.Max(10, _adornedElement.Height - e.Vector.Y);

        // Adjust Canvas position when dragging top/left edges
        double currentLeft = Canvas.GetLeft(_adornedElement);
        double currentTop = Canvas.GetTop(_adornedElement);

        Canvas.SetLeft(_adornedElement, currentLeft + (_adornedElement.Width - newWidth));
        Canvas.SetTop(_adornedElement, currentTop + (_adornedElement.Height - newHeight));

        _adornedElement.Width = newWidth;
        _adornedElement.Height = newHeight;
    }
}
 
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;

private ResizeAdorner? _currentAdorner;

private void OnRectanglePointerPressed(object? sender, PointerPressedEventArgs e)
{
    if (sender is Control targetShape)
    {
        var adornerLayer = AdornerLayer.GetAdornerLayer(targetShape);
        if (adornerLayer == null) return;

        // Remove existing adorner if switching selection
        if (_currentAdorner != null)
        {
            adornerLayer.Children.Remove(_currentAdorner);
        }

        // Create and attach the new adorner
        _currentAdorner = new ResizeAdorner(targetShape);
        AdornerLayer.SetAdornedElement(_currentAdorner, targetShape);
        adornerLayer.Children.Add(_currentAdorner);
    }
}

 * 
*/