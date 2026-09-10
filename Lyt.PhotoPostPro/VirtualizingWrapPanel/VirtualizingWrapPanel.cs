using Avalonia.Labs.Controls.Utils;

namespace Avalonia.Labs.Controls;

/// <summary>
/// A implementation of a wrap panel that supports virtualization and can be used in horizontal and vertical orientation.
/// </summary>
public class VirtualizingWrapPanel : VirtualizingPanel, IScrollSnapPointsInfo, IItemSizeProvider
{
    // The fallback size in case size calculation went wrong
    private static readonly Size _FallbackItemSize = new Size(400, 120);
    private Size? _sizeOfFirstItem;
    private Size? _averageItemSizeCache;

    private int _startItemIndex = -1;
    private int _endItemIndex = -1;

    private double _startItemOffsetX;
    private double _startItemOffsetY;

    /// <summary> Gets an empty size </summary>
    private static readonly Size _EmptySize = new Size(0, 0);

    private const double EPSILON = 0.001;

    static VirtualizingWrapPanel()
    {
        AffectsMeasure<VirtualizingWrapPanel>(
            OrientationProperty,
            ItemSizeProperty,
            AllowDifferentSizedItemsProperty,
            ItemSizeProviderProperty);

        AffectsArrange<VirtualizingWrapPanel>(
            SpacingModeProperty,
            StretchItemsProperty,
            IsGridLayoutEnabledProperty);
    }

    /// <summary>
    /// Defines the <see cref="Orientation"/> property.
    /// </summary>
    public static readonly StyledProperty<Orientation> OrientationProperty =
        WrapPanel.OrientationProperty.AddOwner<VirtualizingWrapPanel>(
            new StyledPropertyMetadata<Orientation>(Orientation.Horizontal));

    /// <summary> Defines the <see cref="ItemSize"/> property. </summary>
    public static readonly StyledProperty<Size> ItemSizeProperty =
        AvaloniaProperty.Register<VirtualizingWrapPanel, Size>(nameof(ItemSize), _EmptySize);

    /// <summary>
    /// Defines the <see cref="AllowDifferentSizedItems"/> property.
    /// </summary>
    public static readonly StyledProperty<bool> AllowDifferentSizedItemsProperty =
        AvaloniaProperty.Register<VirtualizingWrapPanel, bool>(nameof(AllowDifferentSizedItems));

    /// <summary>
    /// Defines the <see cref="ItemSizeProvider"/> property.
    /// </summary>
    public static readonly StyledProperty<IItemSizeProvider?> ItemSizeProviderProperty =
        AvaloniaProperty.Register<VirtualizingWrapPanel, IItemSizeProvider?>(nameof(ItemSizeProvider));

    /// <summary>
    /// /// Defines the <see cref="SpacingMode"/> property.
    /// </summary>
    public static readonly StyledProperty<SpacingMode> SpacingModeProperty =
        AvaloniaProperty.Register<VirtualizingWrapPanel, SpacingMode>(nameof(SpacingMode), SpacingMode.Uniform);

    /// <summary>
    /// Defines the <see cref="StretchItems"/> property.
    /// </summary>
    public static readonly StyledProperty<bool> StretchItemsProperty =
        AvaloniaProperty.Register<VirtualizingWrapPanel, bool>(nameof(StretchItems));

    /// <summary>
    /// Defines the <see cref="IsGridLayoutEnabled"/> property.
    /// </summary>
    public static readonly StyledProperty<bool> IsGridLayoutEnabledProperty =
        AvaloniaProperty.Register<VirtualizingWrapPanel, bool>(nameof(IsGridLayoutEnabled), true);


    /// <summary>
    /// Defines the <see cref="CacheRows"/> property.
    /// </summary>
    public static readonly StyledProperty<int> CacheRowsProperty =
        AvaloniaProperty.Register<VirtualizingWrapPanel, int>(nameof(CacheRows), 2);

    /// <summary>
    /// Defines the RecycleKey attached property.
    /// </summary>
    private static readonly AttachedProperty<object?> _RecycleKeyProperty =
        AvaloniaProperty.RegisterAttached<VirtualizingWrapPanel, Control, object?>("RecycleKey");

    private static readonly object s_itemIsItsOwnContainer = new object();
    private readonly Action<Control, int> _recycleElement;
    private readonly Action<Control> _recycleElementOnItemRemoved;
    private readonly Action<Control, int, int> _updateElementIndex;
    private int _scrollToIndex = -1;
    private Control? _scrollToElement;
    private bool _isInLayout;
    private bool _isWaitingForViewportUpdate;
    private RealizedWrapElements? _measureElements;
    private RealizedWrapElements? _realizedElements;
    private IScrollAnchorProvider? _scrollAnchorProvider;
    private Rect _viewport;
    private Dictionary<object, Stack<Control>>? _recyclePool;
    private Control? _focusedElement;

    /// <summary>
    /// Stores information about a row of items.
    /// </summary>
    private sealed class RowInfo
    {
        public int StartIndex;
        public double Y;
        public double Height;
        public int Count;
        public double SummedUpChildWidth;
    }

    private readonly List<RowInfo> _rowCache = new();
    private const int RowCacheCapacity = 256; // "some rows" to improve performance without excessive memory
    private const int RecyclePoolMaxSize = 32; // max containers kept per recycle key to bound visual-tree size
    private double _lastLayoutWidth;
    private int _focusedIndex = -1;
    private double? _navigationAnchor;
    private int _lastNavigationIndex = -1;
    protected int LastNavigationIndex => this._lastNavigationIndex;

    private void ClearRowCache()
    {
        this._rowCache.Clear();
    }

