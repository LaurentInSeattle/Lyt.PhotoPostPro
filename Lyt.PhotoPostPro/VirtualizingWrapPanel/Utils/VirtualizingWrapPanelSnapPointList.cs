
namespace Avalonia.Labs.Controls.Utils;

internal class VirtualizingWrapPanelSnapPointList : IReadOnlyList<double>
{
    private const int ExtraCount = 2;
    private readonly RealizedWrapElements _realizedElements;
    private readonly Orientation _orientation;
    private readonly Orientation _parentOrientation;
    private readonly SnapPointsAlignment _snapPointsAlignment;
    private readonly double _size;
    private readonly int _start = -1;
    private readonly int _end;

    private readonly IItemSizeProvider? _itemSizeProvider;

    public VirtualizingWrapPanelSnapPointList(RealizedWrapElements realizedElements, int count, Orientation orientation, Orientation parentOrientation, SnapPointsAlignment snapPointsAlignment, double size, IItemSizeProvider? itemSizeProvider)
    {
        this._realizedElements = realizedElements;
        this._orientation = orientation;
        this._parentOrientation = parentOrientation;
        this._snapPointsAlignment = snapPointsAlignment;
        this._size = size;
        this._itemSizeProvider = itemSizeProvider;
        if (parentOrientation == orientation)
        {
            this._start = Math.Max(0, this._realizedElements.FirstIndex - ExtraCount);
            this._end = Math.Min(count - 1, this._realizedElements.LastIndex + ExtraCount);
        }
    }

    public double this[int index]
    {
        get
        {
            if (index < 0 || index >= this.Count)
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }

            index += this._start;

            double snapPoint = 0;
            var averageElementSize = this._size;

            Control? container;
            switch (this._orientation)
            {
                case Orientation.Horizontal:
                    container = this._realizedElements.GetElement(index);
                    if (container != null)
                    {
                        switch (this._snapPointsAlignment)
                        {
                            case SnapPointsAlignment.Near:
                                snapPoint = container.Bounds.Left;
                                break;
                            case SnapPointsAlignment.Center:
                                snapPoint = container.Bounds.Center.X;
                                break;
                            case SnapPointsAlignment.Far:
                                snapPoint = container.Bounds.Right;
                                break;
                        }
                    }
                    else if (index < this._realizedElements.FirstIndex)
                    {
                        // Estimate position by stepping backward from the first realized element.
                        var firstElement = this._realizedElements.GetElement(this._realizedElements.FirstIndex);
                        double basePosition = firstElement != null
                            ? firstElement.Bounds.Left
                            : this._realizedElements.FirstIndex * averageElementSize;
                        int stepsBack = this._realizedElements.FirstIndex - index;
                        snapPoint = basePosition - stepsBack * averageElementSize;
                        switch (this._snapPointsAlignment)
                        {
                            case SnapPointsAlignment.Center:
                                snapPoint += averageElementSize / 2;
                                break;
                            case SnapPointsAlignment.Far:
                                snapPoint += averageElementSize;
                                break;
                        }
                    }
                    else
                    {
                        // index > LastIndex: estimate forward from the last realized element.
                        int stepsForward = index - this._realizedElements.LastIndex;
                        var lastElement = this._realizedElements.GetElement(this._realizedElements.LastIndex);
                        double basePosition = lastElement != null
                            ? lastElement.Bounds.Right
                            : (this._realizedElements.LastIndex + 1) * averageElementSize;
                        snapPoint = basePosition + (stepsForward - 1) * averageElementSize;
                        switch (this._snapPointsAlignment)
                        {
                            case SnapPointsAlignment.Center:
                                snapPoint += averageElementSize / 2;
                                break;
                            case SnapPointsAlignment.Far:
                                snapPoint += averageElementSize;
                                break;
                        }
                    }
                    break;
                case Orientation.Vertical:
                    container = this._realizedElements.GetElement(index);
                    if (container != null)
                    {
                        switch (this._snapPointsAlignment)
                        {
                            case SnapPointsAlignment.Near:
                                snapPoint = container.Bounds.Top;
                                break;
                            case SnapPointsAlignment.Center:
                                snapPoint = container.Bounds.Center.Y;
                                break;
                            case SnapPointsAlignment.Far:
                                snapPoint = container.Bounds.Bottom;
                                break;
                        }
                    }
                    else if (index < this._realizedElements.FirstIndex)
                    {
                        // Estimate position by stepping backward from the first realized element.
                        var firstElement = this._realizedElements.GetElement(this._realizedElements.FirstIndex);
                        double basePosition = firstElement != null
                            ? firstElement.Bounds.Top
                            : this._realizedElements.FirstIndex * averageElementSize;
                        int stepsBack = this._realizedElements.FirstIndex - index;
                        snapPoint = basePosition - stepsBack * averageElementSize;
                        switch (this._snapPointsAlignment)
                        {
                            case SnapPointsAlignment.Center:
                                snapPoint += averageElementSize / 2;
                                break;
                            case SnapPointsAlignment.Far:
                                snapPoint += averageElementSize;
                                break;
                        }
                    }
                    else
                    {
                        // index > LastIndex: estimate forward from the last realized element.
                        int stepsForward = index - this._realizedElements.LastIndex;
                        var lastElement = this._realizedElements.GetElement(this._realizedElements.LastIndex);
                        double basePosition = lastElement != null
                            ? lastElement.Bounds.Bottom
                            : (this._realizedElements.LastIndex + 1) * averageElementSize;
                        snapPoint = basePosition + (stepsForward - 1) * averageElementSize;
                        switch (this._snapPointsAlignment)
                        {
                            case SnapPointsAlignment.Center:
                                snapPoint += averageElementSize / 2;
                                break;
                            case SnapPointsAlignment.Far:
                                snapPoint += averageElementSize;
                                break;
                        }
                    }
                    break;
            }

            return snapPoint;
        }
    }

    public int Count => this._parentOrientation != this._orientation ? 0 : this._end - this._start + 1;

    public IEnumerator<double> GetEnumerator()
    {
        for (var i = 0; i < this.Count; i++)
        {
            yield return this[i];
        }
    }

    IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();
}
