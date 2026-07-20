namespace Lyt.PhotoPostPro.Workflow.Camera;

using static Avalonia.Controls.Utilities;

public partial class CameraFileView : View
{
    private static readonly SolidColorBrush insideBrush;
    private static readonly SolidColorBrush pressedBrush;
    private static readonly SolidColorBrush selectedBrush;

    private bool isSelected;
    private bool isInside;
    private bool isPressed;

    static CameraFileView()
    {
        insideBrush = FindResource<SolidColorBrush>("OrangePeel_0_100");
        pressedBrush = FindResource<SolidColorBrush>("OrangePeel_1_100");

        // FOR NOW 
        selectedBrush = new SolidColorBrush(Colors.Transparent);
            // FindResource<SolidColorBrush>("FreshGreen_0_080");
    }

    public CameraFileView() : base () 
    {
        this.PointerEntered += this.OnPointerEnter;
        this.PointerExited += this.OnPointerLeave;
        this.PointerPressed += this.OnPointerPressed;
        this.PointerReleased += this.OnPointerReleased;
        this.PointerMoved += this.OnPointerMoved;
        this.SetVisualState();
    }

    ~CameraFileView()
    {
        this.PointerEntered -= this.OnPointerEnter;
        this.PointerExited -= this.OnPointerLeave;
        this.PointerPressed -= this.OnPointerPressed;
        this.PointerReleased -= this.OnPointerReleased;
    }

    public void Select()
    {
        this.isPressed = false;
        this.isInside = false;
        this.isSelected = true;
        this.SetVisualState();
    }

    public void Deselect()
    {
        this.isPressed = false;
        this.isInside = false;
        this.isSelected = false;
        this.SetVisualState();
    }

    private void OnPointerEnter(object? sender, PointerEventArgs args)
    {
        if ((sender is CameraFileView view) && (this == view))
        {
            this.isInside = true;
            this.SetVisualState();
        }
    }

    private void OnPointerLeave(object? sender, PointerEventArgs args)
    {
        if ((sender is CameraFileView view) && (this == view))
        {
            this.isInside = false;
            this.SetVisualState();
        }
    }

    private void OnPointerMoved(object? sender, PointerEventArgs args)
    {
        if (!this.isInside)
        {
            return;
        }

        this.isInside = this.outerBorder.IsPointerInside(args);
        this.SetVisualState();
    }

    private void OnPointerPressed(object? sender, PointerEventArgs args)
    {
        if ((sender is CameraFileView view) && (this == view))
        {
            this.isPressed = true;
            this.SetVisualState();
        }
    }

    private void OnPointerReleased(object? sender, PointerEventArgs args)
    {
        bool wasInside = this.isInside;
        this.isPressed = false;
        if ((sender is CameraFileView view) && (this == view))
        {
            if (wasInside && this.DataContext is CameraThumbnailViewModel thumbnailViewModel)
            {
                this.isInside = false;
                this.isSelected = true;
                this.SetVisualState();
                thumbnailViewModel.OnSelect();
            }
        }
    }

    private void SetVisualState()
    {
        bool visible = this.isInside || this.isSelected;
        this.outerBorder.BorderThickness = new Thickness(visible ? 1.0 : 0.0);
        this.outerBorder.BorderBrush =
            this.isPressed ?
                pressedBrush :
                this.isSelected ? selectedBrush : insideBrush;
        this.outerBorder.Background =
            this.isSelected ? new SolidColorBrush(0x80_000000) : Brushes.Transparent;
    }
}