    private void AddRowCacheEntry(int startIndex, double y, double height, int count, double summedUpChildWidth)
    {
        if (count <= 0)
        {
            return;
        }

        int index = -1;
        // Optimization: check last entry first as it's the most common case during forward realization
        if (this._rowCache.Count > 0)
        {
            var last = this._rowCache[this._rowCache.Count - 1];
            if (last.StartIndex == startIndex)
            {
                index = this._rowCache.Count - 1;
            }
            else if (last.StartIndex < startIndex)
            {
                // New entry after the last one, will be handled by the Add at the end
            }
            else
            {
                // Out of order or scrolling up, find insertion point or existing entry using binary search
                int lo = 0, hi = this._rowCache.Count - 1;
                while (lo <= hi)
                {
                    int mid = (lo + hi) >> 1;
                    if (this._rowCache[mid].StartIndex == startIndex)
                    {
                        index = mid;
                        break;
                    }
                    if (this._rowCache[mid].StartIndex < startIndex)
                    {
                        lo = mid + 1;
                    }
                    else
                    {
                        hi = mid - 1;
                    }
                }

                if (index < 0)
                {
                    // Insertion point is at 'lo'
                    index = lo;
                    this._rowCache.Insert(index, new RowInfo { StartIndex = startIndex, Y = y, Height = height, Count = count, SummedUpChildWidth = summedUpChildWidth });
                    if (index + 1 < this._rowCache.Count)
                    {
                        this._rowCache.RemoveRange(index + 1, this._rowCache.Count - (index + 1));
                    }
                    goto Trim;
                }
            }
        }

        if (index >= 0)
        {
            var existing = this._rowCache[index];
            // If the row info changed, we must invalidate all subsequent rows in the cache
            // as their Y position depends on this row.
            if (!existing.Y.IsCloseTo(y) || !existing.Height.IsCloseTo(height) || existing.Count != count || !existing.SummedUpChildWidth.IsCloseTo(summedUpChildWidth))
            {
                if (index + 1 < this._rowCache.Count)
                {
                    this._rowCache.RemoveRange(index + 1, this._rowCache.Count - (index + 1));
                }
                existing.Y = y;
                existing.Height = height;
                existing.Count = count;
                existing.SummedUpChildWidth = summedUpChildWidth;
            }
        }
        else
        {
            this._rowCache.Add(new RowInfo { StartIndex = startIndex, Y = y, Height = height, Count = count, SummedUpChildWidth = summedUpChildWidth });
            index = this._rowCache.Count - 1;
        }

    Trim:
        if (this._rowCache.Count > RowCacheCapacity)
        {
            // If we are adding/updating near the start, trim from the end.
            // Otherwise trim from the start.
            if (index < this._rowCache.Count / 2)
            {
                this._rowCache.RemoveAt(this._rowCache.Count - 1);
            }
            else
            {
                this._rowCache.RemoveAt(0);
            }
        }
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="VirtualizingWrapPanel"/> class.
    /// </summary>
    public VirtualizingWrapPanel()
    {
        this._recycleElement = this.RecycleElement;
        this._recycleElementOnItemRemoved = this.RecycleElementOnItemRemoved;
        this._updateElementIndex = this.UpdateElementIndex;
        EffectiveViewportChanged += this.OnEffectiveViewportChanged;
    }

    /// <summary>
    /// Gets or sets a value that specifies the orientation in which items are arranged before wrapping.
    /// The default value is <see cref="Orientation.Horizontal"/>.
    /// </summary>
    public Orientation Orientation
    {
        get => this.GetValue(OrientationProperty);
        set => this.SetValue(OrientationProperty, value);
    }

    /// <summary>
    /// Gets or sets a value that specifies the size of the items. The default value is <see cref="_EmptySize"/>.
    /// If the value is <see cref="_EmptySize"/> the item size is determined by measuring the first realized item.
    /// </summary>
    public Size ItemSize
    {
        get => this.GetValue(ItemSizeProperty);
        set => this.SetValue(ItemSizeProperty, value);
    }

    /// <summary>
    /// Specifies whether items can have different sizes. The default value is false. If this property is enabled,
    /// it is strongly recommended to also set the <see cref="ItemSizeProvider"/> property. Otherwise, the position
    /// of the items is not always guaranteed to be correct.
    /// </summary>
    public bool AllowDifferentSizedItems
    {
        get => this.GetValue(AllowDifferentSizedItemsProperty);
        set => this.SetValue(AllowDifferentSizedItemsProperty, value);
    }

    /// <summary>
    /// Specifies an instance of <see cref="IItemSizeProvider"/> which provides the size of the items. In order to allow
    /// different sized items, also enable the <see cref="AllowDifferentSizedItems"/> property.
    /// </summary>
    public IItemSizeProvider? ItemSizeProvider
    {
        get => this.GetValue(ItemSizeProviderProperty);
        set => this.SetValue(ItemSizeProviderProperty, value);
    }

    /// <summary>
    /// Gets or sets the spacing mode used when arranging the items. The default value is <see cref="SpacingMode.Uniform"/>.
    /// </summary>
    public SpacingMode SpacingMode
    {
        get => this.GetValue(SpacingModeProperty);
        set => this.SetValue(SpacingModeProperty, value);
    }

    /// <summary>
    /// Gets or sets a value that specifies if the items get stretched to fill up remaining space. The default value is false.
    /// </summary>
    /// <remarks>
    /// The MaxWidth and MaxHeight properties of the ItemContainerStyle can be used to limit the stretching.
    /// In this case the use of the remaining space will be determined by the SpacingMode property.
    /// </remarks>
    public bool StretchItems
    {
        get => this.GetValue(StretchItemsProperty);
        set => this.SetValue(StretchItemsProperty, value);
    }

    /// <summary>
    /// Specifies whether the items are arranged in a grid-like layout. The default value is <c>true</c>.
    /// When set to <c>true</c>, the items are arranged based on the number of items that can fit in a row.
    /// When set to <c>false</c>, the items are arranged based on the number of items that are actually placed in the row.
    /// </summary>
    /// <remarks>
    /// If <see cref="AllowDifferentSizedItems"/> is enabled, this property has no effect and the items are always
    /// arranged based on the number of items that are actually placed in the row.
    /// </remarks>
    public bool IsGridLayoutEnabled
    {
        get => this.GetValue(IsGridLayoutEnabledProperty);
        set => this.SetValue(IsGridLayoutEnabledProperty, value);
    }

    /// <summary>
    /// Number of rows to keep cached above and below the visible viewport.
    /// Increasing this can reduce layout recalculations while scrolling at the cost of extra realized work.
    /// </summary>
    public int CacheRows
    {
        get => this.GetValue(CacheRowsProperty);
        set => this.SetValue(CacheRowsProperty, value);
    }

    /// <summary>
    /// Gets the index of the first realized element, or -1 if no elements are realized.
    /// </summary>
    public int FirstRealizedIndex => this._realizedElements?.FirstIndex ?? -1;

    /// <summary>
    /// Gets the index of the last realized element, or -1 if no elements are realized.
    /// </summary>
    public int LastRealizedIndex => this._realizedElements?.LastIndex ?? -1;


    /// <inheritdoc />
    protected override Size MeasureOverride(Size availableSize)
    {
        var items = this.Items;

        if (items.Count == 0)
        {
            return default;
        }

        var orientation = this.Orientation;

        double wrappingWidth = this.GetWidth(availableSize);
        if (double.IsInfinity(wrappingWidth))
        {
            wrappingWidth = this.GetWidth(this._viewport.Size);
        }

        if (wrappingWidth <= 0)
        {
            wrappingWidth = this._lastLayoutWidth;
        }

        if (wrappingWidth <= 0)
        {
            wrappingWidth = this.GetWidth(this.Bounds.Size);
        }

        if (wrappingWidth <= 0)
        {
            wrappingWidth = this.GetWidth(this.DesiredSize);
        }

        if (wrappingWidth <= 0)
        {
            wrappingWidth = _FallbackItemSize.Width * 10;
        }

        // If we're bringing an item into view, ignore any layout passes until we receive a new
        // effective viewport.
        if (this._isWaitingForViewportUpdate)
        {
            return this.EstimateDesiredSize(orientation, items.Count, wrappingWidth);
        }

        this._isInLayout = true;

        try
        {
            // _realizedElements?.ValidateStartU(Orientation);
            this._realizedElements ??= new();
            this._measureElements ??= new();

            // If the viewport is disjunct then we can recycle everything
            bool disjunct = this._startItemIndex < this._realizedElements.FirstIndex
                           || this._startItemIndex > this._realizedElements.LastIndex;

            if (disjunct)
            {
                this._realizedElements.RecycleAllElements(this._recycleElement);
            }

            // Do the measure, creating/recycling elements as necessary to fill the viewport. Don't
            // write to _realizedElements yet, only _measureElements.
            this.RealizeAndVirtualizeItems();

            // Now swap the measureElements and realizedElements collection.
            (this._measureElements, this._realizedElements) = (this._realizedElements, this._measureElements);
            this._measureElements.ResetForReuse();

            // If there is a focused element is outside the visible viewport (i.e.
            // _focusedElement is non-null), ensure it's measured.
            this._focusedElement?.Measure(availableSize);

            return this.CalculateDesiredSize(orientation, items.Count, wrappingWidth);
        }
        finally
        {
            this._isInLayout = false;
        }
    }

    private readonly List<Control> _rowChildrenReuse = new();
    private readonly List<Size> _childSizesReuse = new();
    private readonly List<(int index, double y)> _previousRowsReuse = new();

    /// <inheritdoc />
    protected override Size ArrangeOverride(Size finalSize)
    {
        if (this._realizedElements is null)
        {
            return default;
        }

        this._isInLayout = true;

        try
        {
            if (this._startItemIndex == -1)
            {
                return finalSize;
            }

            if (this._realizedElements.Count < this._endItemIndex - this._startItemIndex + 1)
            {
                return finalSize;
            }

            double x = this._startItemOffsetX; // + GetX(_viewport.TopLeft);
            double y = this._startItemOffsetY; // - GetY(_viewport.TopLeft);
            double rowHeight = 0;
            double arrangedRowHeight = 0; // max height of only realized (non-null) children
            double finalWidth = this.GetWidth(finalSize);
            var items = this.Items;

            this._rowChildrenReuse.Clear();
            this._childSizesReuse.Clear();
            double summedUpChildWidth = 0;

            for (int i = this._startItemIndex; i <= this._endItemIndex; i++)
            {
                object? item = items[i];
                var child = this._realizedElements.GetElement(i);

                Size? upfrontKnownItemSize = this.GetUpfrontKnownItemSize(item);

                Size childSize = upfrontKnownItemSize ??
                                 this._realizedElements.GetElementSize(child) ?? _FallbackItemSize;

                if (this._rowChildrenReuse.Count > 0 && x + this.GetWidth(childSize) > finalWidth)
                {
                    this.ArrangeRow(finalWidth, this._rowChildrenReuse, this._childSizesReuse, y, summedUpChildWidth, arrangedRowHeight);
                    x = 0;
                    y += rowHeight;
                    rowHeight = 0;
                    arrangedRowHeight = 0;
                    this._rowChildrenReuse.Clear();
                    this._childSizesReuse.Clear();
                    summedUpChildWidth = 0;
                }

                x += this.GetWidth(childSize);
                rowHeight = Math.Max(rowHeight, this.GetHeight(childSize));
                if (child != null)
                {
                    this._rowChildrenReuse.Add(child);
                    this._childSizesReuse.Add(childSize);
                    summedUpChildWidth += this.GetWidth(childSize);
                    arrangedRowHeight = Math.Max(arrangedRowHeight, this.GetHeight(childSize));

                    this._scrollAnchorProvider?.RegisterAnchorCandidate(child);
                }
            }

            if (this._rowChildrenReuse.Count > 0)
            {
                this.ArrangeRow(finalWidth, this._rowChildrenReuse, this._childSizesReuse, y, summedUpChildWidth, arrangedRowHeight);
            }

            // Ensure that the focused element is in the correct position.
            if (this._focusedElement is not null && this._focusedIndex >= 0)
            {
                var startPoint = this.FindItemOffset(this._focusedIndex, finalWidth);

                double focusedOffsetX = this.GetX(startPoint);
                double focusedOffsetY = this.GetY(startPoint);

                var rect = this.Orientation == Orientation.Horizontal ?
                    new Rect(focusedOffsetX, focusedOffsetY, this._focusedElement.DesiredSize.Width, this._focusedElement.DesiredSize.Height) :
                    new Rect(focusedOffsetY, focusedOffsetX, this._focusedElement.DesiredSize.Width, this._focusedElement.DesiredSize.Height);
                this._focusedElement.Arrange(rect);
            }

            // Ensure that the scrollTo element is in the correct position.
            if (this._scrollToElement is not null && this._scrollToIndex >= 0)
            {
                var startPoint = this.FindItemOffset(this._scrollToIndex, finalWidth);

                double scrollToOffsetX = this.GetX(startPoint);
                double scrollToOffsetY = this.GetY(startPoint);

                var rect = this.Orientation == Orientation.Horizontal ?
                    new Rect(scrollToOffsetX, scrollToOffsetY, this._scrollToElement.DesiredSize.Width,
                        finalSize.Height) :
                    new Rect(scrollToOffsetY, scrollToOffsetX, finalSize.Width,
                        this._scrollToElement.DesiredSize.Height);
                this._scrollToElement.Arrange(rect);
            }

            return finalSize;
        }
        finally
        {
            this._isInLayout = false;

            this.RaiseEvent(new RoutedEventArgs(this.Orientation == Orientation.Horizontal ?
                HorizontalSnapPointsChangedEvent :
                VerticalSnapPointsChangedEvent));
        }
    }

    /// <inheritdoc />
    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        this._scrollAnchorProvider = this.FindAncestorOfType<IScrollAnchorProvider>();
    }

