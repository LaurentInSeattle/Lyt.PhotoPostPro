namespace Lyt.PhotoPostPro.Controls;

public sealed partial class SelectorButtonViewModel(
    string buttonText, double buttonWidth, double buttonHeight,
    Action<object?> onSelect,
    object? tag = null) :
    ViewModel<SelectorButtonView>
{
    private readonly Action<object?> onSelect = onSelect;
    private readonly object? tag = tag;

    public bool IsSelected
    {
        get
        {
            if (!this.IsBound)
            {
                return false;
            }

            return this.View.IsSelected;
        }
    }

    [ObservableProperty]
    public partial string ButtonText { get; set; } = buttonText;

    [ObservableProperty]
    public partial double ButtonWidth { get; set; } = buttonWidth;

    [ObservableProperty]
    public partial double ButtonHeight { get; set; } = buttonHeight;

    [ObservableProperty]
    public partial GridLength RowHeight { get; set; } = new GridLength(buttonHeight - 4, GridUnitType.Pixel);

    [RelayCommand]
    public void OnSelect()
    {
        if (!this.IsBound)
        {
            return;
        }

        if (this.View.IsSelected)
        {
            return;
        }

        this.Select();
    }

    public void Select()
    {
        if (!this.IsBound)
        {
            return;
        }

        this.View.BringIntoView();
        this.View.OnSelect();
        this.onSelect(this.tag);

        var parentScrollViewer = this.View.FindAncestorOfType<ScrollViewer>(includeSelf: false);
        if (parentScrollViewer != null)
        {
            // found the parent ScrollViewer
            CenterItemInScrollViewer(parentScrollViewer, this.View);
        }
    }

    // TODO: Relocate this to some library 
    public static void CenterItemInScrollViewer(ScrollViewer scrollViewer, Control targetItem)
    {
        if (targetItem == null || scrollViewer == null)
        {
            return;
        }

        // Get position of the target item relative to the ScrollViewer
        var transform = targetItem.TransformToVisual(scrollViewer);
        if (transform == null)
        {
            return;
        }

        // Find the point of the item relative to the scroll viewer's client area
        var point = transform.Value.Transform(default);

        // Gather current offsets and viewport dimensions
        var offset = scrollViewer.Offset;
        double currentOffsetX = offset.X;
        double currentOffsetY = offset.Y;
        var viewport = scrollViewer.Viewport;
        double viewportWidth = viewport.Width;
        double viewportHeight = viewport.Height;
        var bounds = targetItem.Bounds;
        double itemWidth = bounds.Width;
        double itemHeight = bounds.Height;

        // Target new offsets to place the item in the middle
        double targetOffsetX = currentOffsetX + point.X - (viewportWidth / 2) + (itemWidth / 2);
        double targetOffsetY = currentOffsetY + point.Y - (viewportHeight / 2) + (itemHeight / 2);

        // Apply the offset (ScrollViewer clamps values automatically if out of bounds)
        scrollViewer.Offset = new Vector(targetOffsetX, targetOffsetY);
    }
}