    /// <inheritdoc />
    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        this._scrollAnchorProvider = null;
    }

    /// <inheritdoc />
    protected override void OnItemsChanged(IReadOnlyList<object?> items, NotifyCollectionChangedEventArgs e)
    {
        this._averageItemSizeCache = null;
        this.ClearRowCache();
        this.InvalidateMeasure();

        if (this._realizedElements is null)
        {
            return;
        }

        switch (e.Action)
        {
            case NotifyCollectionChangedAction.Add:
                if (e.NewItems is not null)
                {
                    this._realizedElements.ItemsInserted(e.NewStartingIndex, e.NewItems.Count, this._updateElementIndex);
                }
                break;

            case NotifyCollectionChangedAction.Remove:
                if (e.OldItems is not null)
                {
                    this._realizedElements.ItemsRemoved(e.OldStartingIndex, e.OldItems.Count, this._updateElementIndex, this._recycleElementOnItemRemoved);
                }
                break;

            case NotifyCollectionChangedAction.Replace:
                if (e.OldItems is not null)
                {
                    this._realizedElements.ItemsReplaced(e.OldStartingIndex, e.OldItems.Count, this._recycleElementOnItemRemoved);
                }
                break;

            case NotifyCollectionChangedAction.Move:
                if (e.OldItems is not null)
                {
                    this._realizedElements.ItemsRemoved(e.OldStartingIndex, e.OldItems.Count, this._updateElementIndex, this._recycleElementOnItemRemoved);
                }

                if (e.NewItems is not null)
                {
                    this._realizedElements.ItemsInserted(e.NewStartingIndex, e.NewItems.Count, this._updateElementIndex);
                }
                break;

            case NotifyCollectionChangedAction.Reset:
                this._realizedElements.ItemsReset(this._recycleElementOnItemRemoved);
                break;
        }
    }

    /// <inheritdoc />
    protected override void OnItemsControlChanged(ItemsControl? oldValue)
    {
        base.OnItemsControlChanged(oldValue);

        if (oldValue is not null)
        {
            oldValue.PropertyChanged -= this.OnItemsControlPropertyChanged;
        }

        if (this.ItemsControl is not null)
        {
            this.ItemsControl.PropertyChanged += this.OnItemsControlPropertyChanged;
        }
    }

    /// <inheritdoc />
    protected override IInputElement? GetControl(NavigationDirection direction, IInputElement? from, bool wrap)
    {
        int count = this.Items.Count;
        var fromControl = from as Control;

        if (count == 0 ||
            (fromControl is null && direction is not NavigationDirection.First and not NavigationDirection.Last))
        {
            return null;
        }

        int fromIndex = fromControl != null ? this.IndexFromContainer(fromControl) : -1;

        if (fromIndex == -1 && direction is not NavigationDirection.First and not NavigationDirection.Last)
        {
            return null;
        }

        int toIndex = fromIndex;

        if (fromIndex != this._lastNavigationIndex)
        {
            this._navigationAnchor = null;
        }

        // Reset or update navigation anchor
        switch (direction)
        {
            case NavigationDirection.Up:
            case NavigationDirection.Down:
                if (this.Orientation == Orientation.Vertical)
                {
                    this._navigationAnchor = null;
                }

                break;
            case NavigationDirection.Left:
            case NavigationDirection.Right:
                if (this.Orientation == Orientation.Horizontal)
                {
                    this._navigationAnchor = null;
                }

                break;
            default:
                this._navigationAnchor = null;
                break;
        }

        switch (direction)
        {
            case NavigationDirection.First:
                toIndex = 0;
                break;
            case NavigationDirection.Last:
                toIndex = count - 1;
                break;
            case NavigationDirection.Next:
                this.NavigateRight(ref toIndex);
                break;
            case NavigationDirection.Previous:
                this.NavigateLeft(ref toIndex);
                break;
            case NavigationDirection.Left:
                this.NavigateLeft(ref toIndex);
                break;
            case NavigationDirection.Right:
                this.NavigateRight(ref toIndex);
                break;
            case NavigationDirection.Up:
                this.NavigateUp(ref toIndex);
                break;
            case NavigationDirection.Down:
                this.NavigateDown(ref toIndex);
                break;
            default:
                return null;
        }

        if (fromIndex == toIndex)
        {
            this._lastNavigationIndex = toIndex;
            return from;
        }

        if (wrap)
        {
            if (toIndex < 0)
            {
                toIndex = count - 1;
            }
            else if (toIndex >= count)
            {
                toIndex = 0;
            }
        }
        else
        {
            if (toIndex < 0)
            {
                toIndex = 0;
            }
            else if (toIndex >= count)
            {
                toIndex = count - 1;
            }
        }

        this._lastNavigationIndex = toIndex;
        return this.ScrollIntoView(toIndex);
    }

    /// <inheritdoc />
    protected override IEnumerable<Control>? GetRealizedContainers()
    {
        if (this._realizedElements is null)
        {
            return null;
        }

        var elements = this._realizedElements.Elements;
        int count = elements.Count;
        var result = new List<Control>(count);
        for (int i = 0; i < count; i++)
        {
            if (elements[i] is { } element)
            {
                result.Add(element);
            }
        }

        return result;
    }

    /// <inheritdoc />
    protected override Control? ContainerFromIndex(int index)
    {
        if (index < 0 || index >= this.Items.Count)
        {
            return null;
        }

        if (this._scrollToIndex == index)
        {
            return this._scrollToElement;
        }

        if (this._focusedIndex == index)
        {
            return this._focusedElement;
        }

        if (this.GetRealizedElement(index) is { } realized)
        {
            return realized;
        }

        if (this.Items[index] is Control c && ReferenceEquals(c.GetValue(_RecycleKeyProperty), s_itemIsItsOwnContainer))
        {
            return c;
        }

        return null;
    }

    /// <inheritdoc />
    protected override int IndexFromContainer(Control container)
    {
        if (ReferenceEquals(container, this._scrollToElement))
        {
            return this._scrollToIndex;
        }

        if (ReferenceEquals(container, this._focusedElement))
        {
            return this._focusedIndex;
        }

        return this._realizedElements?.GetIndex(container) ?? -1;
    }

    private Rect GetExpectedItemRect(int index, double wrappingWidth)
    {
        var items = this.Items;
        if (index < 0 || index >= items.Count)
        {
            return default;
        }

        var start = this.FindItemOffset(index, wrappingWidth);
        var itemSize = this.GetAssumedItemSize(index, items[index]);
        double width = this.GetWidth(itemSize);
        double height = this.GetHeight(itemSize);

        // Find extra width from stretching if applicable
        if (this.StretchItems)
        {
            // Find start of row
            double y = this.GetY(start);
            int rowStartIndex = index;
            while (rowStartIndex > 0 && this.GetY(this.FindItemOffset(rowStartIndex - 1, wrappingWidth)).IsCloseTo(y))
            {
                rowStartIndex--;
            }

            // Find row info
            double rowSummedUpWidth = 0;
            int rowCount = 0;
            int k = rowStartIndex;
            while (k < items.Count && this.GetY(this.FindItemOffset(k, wrappingWidth)).IsCloseTo(y))
            {
                rowSummedUpWidth += this.GetWidth(this.GetAssumedItemSize(k, items[k]));
                rowCount++;
                k++;
            }

            this.GetRowLayout(wrappingWidth, rowCount, rowSummedUpWidth, out double innerSpacing, out _, out double extraWidth);
            width += extraWidth;

            // If it's not the last item in the row, add innerSpacing to ensure the spacing is also visible
            if (index < rowStartIndex + rowCount - 1)
            {
                width += innerSpacing;
            }
        }

        return this.CreateRect(this.GetX(start), this.GetY(start), width, height);
    }

    /// <inheritdoc />
    protected override Control? ScrollIntoView(int index)
    {
        var items = this.Items;

        if (this._isInLayout || index < 0 || index >= items.Count || this._realizedElements is null || !this.IsEffectivelyVisible)
        {
            return null;
        }

        double wrappingWidth = this.GetWrappingWidth();

        if (TopLevel.GetTopLevel(this) is not { } root)
        {
            return null;
        }

        var element = this.GetRealizedElement(index);

        if (element is not null)
        {
            var rect = this.GetExpectedItemRect(index, wrappingWidth);

            if (!this._viewport.Contains(rect))
            {
                this._isWaitingForViewportUpdate = true;
                this.InvalidateMeasure();
                root.UpdateLayout();
                this._isWaitingForViewportUpdate = false;
            }

            element.BringIntoView();
            return element;
        }
        else
        {
            // Create and measure the element to be brought into view. Store it in a field so that
            // it can be re-used in the layout pass.
            var scrollToElement = this.GetOrCreateElement(items, index);
            scrollToElement.Measure(Size.Infinity);

            // Get the expected position of the element and put it in place.
            var rect = this.GetExpectedItemRect(index, wrappingWidth);
            scrollToElement.Arrange(rect);

            // Store the element and index so that they can be used in the layout pass.
            this._scrollToElement = scrollToElement;
            this._scrollToIndex = index;

            // If the item being brought into view was added since the last layout pass then
            // our bounds won't be updated, so any containing scroll viewers will not have an
            // updated extent. Do a layout pass to ensure that the containing scroll viewers
            // will be able to scroll the new item into view.
            if (!this.Bounds.Contains(rect) && !this._viewport.Contains(rect))
            {
                this._isWaitingForViewportUpdate = true;
                root.UpdateLayout();
                this._isWaitingForViewportUpdate = false;
            }

            // Try to bring the item into view.
            scrollToElement.BringIntoView();

            // If the viewport does not contain the item to scroll to, set _isWaitingForViewportUpdate:
            // this should cause the following chain of events:
            // - Measure is first done with the old viewport (which will be a no-op, see MeasureOverride)
            // - The viewport is then updated by the layout system which invalidates our measure
            // - Measure is then done with the new viewport.
            this._isWaitingForViewportUpdate = !this._viewport.Contains(rect);
            root.UpdateLayout();

            // If for some reason the layout system didn't give us a new viewport during the layout, we
            // need to do another layout pass as the one that took place was a no-op.
            if (this._isWaitingForViewportUpdate)
            {
                this._isWaitingForViewportUpdate = false;
                this.InvalidateMeasure();
                root.UpdateLayout();
            }

            // During the previous BringIntoView, the scroll width extent might have been out of date if
            // elements have different widths. Because of that, the ScrollViewer might not scroll to the correct offset.
            // After the previous BringIntoView, Y offset should be correct and an extra layout pass has been executed,
            // hence the width extent should be correct now, and we can try to scroll again.
            scrollToElement.BringIntoView();

            if (this._scrollToElement is not null)
            {
                this.RecycleElement(this._scrollToElement, this._scrollToIndex);
            }

            this._scrollToElement = null;
            this._scrollToIndex = -1;
            return scrollToElement;
        }
    }

    private double GetWrappingWidth()
    {
        double width = this.GetWidth(this._viewport.Size);
        if (width <= 0)
        {
            width = this._lastLayoutWidth;
        }

        if (width <= 0)
        {
            width = this.GetWidth(this.Bounds.Size);
        }

        if (width <= 0)
        {
            width = this.GetWidth(this.DesiredSize);
        }

        if (width <= 0)
        {
            width = _FallbackItemSize.Width * 10;
        }

        return width;
    }

    /// <summary>
    /// Calculates the desired size of the viewport.
    /// </summary>
    /// <param name="orientation">the <see cref="Orientation"/> to use</param>
    /// <param name="itemCount">The number of items</param>
    /// <param name="wrappingWidth">the width used for wrapping</param>
    /// <returns>the desired size</returns>
    private Size CalculateDesiredSize(Orientation orientation, int itemCount, double wrappingWidth)
    {
        if (itemCount == 0)
        {
            return _EmptySize;
        }

        var averageItemSize = this.GetAverageItemSize();

        double itemWidth = this.GetWidth(averageItemSize);
        double itemHeight = this.GetHeight(averageItemSize);

        if (itemWidth == 0 || itemHeight == 0)
        {
            return _EmptySize;
        }

        double itemsPerRow = Math.Max(Math.Floor((wrappingWidth + EPSILON) / itemWidth), 1);

        double sizeU = 0d;
        if (this.AllowDifferentSizedItems)
        {
            // If we have a partially populated row cache, we can use it to estimate the rest
            if (this._rowCache.Count > 0)
            {
                var lastRow = this._rowCache[this._rowCache.Count - 1];
                int startIndex = lastRow.StartIndex + lastRow.Count;
                int remainingItems = itemCount - startIndex;

                if (remainingItems <= 0)
                {
                    sizeU = lastRow.Y + lastRow.Height;
                }
                else
                {
                    if (this.ItemSizeProvider is not null)
                    {
                        double x = 0;
                        double rowHeight = 0;
                        double y = lastRow.Y + lastRow.Height;

                        for (int i = startIndex; i < itemCount; i++)
                        {
                            object? item = this.Items[i];
                            Size itemSize = this.GetAssumedItemSize(i, item);

                            if (x != 0 && x + this.GetWidth(itemSize) > wrappingWidth)
                            {
                                x = 0;
                                y += rowHeight;
                                rowHeight = 0;
                            }

                            x += this.GetWidth(itemSize);
                            rowHeight = Math.Max(rowHeight, this.GetHeight(itemSize));
                        }
                        sizeU = y + rowHeight;
                    }
                    else
                    {
                        // Estimate remaining rows
                        double remainingRows = Math.Ceiling(remainingItems / itemsPerRow);
                        sizeU = lastRow.Y + lastRow.Height + (remainingRows * itemHeight);
                    }
                }
            }
            else
            {
                // No row cache and no ItemSizeProvider: use average-based estimation to avoid
                // an O(N) full-scan on every measure pass. This matches the non-different-sizes
                // path and is acceptable because GetAssumedItemSize already returns the average
                // for unrealized items in this scenario.
                sizeU = Math.Ceiling(itemCount / itemsPerRow) * itemHeight;
            }
        }
        else
        {
            sizeU = Math.Ceiling(itemCount / itemsPerRow) * itemHeight;
        }

        return orientation == Orientation.Horizontal ?
            new Size(wrappingWidth, sizeU) :
            new Size(sizeU, wrappingWidth);
    }

    /// <summary>
    /// Estimates the desired size
    /// </summary>
    /// <param name="orientation">the <see cref="Orientation"/> to use</param>
    /// <param name="itemCount">The number of items</param>
    /// <param name="wrappingWidth">the width used for wrapping</param>
    /// <returns>the estimated desired size</returns>
    private Size EstimateDesiredSize(Orientation orientation, int itemCount, double wrappingWidth)
    {
        if (this._scrollToIndex >= 0 && this._scrollToElement is not null)
        {
            // We have an element to scroll to, so we can estimate the desired size based on the
            // element's position and the remaining elements.
            int remainingItems = itemCount - this._scrollToIndex - 1;

            if (remainingItems <= 0)
            {
                double u = this.GetY(this._scrollToElement.Bounds.BottomRight);
                return orientation == Orientation.Horizontal ?
                    new(wrappingWidth, u) :
                    new(u, wrappingWidth);
            }

            double sizeU;

            if (this.AllowDifferentSizedItems && this.ItemSizeProvider is not null)
            {
                double x = 0;
                double rowHeight = 0;
                double y = 0;

                // Find start of row for _scrollToIndex
                var start = this.FindItemOffset(this._scrollToIndex, wrappingWidth);
                x = this.GetX(start);
                y = this.GetY(start);

                for (int i = this._scrollToIndex; i < itemCount; i++)
                {
                    Size itemSize = (i == this._scrollToIndex) ?
                        new Size(this.GetWidth(this._scrollToElement.Bounds.Size), this.GetHeight(this._scrollToElement.Bounds.Size)) :
                        this.GetAssumedItemSize(i, this.Items[i]);

                    if (x != 0 && x + this.GetWidth(itemSize) > wrappingWidth)
                    {
                        x = 0;
                        y += rowHeight;
                        rowHeight = 0;
                    }

                    x += this.GetWidth(itemSize);
                    rowHeight = Math.Max(rowHeight, this.GetHeight(itemSize));
                }
                sizeU = y + rowHeight;
            }
            else
            {
                var avgSize = this.GetAverageItemSize();
                double avgWidth = this.GetWidth(avgSize);
                double itemsPerRow = Math.Max(Math.Floor((wrappingWidth + EPSILON) / avgWidth), 1);
                int remainingRows = (int)Math.Ceiling(remainingItems / itemsPerRow);
                double u = this.GetY(this._scrollToElement.Bounds.BottomRight);
                sizeU = u + (remainingRows * this.GetHeight(avgSize));
            }

            return orientation == Orientation.Horizontal ?
                new(wrappingWidth, sizeU) :
                new(sizeU, wrappingWidth);
        }

        return this.DesiredSize;
    }

    /// <inheritdoc />
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {

        if (change.Property == OrientationProperty)
        {
            this.ClearRowCache();
            this.InvalidateMeasure();
            this.InvalidateArrange();
            // Defer ScrollIntoView until after the layout triggered above has completed.
            // Calling it synchronously here risks reentrancy because OnPropertyChanged
            // fires before Measure/Arrange, and ScrollIntoView itself calls UpdateLayout.
            Dispatcher.UIThread.Post(() => this.ScrollIntoView(0), DispatcherPriority.Background);
        }

        if (change.Property == AllowDifferentSizedItemsProperty || change.Property == ItemSizeProperty ||
            change.Property == IsGridLayoutEnabledProperty || change.Property == StretchItemsProperty ||
            change.Property == CacheRowsProperty)
        {
            foreach (var child in this.Children)
            {
                child.InvalidateMeasure();
            }

            this.ClearRowCache();
            this.InvalidateMeasure();
            this.InvalidateArrange();
        }


        base.OnPropertyChanged(change);
    }

    /// <summary>
    /// Realizes visible items and virtualizes non-visible items
    /// </summary>
    private void RealizeAndVirtualizeItems()
    {
        this.FindStartIndexAndOffset();
        this.VirtualizeItemsBeforeStartIndex();
        this.RealizeItemsAndFindEndIndex();
        this.VirtualizeItemsAfterEndIndex();
    }

    /// <summary>
    /// Calculates the predicted average item size
    /// </summary>
    /// <returns>the estimated average Size</returns>
    private Size GetAverageItemSize()
    {
        if (!this.ItemSize.NearlyEquals(_EmptySize))
        {
            return this.ItemSize;
        }
        else if (!this.AllowDifferentSizedItems)
        {
            return this._sizeOfFirstItem ?? _FallbackItemSize;
        }
        else
        {
            return this._averageItemSizeCache ??= this.CalculateAverageItemSize();
        }
    }

    /// <summary>
    /// Calculates the start offset for a given item index
    /// </summary>
    /// <param name="itemIndex">the index of the requested item</param>
    /// <param name="wrappingWidth">the width used for wrapping</param>
    /// <returns>the starting point</returns>
    private Point FindItemOffset(int itemIndex, double? wrappingWidth = null)
    {
        double x = 0, y = 0, rowHeight = 0;

        if (!this.AllowDifferentSizedItems && this.Items.Count > 0)
        {
            double itemWidth = this.GetWidth(this.GetAssumedItemSize(this.Items[0]));
            double itemHeight = this.GetHeight(this.GetAssumedItemSize(this.Items[0]));

            if (itemWidth == 0 || itemHeight == 0)
            {
                return new Point();
            }

            double viewportWidth = wrappingWidth ?? this.GetWidth(this._viewport.Size);
            if (viewportWidth <= 0)
            {
                viewportWidth = this._lastLayoutWidth;
            }

            if (viewportWidth <= 0)
            {
                viewportWidth = this.GetWidth(this.Bounds.Size);
            }

            if (viewportWidth <= 0)
            {
                viewportWidth = this.GetWidth(this.DesiredSize);
            }

            if (viewportWidth <= 0)
            {
                viewportWidth = _FallbackItemSize.Width * 10; // Extreme fallback
            }

            int itemsPerRow = (int)Math.Max(Math.Floor((viewportWidth + EPSILON) / itemWidth), 1);

            int itemRowIndex = (int)Math.Floor(itemIndex * 1.0 / itemsPerRow);
            y = itemRowIndex * itemHeight;

            this.GetRowLayout(viewportWidth, itemsPerRow, itemsPerRow * itemWidth, out double innerSpacing, out double outerSpacing, out double extraWidth);
            int indexInRow = itemIndex - itemRowIndex * itemsPerRow;
            x = outerSpacing + indexInRow * (itemWidth + extraWidth + innerSpacing);

            return this.CreatePoint(x, y);
        }

        double effectiveWrappingWidth = wrappingWidth ?? this.GetWidth(this._viewport.Size);
        if (effectiveWrappingWidth <= 0)
        {
            effectiveWrappingWidth = this._lastLayoutWidth;
        }

        if (effectiveWrappingWidth <= 0)
        {
            effectiveWrappingWidth = this.GetWidth(this.Bounds.Size);
        }

        if (effectiveWrappingWidth <= 0)
        {
            effectiveWrappingWidth = this.GetWidth(this.DesiredSize);
        }

        if (effectiveWrappingWidth <= 0)
        {
            effectiveWrappingWidth = _FallbackItemSize.Width * 10; // Extreme fallback
        }

        int startIndex = 0;

        // Try to use row cache to quickly jump to the correct row and then accumulate within the row
        if (this._rowCache.Count > 0)
        {
            int lo = 0, hi = this._rowCache.Count - 1, best = -1;
            while (lo <= hi)
            {
                int mid = (lo + hi) >> 1;
                var r = this._rowCache[mid];
                if (r.StartIndex <= itemIndex)
                {
                    best = mid;
                    lo = mid + 1;
                }
                else
                {
                    hi = mid - 1;
                }
            }

            if (best >= 0)
            {
                var row = this._rowCache[best];
                y = row.Y;
                rowHeight = row.Height;

                // If the item is within this row, we can calculate its X using row info
                if (itemIndex < row.StartIndex + row.Count)
                {
                    this.GetRowLayout(effectiveWrappingWidth, row.Count, row.SummedUpChildWidth, out double innerSpacing, out double outerSpacing, out double extraWidth);
                    x = outerSpacing;
                    for (int i = row.StartIndex; i < itemIndex; i++)
                    {
                        var size = this.GetAssumedItemSize(i, this.Items[i]);
                        x += this.GetWidth(size) + extraWidth + innerSpacing;
                    }
                    return this.CreatePoint(x, y);
                }
                else
                {
                    // Item is beyond this row. 
                    // If it's the last row in cache, it might be partial, so we start linear scan from its start.
                    if (best == this._rowCache.Count - 1)
                    {
                        startIndex = row.StartIndex;
                        y = row.Y;
                    }
                    else
                    {
                        y += rowHeight;
                        startIndex = row.StartIndex + row.Count;
                    }
                    rowHeight = 0;
                }
            }
        }

        // Fallback or continuation: linear accumulation
        {
            int currentRowStartIndex = startIndex;
            int currentRowCount = 0;
            double currentRowSummedUpWidth = 0;
            x = 0;

            for (int i = startIndex; i < this.Items.Count; i++)
            {
                Size itemSize = this.GetAssumedItemSize(i, this.Items[i]);
                double itemWidth = this.GetWidth(itemSize);

                if (currentRowCount > 0 && x + itemWidth > effectiveWrappingWidth + EPSILON)
                {
                    // Current row is finished. Check if target was in it.
                    if (itemIndex < i)
                    {
                        // Target was in the row we just finished.
                        this.GetRowLayout(effectiveWrappingWidth, currentRowCount, currentRowSummedUpWidth, out double innerSpacing, out double outerSpacing, out double extraWidth);
                        double finalX = outerSpacing;
                        for (int j = currentRowStartIndex; j < itemIndex; j++)
                        {
                            var s = this.GetAssumedItemSize(j, this.Items[j]);
                            finalX += this.GetWidth(s) + extraWidth + innerSpacing;
                        }
                        return this.CreatePoint(finalX, y);
                    }

                    x = 0;
                    y += rowHeight;
                    rowHeight = 0;
                    currentRowSummedUpWidth = 0;
                    currentRowStartIndex = i;
                    currentRowCount = 0;
                }

                x += itemWidth;
                currentRowSummedUpWidth += itemWidth;
                rowHeight = Math.Max(rowHeight, this.GetHeight(itemSize));
                currentRowCount++;

                if (i == itemIndex && i == this.Items.Count - 1)
                {
                    // It's the last item and it's our target.
                    this.GetRowLayout(effectiveWrappingWidth, currentRowCount, currentRowSummedUpWidth, out double innerSpacing, out double outerSpacing, out double extraWidth);
                    double finalX = outerSpacing;
                    for (int j = currentRowStartIndex; j < itemIndex; j++)
                    {
                        var s = this.GetAssumedItemSize(j, this.Items[j]);
                        finalX += this.GetWidth(s) + extraWidth + innerSpacing;
                    }
                    return this.CreatePoint(finalX, y);
                }
            }

            // Check if it's in the last (possibly unfinished) row
            if (itemIndex >= currentRowStartIndex && itemIndex < currentRowStartIndex + currentRowCount)
            {
                this.GetRowLayout(effectiveWrappingWidth, currentRowCount, currentRowSummedUpWidth, out double innerSpacing, out double outerSpacing, out double extraWidth);
                double finalX = outerSpacing;
                for (int j = currentRowStartIndex; j < itemIndex; j++)
                {
                    var s = this.GetAssumedItemSize(j, this.Items[j]);
                    finalX += this.GetWidth(s) + extraWidth + innerSpacing;
                }
                return this.CreatePoint(finalX, y);
            }

            return this.CreatePoint(0, y);
        }
    }

    /// <summary>
    ///  Calculates the anchor index and scroll offset for the anchor
    /// </summary>
    private void FindStartIndexAndOffset()
    {
        double startOffsetY = this.DetermineStartOffsetY();

        if (startOffsetY <= 0)
        {
            this._startItemIndex = this.Items.Count > 0 ? 0 : -1;
            this._startItemOffsetX = 0;
            this._startItemOffsetY = 0;
            return;
        }

        this._startItemIndex = -1;

        double x = 0, y = 0, rowHeight = 0;
        int indexOfFirstRowItem = 0;

        int itemIndex = 0;
        double wrappingWidth = this.GetWrappingWidth();

        // Use cached rows if available to quickly resolve the starting row
        if (this._rowCache.Count > 0)
        {
            int lo = 0, hi = this._rowCache.Count - 1, best = -1;
            while (lo <= hi)
            {
                int mid = (lo + hi) >> 1;
                var r = this._rowCache[mid];
                if (r.Y <= startOffsetY)
                {
                    best = mid;
                    lo = mid + 1;
                }
                else
                {
                    hi = mid - 1;
                }
            }

            if (best >= 0)
            {
                // Found a row that starts at or before startOffsetY
                var foundRow = this._rowCache[best];

                if (startOffsetY < foundRow.Y + foundRow.Height)
                {
                    // This row (or one before it) contains startOffsetY
                    int targetIndex = Math.Max(0, best - Math.Max(0, this.CacheRows));
                    var r = this._rowCache[targetIndex];
                    this._startItemIndex = r.StartIndex;
                    this._startItemOffsetX = 0;
                    this._startItemOffsetY = r.Y;
                    return;
                }
                else
                {
                    // startOffsetY is beyond the foundRow, use it as a starting point for linear scan
                    itemIndex = foundRow.StartIndex + foundRow.Count;
                    x = 0;
                    y = foundRow.Y + foundRow.Height;
                    rowHeight = 0;
                    indexOfFirstRowItem = itemIndex;
                }
            }
        }

        if (!this.AllowDifferentSizedItems && this.Items.Count > 0)
        {
            double itemWidth = this.GetWidth(this.GetAssumedItemSize(this.Items[0]));
            double itemHeight = this.GetHeight(this.GetAssumedItemSize(this.Items[0]));

            if (itemWidth == 0 || itemHeight == 0)
            {
                return;
            }

            double itemsPerRow = Math.Max(Math.Floor((wrappingWidth + EPSILON) / itemWidth), 1);

            int startRowIndex = (int)Math.Floor(startOffsetY / itemHeight);
            this._startItemIndex = (int)(startRowIndex * itemsPerRow);
            this._startItemOffsetX = 0;
            this._startItemOffsetY = startRowIndex * itemHeight;

            // Apply CacheRows
            int rowsToMoveUp = Math.Max(0, this.CacheRows);
            int actualRowsToMoveUp = Math.Min(startRowIndex, rowsToMoveUp);
            this._startItemIndex -= (int)(actualRowsToMoveUp * itemsPerRow);
            this._startItemOffsetY -= actualRowsToMoveUp * itemHeight;

            return;
        }

        if (this.AllowDifferentSizedItems && this.Items.Count > 0)
        {
            this._previousRowsReuse.Clear();

            // Linear scan fallback
            for (; itemIndex < this.Items.Count; itemIndex++)
            {
                object? item = this.Items[itemIndex];
                Size itemSize = this.GetAssumedItemSize(itemIndex, item);

                if (x + this.GetWidth(itemSize) > wrappingWidth && x != 0)
                {
                    this._previousRowsReuse.Add((indexOfFirstRowItem, y));
                    if (this._previousRowsReuse.Count > this.CacheRows + 1)
                    {
                        this._previousRowsReuse.RemoveAt(0);
                    }

                    x = 0;
                    y += rowHeight;
                    rowHeight = 0;
                    indexOfFirstRowItem = itemIndex;
                }

                x += this.GetWidth(itemSize);
                rowHeight = Math.Max(rowHeight, this.GetHeight(itemSize));

                if (y + rowHeight > startOffsetY)
                {
                    // Found the row containing startOffsetY. 
                    // Move back by CacheRows if possible.
                    if (this._previousRowsReuse.Count > 0)
                    {
                        var targetRow = this._previousRowsReuse[Math.Max(0, this._previousRowsReuse.Count - Math.Max(0, this.CacheRows))];
                        this._startItemIndex = targetRow.index;
                        this._startItemOffsetX = 0;
                        this._startItemOffsetY = targetRow.y;
                    }
                    else
                    {
                        this._startItemIndex = indexOfFirstRowItem;
                        this._startItemOffsetX = 0;
                        this._startItemOffsetY = y;
                    }
                    return;
                }
            }
        }

        // make sure that at least one item is realized to allow correct calculation of the extent
        if (this._startItemIndex == -1 && this.Items.Count > 0)
        {
            this._startItemIndex = this.Items.Count - 1;
            this._startItemOffsetX = x;
            this._startItemOffsetY = y;
        }
    }

    /// <summary>
    /// Realizes all elements until the visible ViewPort is full
    /// </summary>
    private void RealizeItemsAndFindEndIndex()
    {
        if (this._startItemIndex == -1)
        {
            this._endItemIndex = -1;
            return;
        }

        int newEndItemIndex = this.Items.Count - 1;
        bool endItemIndexFound = false;

        double endOffsetY = this.DetermineEndOffsetY();

        double wrappingWidth = this.GetWrappingWidth();
        double x = this._startItemOffsetX;
        double y = this._startItemOffsetY;
        double rowHeight = 0;
        double currentRowSummedUpWidth = 0;
        int currentRowStartIndex = this._startItemIndex;
        int currentRowCount = 0;
        bool endRowReached = false;
        int extraRowsToRealize = Math.Max(0, this.CacheRows);

        for (int itemIndex = this._startItemIndex; itemIndex <= newEndItemIndex; itemIndex++)
        {
            if (itemIndex == 0)
            {
                this._sizeOfFirstItem = null;
            }

            object? item = this.Items[itemIndex];

            var container = this.GetOrCreateElement(this.Items, itemIndex);

            if (container == this._scrollToElement)
            {
                this._scrollToIndex = -1;
                this._scrollToElement = null;
            }

            Size? upfrontKnownItemSize = this.GetUpfrontKnownItemSize(item);

            // Prefer measuring with a concrete size when truly known (ItemSize, _sizeOfFirstItem, or provider).
            // If unknown, use Size.Infinity so the template can produce its natural DesiredSize.
            Size? measureSize = upfrontKnownItemSize
                                ?? this._sizeOfFirstItem
                                ?? (!this.ItemSize.NearlyEquals(_EmptySize) ? this.ItemSize : (Size?)null);

            // Optimization: Skip Measure if the container already has the correct desired size.
            // However, we MUST measure if the container was just recycled (e.g. from GetOrCreateElement)
            // because it might have a different item now.
            // Avalonia's VirtualizingPanel usually handles this, but since we are doing custom realization:
            container.Measure(measureSize ?? Size.Infinity);

            var containerSize = this.DetermineContainerSize(item, container, upfrontKnownItemSize);

            if (this._measureElements is not null)
            {
                if (this._measureElements.GetElement(itemIndex) == null)
                {
                    this._averageItemSizeCache = null;
                    this._measureElements.Add(itemIndex, container, containerSize);
                }
            }

            if (this.AllowDifferentSizedItems == false && this._sizeOfFirstItem is null)
            {
                this._sizeOfFirstItem = containerSize;
            }

            if (x != 0 && (x + this.GetWidth(containerSize)) > (wrappingWidth + EPSILON))
            {
                // finalize previous row in cache
                this.AddRowCacheEntry(currentRowStartIndex, y, rowHeight, currentRowCount, currentRowSummedUpWidth);

                // If we've already reached the viewport end row earlier, count down extra rows
                if (endRowReached)
                {
                    if (extraRowsToRealize <= 0)
                    {
                        newEndItemIndex = itemIndex - 1;
                        break;
                    }
                    extraRowsToRealize--;
                }

                x = 0;
                y += rowHeight;
                rowHeight = 0;
                currentRowSummedUpWidth = 0;
                currentRowStartIndex = itemIndex;
                currentRowCount = 0;
            }

            x += this.GetWidth(containerSize);
            currentRowSummedUpWidth += this.GetWidth(containerSize);
            rowHeight = Math.Max(rowHeight, this.GetHeight(containerSize));
            currentRowCount++;

            if (endItemIndexFound == false)
            {
                Debug.Assert(this._sizeOfFirstItem is not null);

                // ! See Debug assert above 
                if (y >= endOffsetY
                    || (this.AllowDifferentSizedItems == false
                        && x + this.GetWidth(this._sizeOfFirstItem!.Value) > wrappingWidth
                        && y + rowHeight >= endOffsetY))
                {
                    endItemIndexFound = true;
                    endRowReached = true;
                    newEndItemIndex = itemIndex;
                }
            }
        }

        // finalize last row
        this.AddRowCacheEntry(currentRowStartIndex, y, rowHeight, currentRowCount, currentRowSummedUpWidth);

        this._endItemIndex = newEndItemIndex;
    }

    /// <summary>
    /// Determines the container size
    /// </summary>
    /// <param name="item">the item to use</param>
    /// <param name="container">the container</param>
    /// <param name="upfrontKnownItemSize">the known item size, if any</param>
    /// <returns></returns>
    private Size DetermineContainerSize(object? item, Control container, Size? upfrontKnownItemSize)
    {
        if (this.ItemSizeProvider is not null && item is not null)
        {
            return this.ItemSizeProvider.GetSizeForItem(item);
        }

        return upfrontKnownItemSize ?? this._realizedElements?.GetElementSize(container) ?? container.DesiredSize;
    }

    /// <summary>
    /// Removes all items that are realized before start index
    /// </summary>
    private void VirtualizeItemsBeforeStartIndex()
    {
        this._realizedElements?.RecycleElementsBefore(this._startItemIndex, this.RecycleElement);
    }

    /// <summary>
    /// Removes all items that are realized after start index
    /// </summary>
    private void VirtualizeItemsAfterEndIndex()
    {
        this._realizedElements?.RecycleElementsAfter(this._endItemIndex, this.RecycleElement);
    }

    /// <summary>
    /// Calculates the start y-offset of the effective viewport
    /// </summary>
    /// <returns>the y-component of the effective viewport</returns>
    private double DetermineStartOffsetY()
    {
        return Math.Max(this.GetY(this._viewport.TopLeft), 0);
    }

    /// <summary>
    /// Calculates the end y-offset of the effective viewport
    /// </summary>
    /// <returns>the y-component of the effective viewport</returns>
    private double DetermineEndOffsetY()
    {
        return Math.Max(0, this.GetY(this._viewport.BottomRight));
    }

    /// <summary>
    /// Calculates the upfront known item size
    /// </summary>
    /// <param name="item">the item to use</param>
    /// <returns>the size of the item or null if not known</returns>
    private Size? GetUpfrontKnownItemSize(object? item)
    {
        if (item is null)
        {
            return null;
        }

        if (!this.ItemSize.NearlyEquals(_EmptySize))
        {
            return this.ItemSize;
        }

        if (!this.AllowDifferentSizedItems && this._sizeOfFirstItem != null)
        {
            return this._sizeOfFirstItem;
        }

        if (this.ItemSizeProvider != null)
        {
            return this.ItemSizeProvider.GetSizeForItem(item);
        }

        return null;
    }

    /// <summary>
    /// Calculates the assumed item size
    /// </summary>
    /// <param name="index">the index of the item</param>
    /// <param name="item">the item to use</param>
    /// <returns>the assumed size of the item</returns>
    private Size GetAssumedItemSize(int index, object? item)
    {
        if (item is null)
        {
            return _EmptySize;
        }

        if (this.GetUpfrontKnownItemSize(item) is { } upfrontKnownItemSize)
        {
            return upfrontKnownItemSize;
        }

        if (this._realizedElements?.GetElementSize(index) is { } cachedItemSize)
        {
            return cachedItemSize;
        }

        return this.GetAverageItemSize();
    }

    /// <summary>
    /// Calculates the assumed item size
    /// </summary>
    /// <param name="item">the item to use</param>
    /// <returns>the assumed size of the item</returns>
    private Size GetAssumedItemSize(object? item)
    {
        if (item is null)
        {
            return _EmptySize;
        }

        if (this.GetUpfrontKnownItemSize(item) is { } upfrontKnownItemSize)
        {
            return upfrontKnownItemSize;
        }

        return this.GetAverageItemSize();
    }

    /// <summary>
    /// Arranges items in a single row
    /// </summary>
    /// <param name="rowWidth">the available row width</param>
    /// <param name="children">the children to arrange</param>
    /// <param name="childSizes">the sizes of the children</param>
    /// <param name="y">the y offset of the row</param>
    /// <param name="summedUpChildWidth">the pre-calculated sum of all children's width</param>
    /// <param name="rowHeight">the pre-calculated maximum height of all realized children in the row</param>
    private void ArrangeRow(double rowWidth, List<Control> children, List<Size> childSizes, double y, double summedUpChildWidth, double rowHeight)
    {
        int childCount = children.Count;
        this.GetRowLayout(rowWidth, childCount, summedUpChildWidth, out double innerSpacing, out double outerSpacing, out double extraWidth);

        double x = -this.GetX(this._viewport.TopLeft) + outerSpacing;

        if (this.AllowDifferentSizedItems)
        {
            for (int i = 0; i < childCount; i++)
            {
                var child = children[i];
                Size childSize = childSizes[i];
                child.Arrange(this.CreateRect(x, y, this.GetWidth(childSize) + extraWidth, rowHeight));
                x += this.GetWidth(childSize) + extraWidth + innerSpacing;
            }
        }
        else
        {
            double childWidth = this.GetWidth(childSizes[0]);
            double arrangedWidth = childWidth + extraWidth;
            for (int i = 0; i < childCount; i++)
            {
                var child = children[i];
                child.Arrange(this.CreateRect(x, y, arrangedWidth, rowHeight));
                x += arrangedWidth + innerSpacing;
            }
        }
    }

    private void GetRowLayout(double rowWidth, int actualChildCount, double summedUpChildWidth,
        out double innerSpacing, out double outerSpacing, out double extraWidthPerItem)
    {
        extraWidthPerItem = 0;
        double effectiveSummedUpWidth = summedUpChildWidth;

        if (this.StretchItems && actualChildCount > 0)
        {
            if (this.AllowDifferentSizedItems)
            {
                extraWidthPerItem = (rowWidth - summedUpChildWidth) / actualChildCount;
                effectiveSummedUpWidth = rowWidth;
            }
            else
            {
                var averageSize = this.GetAverageItemSize();
                double childWidth = this.GetWidth(averageSize);
                int itemsPerRow = this.IsGridLayoutEnabled ?
                    (int)Math.Max(1, Math.Floor((rowWidth + EPSILON) / childWidth)) :
                    actualChildCount;

                double stretchedChildWidth = rowWidth / itemsPerRow;
                // Note: We don't have access to children's MaxWidth here easily, 
                // but ArrangeRow handles it if needed. For estimation we use full stretch.
                extraWidthPerItem = stretchedChildWidth - childWidth;
                effectiveSummedUpWidth = itemsPerRow * stretchedChildWidth;
            }
        }

        this.CalculateRowSpacing(rowWidth, actualChildCount, effectiveSummedUpWidth, out innerSpacing, out outerSpacing);
    }

    /// <summary>
    /// Calculates the row spacing between the items and before and after the row
    /// </summary>
    /// <param name="rowWidth">the available row width</param>
    /// <param name="actualChildCount">the number of children in the row</param>
    /// <param name="summedUpChildWidth">the sum of all children's width</param>
    /// <param name="innerSpacing">returns the spacing between items</param>
    /// <param name="outerSpacing">returns the spacing before and after each row</param>
    private void CalculateRowSpacing(double rowWidth, int actualChildCount, double summedUpChildWidth,
        out double innerSpacing, out double outerSpacing)
    {
        int spacingChildCount = actualChildCount;

        if (!this.AllowDifferentSizedItems && this.IsGridLayoutEnabled)
        {
            var averageItemSize = this.GetAverageItemSize();
            double itemWidth = this.GetWidth(averageItemSize);
            if (itemWidth > 0)
            {
                spacingChildCount = (int)Math.Max(1, Math.Floor((rowWidth + EPSILON) / itemWidth));
            }
        }

        double unusedWidth = Math.Max(0, rowWidth - summedUpChildWidth);

        switch (this.SpacingMode)
        {
            case SpacingMode.Uniform:
                innerSpacing = outerSpacing = unusedWidth / (spacingChildCount + 1);
                break;

            case SpacingMode.BetweenItemsOnly:
                innerSpacing = unusedWidth / Math.Max(spacingChildCount - 1, 1);
                outerSpacing = 0;
                break;

            case SpacingMode.StartAndEndOnly:
                innerSpacing = 0;
                outerSpacing = unusedWidth / 2;
                break;

            case SpacingMode.None:
            default:
                innerSpacing = 0;
                outerSpacing = 0;
                break;
        }
    }

    /// <summary>
    /// Calculates the average item size of all realized items
    /// </summary>
    /// <returns>the average item size or <see cref="_FallbackItemSize"/> if no items are available</returns>
    private Size CalculateAverageItemSize()
    {
        var sizes = this._realizedElements?.Sizes;
        int count = sizes?.Count ?? 0;
        if (sizes is null || count == 0)
        {
            return _FallbackItemSize;
        }

        double totalWidth = 0;
        double totalHeight = 0;

        for (int i = 0; i < count; i++)
        {
            totalWidth += sizes[i].Width;
            totalHeight += sizes[i].Height;
        }

        return new Size(totalWidth / count, totalHeight / count);
    }

    /// <summary>
    /// This method gets called when the effective viewport got changed
    /// </summary>
    /// <param name="sender">the sender of the event</param>
    /// <param name="e">the event args</param>
    private void OnEffectiveViewportChanged(object? sender, EffectiveViewportChangedEventArgs e)
    {
        // var vertical = Orientation == Orientation.Vertical;
        double oldViewportStartX = this.GetX(this._viewport.TopLeft);
        double oldViewportStartY = this.GetY(this._viewport.TopLeft); // vertical ? ScrollOffset.Top : _viewport.Left;
        double oldViewportEndX = this.GetX(this._viewport.BottomRight);
        double oldViewportEndY = this.GetY(this._viewport.BottomRight); // vertical ? _viewport.Bottom : _viewport.Right;

        this._viewport = e.EffectiveViewport;
        this._isWaitingForViewportUpdate = false;

        double newViewportStartX = this.GetX(this._viewport.TopLeft);
        double newViewportStartY = this.GetY(this._viewport.TopLeft); // vertical ? _viewport.Top : _viewport.Left;
        double newViewportEndX = this.GetX(this._viewport.BottomRight);
        double newViewportEndY = this.GetY(this._viewport.BottomRight); // ? _viewport.Bottom : _viewport.Right);

        double newViewportWidth = this.GetWidth(this._viewport.Size);

        if (this._lastLayoutWidth.IsCloseTo(newViewportWidth))
        {
            // Optimization: Skip InvalidateMeasure if the new viewport is within what we already have realized/cached.
            // This is safe because:
            // 1. We already have the elements in _realizedElements.
            // 2. MeasureOverride would just result in the same _startItemIndex and _endItemIndex.
            // 3. We STILL call InvalidateArrange() because items might need to be repositioned relative to the viewport.

            // Compute the bottom of the last realized row from the row cache (O(1)) instead of
            // calling FindItemOffset which can be O(N) for AllowDifferentSizedItems.
            double endItemBottom = double.MaxValue;
            if (this._rowCache.Count > 0)
            {
                var lastRow = this._rowCache[this._rowCache.Count - 1];
                endItemBottom = lastRow.Y + lastRow.Height;
            }

            double cacheMargin = this.CacheRows > 0 ? this.GetHeight(this.GetAverageItemSize()) : 0;
            bool withinCached = this._realizedElements != null &&
                                this._startItemIndex >= 0 && this._endItemIndex >= 0 &&
                                newViewportStartY >= this._startItemOffsetY + cacheMargin &&
                                newViewportEndY <= endItemBottom - cacheMargin;

            if (withinCached)
            {
                if (!oldViewportStartX.IsCloseTo(newViewportStartX) ||
                    !oldViewportEndX.IsCloseTo(newViewportEndX) ||
                    !oldViewportStartY.IsCloseTo(newViewportStartY) ||
                    !oldViewportEndY.IsCloseTo(newViewportEndY))
                {
                    this.InvalidateArrange();
                }

                return;
            }
        }

        this._lastLayoutWidth = newViewportWidth;
        this.ClearRowCache();

        if (!oldViewportStartX.IsCloseTo(newViewportStartX) ||
            !oldViewportEndX.IsCloseTo(newViewportEndX) ||
            !oldViewportStartY.IsCloseTo(newViewportStartY) ||
            !oldViewportEndY.IsCloseTo(newViewportEndY))
        {
            this.InvalidateMeasure();
        }
    }

    /// <summary>
    /// This method gets called when the associated ItemsControl is changed
    /// </summary>
    /// <param name="sender">the sender of the event</param>
    /// <param name="e">the event args</param>
    private void OnItemsControlPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (this._focusedElement is not null &&
            e.Property == KeyboardNavigation.TabOnceActiveElementProperty &&
            ReferenceEquals(e.GetOldValue<IInputElement?>(), this._focusedElement))
        {
            // TabOnceActiveElement has moved away from _focusedElement so we can recycle it.
            this.RecycleElement(this._focusedElement, this._focusedIndex);
            this._focusedElement = null;
            this._focusedIndex = -1;
        }
    }

    private void NavigateLeft(ref int currentIndex)
    {
        switch (this.Orientation)
        {
            case Orientation.Horizontal:
                --currentIndex;
                break;

            case Orientation.Vertical:
                if (this.AllowDifferentSizedItems)
                {
                    currentIndex = this.GetIndexInRelativeRow(currentIndex, -1);
                }
                else
                {
                    int itemsPerRow =
                        (int)Math.Max(Math.Floor((this.GetWidth(this._viewport.Size) + EPSILON) / this.GetWidth(this.GetAverageItemSize())), 1);
                    currentIndex -= itemsPerRow;
                }
                break;
        }
    }

    private void NavigateRight(ref int currentIndex)
    {
        switch (this.Orientation)
        {
            case Orientation.Horizontal:
                ++currentIndex;
                break;

            case Orientation.Vertical:
                if (this.AllowDifferentSizedItems)
                {
                    currentIndex = this.GetIndexInRelativeRow(currentIndex, 1);
                }
                else
                {
                    int itemsPerRow =
                        (int)Math.Max(Math.Floor((this.GetWidth(this._viewport.Size) + EPSILON) / this.GetWidth(this.GetAverageItemSize())), 1);
                    currentIndex += itemsPerRow;
                }
                break;
        }
    }

    private void NavigateUp(ref int currentIndex)
    {
        switch (this.Orientation)
        {
            case Orientation.Vertical:
                --currentIndex;
                break;
            case Orientation.Horizontal:
                if (this.AllowDifferentSizedItems)
                {
                    currentIndex = this.GetIndexInRelativeRow(currentIndex, -1);
                }
                else
                {
                    int itemsPerRow =
                        (int)Math.Max(Math.Floor((this.GetWidth(this._viewport.Size) + EPSILON) / this.GetWidth(this.GetAverageItemSize())), 1);
                    currentIndex -= itemsPerRow;
                }
                break;
        }
    }

    private void NavigateDown(ref int currentIndex)
    {
        switch (this.Orientation)
        {
            case Orientation.Vertical:
                ++currentIndex;
                break;
            case Orientation.Horizontal:
                if (this.AllowDifferentSizedItems)
                {
                    currentIndex = this.GetIndexInRelativeRow(currentIndex, 1);
                }
                else
                {
                    int itemsPerRow =
                        (int)Math.Max(Math.Floor((this.GetWidth(this._viewport.Size) + EPSILON) / this.GetWidth(this.GetAverageItemSize())), 1);
                    currentIndex += itemsPerRow;
                }
                break;
        }
    }

    private int GetIndexInRelativeRow(int currentIndex, int rowOffset)
    {
        int itemCount = this.Items.Count;
        if (currentIndex < 0 || currentIndex >= itemCount)
        {
            return currentIndex;
        }

        double wrappingWidth = this.GetWrappingWidth();

        // --- Step 1: find source row StartIndex from cache (O(log N)) or single linear scan (O(N)) ---
        int sourceCacheIndex = -1;
        RowInfo? sourceRowHint = null;

        if (this._rowCache.Count > 0)
        {
            int lo = 0, hi = this._rowCache.Count - 1;
            while (lo <= hi)
            {
                int mid = (lo + hi) >> 1;
                var r = this._rowCache[mid];
                if (r.StartIndex <= currentIndex)
                {
                    if (currentIndex < r.StartIndex + r.Count)
                    {
                        sourceRowHint = r;
                        sourceCacheIndex = mid;
                        break;
                    }
                    lo = mid + 1;
                }
                else
                {
                    hi = mid - 1;
                }
            }
        }

        if (sourceRowHint is null)
        {
            sourceRowHint = this.FindRowByLinearScan(currentIndex, wrappingWidth);
            if (sourceRowHint is null)
            {
                return currentIndex;
            }
        }

        // Scan source row for true Count/SummedWidth (cache entry may be incomplete if it's the last row)
        var (sourceCount, sourceSumW) = this.ScanRowFromStart(sourceRowHint.StartIndex, wrappingWidth);

        // --- Step 2: compute current item X and width (no FindItemOffset) ---
        this.GetRowLayout(wrappingWidth, sourceCount, sourceSumW,
            out double sourceInnerSpacing, out double sourceOuterSpacing, out double sourceExtraWidth);

        double currentX = sourceOuterSpacing;
        for (int i = sourceRowHint.StartIndex; i < currentIndex; i++)
        {
            currentX += this.GetWidth(this.GetAssumedItemSize(i, this.Items[i])) + sourceExtraWidth + sourceInnerSpacing;
        }

        double rawCurrentWidth = this.GetWidth(this.GetAssumedItemSize(currentIndex, this.Items[currentIndex]));
        double currentWidth = rawCurrentWidth + sourceExtraWidth;
        double currentMidX = currentX + currentWidth / 2;

        // Use or initialize navigation anchor
        if (this._navigationAnchor.HasValue)
        {
            currentMidX = this._navigationAnchor.Value;
            currentWidth = 0; // Use a zero-width span when we have an anchor to avoid wide-item drift
        }
        else
        {
            this._navigationAnchor = currentMidX;
        }

        // --- Step 3: find target row StartIndex from cache (O(1)) or single linear scan (O(N)) ---
        int targetStartIndex;

        if (sourceCacheIndex >= 0)
        {
            int targetCacheIndex = sourceCacheIndex + rowOffset;
            if (targetCacheIndex >= 0 && targetCacheIndex < this._rowCache.Count)
            {
                targetStartIndex = this._rowCache[targetCacheIndex].StartIndex;
            }
            else
            {
                // Adjacent row is outside cache bounds
                int searchFrom = rowOffset < 0 ? sourceRowHint.StartIndex - 1 : sourceRowHint.StartIndex + sourceCount;
                if (searchFrom < 0 || searchFrom >= itemCount)
                {
                    return currentIndex;
                }

                var fallback = this.FindRowByLinearScan(searchFrom, wrappingWidth);
                if (fallback is null)
                {
                    return currentIndex;
                }

                targetStartIndex = fallback.StartIndex;
            }
        }
        else
        {
            int searchFrom = rowOffset < 0 ? sourceRowHint.StartIndex - 1 : sourceRowHint.StartIndex + sourceCount;
            if (searchFrom < 0 || searchFrom >= itemCount)
            {
                return currentIndex;
            }

            var fallback = this.FindRowByLinearScan(searchFrom, wrappingWidth);
            if (fallback is null)
            {
                return currentIndex;
            }

            targetStartIndex = fallback.StartIndex;
        }

        // --- Step 4: scan target row for true Count/SummedWidth, then find closest item ---
        var (targetCount, targetSumW) = this.ScanRowFromStart(targetStartIndex, wrappingWidth);
        if (targetCount == 0)
        {
            return currentIndex;
        }

        this.GetRowLayout(wrappingWidth, targetCount, targetSumW,
            out double targetInnerSpacing, out double targetOuterSpacing, out double targetExtraWidth);

        double sourceStart = currentMidX - currentWidth / 2;
        double sourceEnd = currentMidX + currentWidth / 2;
        if (currentWidth.IsAlmostZero())
        {
            sourceStart -= EPSILON;
            sourceEnd += EPSILON;
        }

        int bestIndex = targetStartIndex;
        double maxOverlap = -1;
        double minDiff = double.MaxValue;
        double itemX = targetOuterSpacing;

        for (int i = 0; i < targetCount; i++)
        {
            int idx = targetStartIndex + i;
            double itemWidth = this.GetWidth(this.GetAssumedItemSize(idx, this.Items[idx])) + targetExtraWidth;
            double itemMidX = itemX + itemWidth / 2;

            double overlap = Math.Max(0, Math.Min(sourceEnd, itemX + itemWidth) - Math.Max(sourceStart, itemX));
            double diff = Math.Abs(itemMidX - currentMidX);

            if (overlap > maxOverlap + EPSILON || (Math.Abs(overlap - maxOverlap) < EPSILON && diff < minDiff - EPSILON))
            {
                maxOverlap = overlap;
                minDiff = diff;
                bestIndex = idx;
            }
            else if (itemX > sourceEnd && overlap.IsAlmostZero() && maxOverlap > 0)
            {
                break; // moved past the source span, done
            }

            itemX += itemWidth + targetInnerSpacing;
        }

        return bestIndex;
    }

    /// <summary>
    /// Scans a row starting at <paramref name="startIndex"/> and accumulates items until the row wraps.
    /// Returns the true item count and total width for that row.
    /// This is needed because the row cache entry may record an incomplete count (the last measured row
    /// can be cut off before all its items are processed).
    /// </summary>
    private (int Count, double SummedWidth) ScanRowFromStart(int startIndex, double wrappingWidth)
    {
        double x = 0, sumW = 0;
        int count = 0;
        for (int i = startIndex; i < this.Items.Count; i++)
        {
            double w = this.GetWidth(this.GetAssumedItemSize(i, this.Items[i]));
            if (count > 0 && x + w > wrappingWidth + EPSILON)
            {
                break;
            }

            x += w;
            sumW += w;
            count++;
        }
        return (count, sumW);
    }

    /// <summary>
    /// Single O(N) linear scan to find the row containing <paramref name="itemIndex"/>.
    /// Uses the row cache to find the best starting point, so in practice the scan
    /// only covers items not yet cached.
    /// </summary>
    private RowInfo? FindRowByLinearScan(int itemIndex, double wrappingWidth)
    {
        if (itemIndex < 0 || itemIndex >= this.Items.Count)
        {
            return null;
        }

        double x = 0, y = 0, rowHeight = 0, rowSummedWidth = 0;
        int scanFrom = 0, rowStart = 0, rowCount = 0;

        // Seed from the nearest cached predecessor to avoid scanning from index 0
        if (this._rowCache.Count > 0)
        {
            int lo = 0, hi = this._rowCache.Count - 1, best = -1;
            while (lo <= hi)
            {
                int mid = (lo + hi) >> 1;
                if (this._rowCache[mid].StartIndex <= itemIndex) { best = mid; lo = mid + 1; }
                else
                {
                    hi = mid - 1;
                }
            }
            if (best >= 0)
            {
                var seed = this._rowCache[best];
                if (itemIndex < seed.StartIndex + seed.Count)
                {
                    return seed; // already covered by cache
                }

                scanFrom = seed.StartIndex + seed.Count;
                y = seed.Y + seed.Height;
                rowStart = scanFrom;
            }
        }

        for (int i = scanFrom; i < this.Items.Count; i++)
        {
            var size = this.GetAssumedItemSize(i, this.Items[i]);
            double w = this.GetWidth(size);

            if (rowCount > 0 && x + w > wrappingWidth + EPSILON)
            {
                if (itemIndex >= rowStart && itemIndex < i)
                {
                    return new RowInfo { StartIndex = rowStart, Y = y, Height = rowHeight, Count = rowCount, SummedUpChildWidth = rowSummedWidth };
                }

                y += rowHeight;
                rowHeight = 0;
                rowSummedWidth = 0;
                rowStart = i;
                rowCount = 0;
                x = 0;
            }

            x += w;
            rowSummedWidth += w;
            rowHeight = Math.Max(rowHeight, this.GetHeight(size));
            rowCount++;
        }

        // Last (possibly incomplete) row
        if (itemIndex >= rowStart && itemIndex < rowStart + rowCount)
        {
            return new RowInfo { StartIndex = rowStart, Y = y, Height = rowHeight, Count = rowCount, SummedUpChildWidth = rowSummedWidth };
        }

        return null;
    }

    /// <summary>
    /// Calculates a virtual X-coordinate based on the <see cref="Orientation"/>
    /// </summary>
    private double GetX(Point point) => this.Orientation == Orientation.Horizontal ? point.X : point.Y;

    /// <summary>
    /// Calculates a virtual Y-coordinate based on the <see cref="Orientation"/>
    /// </summary>
    private double GetY(Point point) => this.Orientation == Orientation.Horizontal ? point.Y : point.X;

    /// <summary>
    /// Calculates a virtual width-component based on the <see cref="Orientation"/>
    /// </summary>
    private double GetWidth(Size size) => this.Orientation == Orientation.Horizontal ? size.Width : size.Height;

    /// <summary>
    /// Calculates a virtual height-component based on the <see cref="Orientation"/>
    /// </summary>
    private double GetHeight(Size size) => this.Orientation == Orientation.Horizontal ? size.Height : size.Width;

    /// <summary>
    /// Creates a virtual Point based on the <see cref="Orientation"/>
    /// </summary>
    private Point CreatePoint(double x, double y) =>
        this.Orientation == Orientation.Horizontal ? new Point(x, y) : new Point(y, x);

    /// <summary>
    /// Creates a virtual Rect based on the <see cref="Orientation"/>
    /// </summary>
    private Rect CreateRect(double x, double y, double width, double height) =>
        this.Orientation == Orientation.Horizontal ? new Rect(x, y, width, height) : new Rect(y, x, height, width);


    /// <summary>
    /// Gets an existing container or creates a new one of none was present
    /// </summary>
    /// <param name="items">the available items</param>
    /// <param name="index">the index to create</param>
    /// <returns>the requested container</returns>
    private Control GetOrCreateElement(IReadOnlyList<object?> items, int index)
    {
        Debug.Assert(this.ItemContainerGenerator is not null);

        if (GetRealizedElement(index, ref this._focusedIndex, ref this._focusedElement) is { } focusedElement)
        {
            return focusedElement;
        }

        if (GetRealizedElement(index, ref this._scrollToIndex, ref this._scrollToElement) is { } scrollToElement)
        {
            return scrollToElement;
        }

        if (this.GetRealizedElement(index) is { } realized)
        {
            return realized;
        }

        object? item = items[index];

        // ! See Debug assert above 
        var generator = this.ItemContainerGenerator!;

        if (generator.NeedsContainer(item, index, out object? recycleKey))
        {
            return this.GetRecycledElement(item, index, recycleKey) ??
                   this.CreateElement(item, index, recycleKey);
        }
        else
        {
            return this.GetItemAsOwnContainer(item, index);
        }
    }

    /// <summary>
    /// Gets the realized element or null if not available
    /// </summary>
    /// <param name="index">The container index to lookup</param>
    /// <returns>the realized container</returns>
    private Control? GetRealizedElement(int index)
    {
        return this._realizedElements?.GetElement(index);
    }

    /// <summary>
    /// Gets the realized element or null if not available
    /// </summary>
    /// <param name="index">The container index to lookup</param>
    /// <param name="specialIndex">the reference to a special index, e.g. <see cref="_focusedIndex"/></param>
    /// <param name="specialElement">the reference to a special element, e.g. <see cref="_focusedElement"/></param>
    /// <returns>the realized container</returns>
    private static Control? GetRealizedElement(
        int index,
        ref int specialIndex,
        ref Control? specialElement)
    {
        if (specialIndex == index)
        {
            Debug.Assert(specialElement is not null);

            var result = specialElement;
            specialIndex = -1;
            specialElement = null;
            return result;
        }

        return null;
    }

    /// <summary>
    /// Prepares a container if the item is its own container
    /// </summary>
    /// <param name="item">the item to use</param>
    /// <param name="index">the item index</param>
    /// <returns>the prepared container</returns>
    private Control GetItemAsOwnContainer(object? item, int index)
    {
        Debug.Assert(item is not null);
        Debug.Assert(this.ItemContainerGenerator is not null);

        // ! See Debug assert above 
        var controlItem = (Control)item!;

        // ! See Debug assert above 
        var generator = this.ItemContainerGenerator!;

        if (!controlItem.IsSet(_RecycleKeyProperty))
        {
            generator.PrepareItemContainer(controlItem, controlItem, index);
            this.AddInternalChild(controlItem);
            controlItem.SetValue(_RecycleKeyProperty, s_itemIsItsOwnContainer);
            generator.ItemContainerPrepared(controlItem, item, index);
        }

        controlItem.SetCurrentValue(Visual.IsVisibleProperty, true);
        return controlItem;
    }

    /// <summary>
    /// Gets a recycled container or null if no container to recycle was available
    /// </summary>
    /// <param name="item">the item which uses the container</param>
    /// <param name="index">the item index</param>
    /// <param name="recycleKey">the recycle key</param>
    /// <returns>the recycled container</returns>
    private Control? GetRecycledElement(object? item, int index, object? recycleKey)
    {
        Debug.Assert(this.ItemContainerGenerator is not null);

        if (recycleKey is null)
        {
            return null;
        }

        // ! See Debug assert above 
        var generator = this.ItemContainerGenerator!;

        if (this._recyclePool?.TryGetValue(recycleKey, out var recyclePool) == true && recyclePool.Count > 0)
        {
            var recycled = recyclePool.Pop();
            recycled.SetCurrentValue(Visual.IsVisibleProperty, true);
            generator.PrepareItemContainer(recycled, item, index);
            generator.ItemContainerPrepared(recycled, item, index);
            return recycled;
        }

        return null;
    }

    /// <summary>
    /// Creates a container for a given item
    /// </summary>
    /// <param name="item">the item which needs a container</param>
    /// <param name="index">the item index</param>
    /// <param name="recycleKey">the recycle key to use</param>
    /// <returns>the created element</returns>
    private Control CreateElement(object? item, int index, object? recycleKey)
    {
        Debug.Assert(this.ItemContainerGenerator is not null);

        // ! See Debug assert above 
        var generator = this.ItemContainerGenerator!;
        var container = generator.CreateContainer(item, index, recycleKey);

        container.SetValue(_RecycleKeyProperty, recycleKey);
        generator.PrepareItemContainer(container, item, index);
        this.AddInternalChild(container);
        generator.ItemContainerPrepared(container, item, index);

        return container;
    }

    /// <summary>
    /// Recycles a container
    /// </summary>
    /// <param name="element">the container to recycle</param>
    /// <param name="index">the item index</param>
    private void RecycleElement(Control element, int index)
    {
        Debug.Assert(this.ItemsControl is not null);
        Debug.Assert(this.ItemContainerGenerator is not null);

        this._scrollAnchorProvider?.UnregisterAnchorCandidate(element);

        object? recycleKey = element.GetValue(_RecycleKeyProperty);

        if (recycleKey is null)
        {
            this.RemoveInternalChild(element);
        }
        else if (recycleKey == s_itemIsItsOwnContainer)
        {
            element.SetCurrentValue(Visual.IsVisibleProperty, false);
        }
        else if (ReferenceEquals(KeyboardNavigation.GetTabOnceActiveElement(this.ItemsControl), element))
        {
            this._focusedElement = element;
            this._focusedIndex = index;
        }
        else
        {
            // ! See Debug assert above 
            this.ItemContainerGenerator!.ClearItemContainer(element);
            this.PushToRecyclePool(recycleKey, element);
            element.SetCurrentValue(Visual.IsVisibleProperty, false);
        }
    }

    /// <summary>
    /// Recycles a container if the item was removed
    /// </summary>
    /// <param name="element">the container to recycle</param>
    private void RecycleElementOnItemRemoved(Control element)
    {
        Debug.Assert(this.ItemContainerGenerator is not null);

        object? recycleKey = element.GetValue(_RecycleKeyProperty);

        if (recycleKey is null || recycleKey == s_itemIsItsOwnContainer)
        {
            this.RemoveInternalChild(element);
        }
        else
        {
            // RemoveInternalChild(element);
            // ! See Debug assert above 
            this.ItemContainerGenerator!.ClearItemContainer(element);
            this.PushToRecyclePool(recycleKey, element);
            element.SetCurrentValue(Visual.IsVisibleProperty, false);
        }
    }

    /// <summary>
    /// Pushes a container to the recycle pool
    /// </summary>
    /// <param name="recycleKey">the containers recycle-key</param>
    /// <param name="element">the container to recycle</param>
    private void PushToRecyclePool(object recycleKey, Control element)
    {
        this._recyclePool ??= new();

        if (!this._recyclePool.TryGetValue(recycleKey, out var pool))
        {
            pool = new();
            this._recyclePool.Add(recycleKey, pool);
        }

        pool.Push(element);

        // If the pool exceeds the cap, eject the oldest container from the visual tree.
        while (pool.Count > RecyclePoolMaxSize)
        {
            this.RemoveInternalChild(pool.Pop());
        }
    }

    /// <summary>
    /// Updates the index of an element
    /// </summary>
    /// <param name="element">the affected element</param>
    /// <param name="oldIndex">the old index</param>
    /// <param name="newIndex">the new index</param>
    private void UpdateElementIndex(Control element, int oldIndex, int newIndex)
    {
        Debug.Assert(this.ItemContainerGenerator is not null);

        this.ItemContainerGenerator.ItemContainerIndexChanged(element, oldIndex, newIndex);
    }

    /// <summary>
    /// Defines the <see cref="AreHorizontalSnapPointsRegular"/> property.
    /// </summary>
    public static readonly StyledProperty<bool> AreHorizontalSnapPointsRegularProperty =
        StackPanel.AreHorizontalSnapPointsRegularProperty.AddOwner<VirtualizingWrapPanel>();

    /// <summary>
    /// Defines the <see cref="AreVerticalSnapPointsRegular"/> property.
    /// </summary>
    public static readonly StyledProperty<bool> AreVerticalSnapPointsRegularProperty =
        StackPanel.AreVerticalSnapPointsRegularProperty.AddOwner<VirtualizingWrapPanel>();

    /// <summary>
    /// Defines the <see cref="HorizontalSnapPointsChanged"/> event.
    /// </summary>
    public static readonly RoutedEvent<RoutedEventArgs> HorizontalSnapPointsChangedEvent =
        RoutedEvent.Register<VirtualizingWrapPanel, RoutedEventArgs>(
            nameof(HorizontalSnapPointsChanged),
            RoutingStrategies.Bubble);

    /// <summary>
    /// Defines the <see cref="VerticalSnapPointsChanged"/> event.
    /// </summary>
    public static readonly RoutedEvent<RoutedEventArgs> VerticalSnapPointsChangedEvent =
        RoutedEvent.Register<VirtualizingWrapPanel, RoutedEventArgs>(
            nameof(VerticalSnapPointsChanged),
            RoutingStrategies.Bubble);

    /// <inheritdoc/>
    public Size GetSizeForItem(object item)
    {
        return this.GetUpfrontKnownItemSize(item) ?? this.GetAssumedItemSize(item);
    }

    /// <inheritdoc/>
    public IReadOnlyList<double> GetIrregularSnapPoints(Orientation orientation,
        SnapPointsAlignment snapPointsAlignment)
    {
        if (this._realizedElements is null)
        {
            return Array.Empty<double>();
        }

        return new VirtualizingWrapPanelSnapPointList(
            this._realizedElements,
            this.Items.Count,
            orientation,
            this.Orientation,
            snapPointsAlignment,
            this.GetWidth(this.GetAverageItemSize()), // This is slightly wrong as it depends on orientation, but VirtualizingWrapPanelSnapPointList seems to want one size
            this as IItemSizeProvider);
    }

    private Control? GetFirstRealizedContainer()
    {
        if (this._realizedElements is null)
        {
            return null;
        }

        var elements = this._realizedElements.Elements;
        for (int i = 0; i < elements.Count; i++)
        {
            if (elements[i] is { } e)
            {
                return e;
            }
        }

        return null;
    }

    /// <inheritdoc/>
    public double GetRegularSnapPoints(Orientation orientation, SnapPointsAlignment snapPointsAlignment,
        out double offset)
    {
        offset = 0f;
        var firstChild = this.GetFirstRealizedContainer();

        if (firstChild == null)
        {
            return 0;
        }

        double snapPoint = 0;

        switch (this.Orientation)
        {
            case Orientation.Horizontal:
                if (!this.AreHorizontalSnapPointsRegular)
                {
                    throw new InvalidOperationException();
                }

                snapPoint = firstChild.Bounds.Width;
                switch (snapPointsAlignment)
                {
                    case SnapPointsAlignment.Near:
                        offset = firstChild.Bounds.Left;
                        break;
                    case SnapPointsAlignment.Center:
                        offset = firstChild.Bounds.Center.X;
                        break;
                    case SnapPointsAlignment.Far:
                        offset = firstChild.Bounds.Right;
                        break;
                }

                break;
            case Orientation.Vertical:
                if (!this.AreVerticalSnapPointsRegular)
                {
                    throw new InvalidOperationException();
                }

                snapPoint = firstChild.Bounds.Height;
                switch (snapPointsAlignment)
                {
                    case SnapPointsAlignment.Near:
                        offset = firstChild.Bounds.Top;
                        break;
                    case SnapPointsAlignment.Center:
                        offset = firstChild.Bounds.Center.Y;
                        break;
                    case SnapPointsAlignment.Far:
                        offset = firstChild.Bounds.Bottom;
                        break;
                }

                break;
        }

        return snapPoint;
    }

    /// <summary>
    /// Occurs when the measurements for horizontal snap points change.
    /// </summary>
    public event EventHandler<RoutedEventArgs>? HorizontalSnapPointsChanged
    {
        add => this.AddHandler(HorizontalSnapPointsChangedEvent, value);
        remove => this.RemoveHandler(HorizontalSnapPointsChangedEvent, value);
    }

    /// <summary>
    /// Occurs when the measurements for vertical snap points change.
    /// </summary>
    public event EventHandler<RoutedEventArgs>? VerticalSnapPointsChanged
    {
        add => this.AddHandler(VerticalSnapPointsChangedEvent, value);
        remove => this.RemoveHandler(VerticalSnapPointsChangedEvent, value);
    }

    /// <summary>
    /// Gets or sets whether the horizontal snap points for the <see cref="StackPanel"/> are equidistant from each other.
    /// </summary>
    public bool AreHorizontalSnapPointsRegular
    {
        get => this.GetValue(AreHorizontalSnapPointsRegularProperty);
        set => this.SetValue(AreHorizontalSnapPointsRegularProperty, value);
    }

    /// <summary>
    /// Gets or sets whether the vertical snap points for the <see cref="StackPanel"/> are equidistant from each other.
    /// </summary>
    public bool AreVerticalSnapPointsRegular
    {
        get => this.GetValue(AreVerticalSnapPointsRegularProperty);
        set => this.SetValue(AreVerticalSnapPointsRegularProperty, value);
    }
}