namespace Avalonia.Labs.Controls; 

using Avalonia.Labs.Controls.Utils;

/// <summary>
/// Stores the realized element state for a virtualizing panel that arranges its children
/// in a wrap layout, such as <see cref="VirtualizingWrapPanel"/>.
/// </summary>
internal class RealizedWrapElements
{
    private int _firstIndex;
    private readonly List<Control?> _elements;
    private readonly List<Size> _sizes;
    private readonly Dictionary<Control, int> _elementToIndex = new();

    public RealizedWrapElements()
    {
        // Pre-allocate with reasonable capacity to reduce reallocations
        this._elements = new List<Control?>(32);
        this._sizes = new List<Size>(32);
    }

    /// <summary>
    /// Gets the number of realized elements.
    /// </summary>
    public int Count => this._elements.Count;

    /// <summary>
    /// Gets the index of the first realized element, or -1 if no elements are realized.
    /// </summary>
    public int FirstIndex => this._elements.Count > 0 ? this._firstIndex : -1;

    /// <summary>
    /// Gets the index of the last realized element, or -1 if no elements are realized.
    /// </summary>
    public int LastIndex => this._elements.Count > 0 ? this._firstIndex + this._elements.Count - 1 : -1;

    /// <summary>
    /// Gets the elements.
    /// </summary>
    public IReadOnlyList<Control?> Elements => this._elements;

    /// <summary>
    /// Gets the sizes of the elements on the primary axis.
    /// </summary>
    public IReadOnlyList<Size> Sizes => this._sizes;

    /// <summary>
    /// Adds a newly realized element to the collection.
    /// </summary>
    /// <param name="index">The index of the element.</param>
    /// <param name="element">The element.</param>
    /// <param name="size">The size of the element on the primary axis.</param>
    public void Add(int index, Control element, Size size)
    {
        if (index < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(index));
        }

        int count = this._elements.Count;

        if (count == 0)
        {
            this._elements.Add(element);
            this._sizes.Add(size);
            this._elementToIndex[element] = index;
            this._firstIndex = index;
        }
        else if (index == this._firstIndex + count)
        {
            this._elements.Add(element);
            this._sizes.Add(size);
            this._elementToIndex[element] = index;
        }
        else if (index == this._firstIndex - 1)
        {
            --this._firstIndex;
            this._elements.Insert(0, element);
            this._sizes.Insert(0, size);
            this._elementToIndex[element] = index;
        }
        else
        {
            throw new NotSupportedException("Can only add items to the beginning or end of realized elements.");
        }
    }

    /// <summary>
    /// Gets the element at the specified index, if realized.
    /// </summary>
    /// <param name="index">The index in the source collection of the element to get.</param>
    /// <returns>The element if realized; otherwise null.</returns>
    public Control? GetElement(int index)
    {
        int i = index - this._firstIndex;
        int count = this._elements.Count;
        if (i >= 0 && i < count)
        {
            return this._elements[i];
        }

        return null;
    }

    /// <summary>
    /// Gets the Size of the element, if realized.
    /// </summary>
    /// <returns>
    /// The size of the element or Infinite if not found
    /// </returns>
    public Size? GetElementSize(Control? child)
    {
        if (child == null)
        {
            return null;
        }

        int index = this.GetIndex(child);

        if (index < 0)
        {
            return null;
        }

        int localIndex = index - this._firstIndex;
        if (localIndex < 0 || localIndex >= this._sizes.Count)
        {
            return null;
        }

        return this._sizes[localIndex];
    }

    /// <summary>
    /// Gets the Size of the element, if realized.
    /// </summary>
    /// <param name="index">The index to lookup</param>
    /// <returns>The size of the element or null if not found</returns>
    public Size? GetElementSize(int index)
    {
        if (index < this.FirstIndex)
        {
            return null;
        }

        int localIndex = index - this._firstIndex;
        if (localIndex >= this._sizes.Count)
        {
            return null;
        }

        return this._sizes[localIndex];
    }

    /// <summary>
    /// Gets the index of the specified element.
    /// </summary>
    /// <param name="element">The element.</param>
    /// <returns>The index or -1 if the element is not present in the collection.</returns>
    public int GetIndex(Control element) => this._elementToIndex.TryGetValue(element, out int index) ? index : -1;

    /// <summary>
    /// Updates the elements in response to items being inserted into the source collection.
    /// </summary>
    /// <param name="index">The index in the source collection of the insert.</param>
    /// <param name="count">The number of items inserted.</param>
    /// <param name="updateElementIndex">A method used to update the element indexes.</param>
    public void ItemsInserted(int index, int count, Action<Control, int, int> updateElementIndex)
    {
        if (index < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(index));
        }

        int elementCount = this._elements.Count;
        if (elementCount == 0)
        {
            return;
        }

        // Get the index within the realized _elements collection.
        int first = this._firstIndex;
        int realizedIndex = index - first;

        if (realizedIndex < elementCount)
        {
            // The insertion point affects the realized elements. Update the index of the
            // elements after the insertion point.
            int start = Math.Max(realizedIndex, 0);

            for (int i = start; i < elementCount; ++i)
            {
                if (this._elements[i] is not { } element)
                {
                    continue;
                }

                int oldIndex = i + first;
                int newIndex = oldIndex + count;
                updateElementIndex(element, oldIndex, newIndex);
                this._elementToIndex[element] = newIndex;
            }

            if (realizedIndex < 0)
            {
                // The insertion point was before the first element, update the first index.
                this._firstIndex += count;
            }
            else
            {
                // The insertion point was within the realized elements, insert an empty space
                // in _elements and _sizes.
                this._elements.InsertMany(realizedIndex, null, count);
                this._sizes.InsertMany(realizedIndex, Size.Infinity, count);
            }
        }
    }

    /// <summary>
    /// Updates the elements in response to items being removed from the source collection.
    /// </summary>
    /// <param name="index">The index in the source collection of the remove.</param>
    /// <param name="count">The number of items removed.</param>
    /// <param name="updateElementIndex">A method used to update the element indexes.</param>
    /// <param name="recycleElement">A method used to recycle elements.</param>
    public void ItemsRemoved(
        int index,
        int count,
        Action<Control, int, int> updateElementIndex,
        Action<Control> recycleElement)
    {
        if (index < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(index));
        }

        int elementCount = this._elements.Count;
        if (elementCount == 0)
        {
            return;
        }

        // Get the removal start and end index within the realized _elements collection.
        int first = this._firstIndex;
        int last = first + elementCount - 1;
        int startIndex = index - first;
        int endIndex = (index + count) - first;

        if (endIndex < 0)
        {
            // The removed range was before the realized elements. Update the first index and
            // the indexes of the realized elements.
            this._firstIndex -= count;

            int newIndex = this._firstIndex;
            for (int i = 0; i < elementCount; ++i)
            {
                if (this._elements[i] is { } element)
                {
                    updateElementIndex(element, newIndex + count, newIndex);
                    this._elementToIndex[element] = newIndex;
                }

                ++newIndex;
            }
        }
        else if (startIndex < elementCount)
        {
            // Recycle and remove the affected elements.
            int start = Math.Max(startIndex, 0);
            int end = Math.Min(endIndex, elementCount);

            for (int i = start; i < end; ++i)
            {
                if (this._elements[i] is { } element)
                {
                    this._elements[i] = null;
                    this._elementToIndex.Remove(element);
                    recycleElement(element);
                }
            }

            this._elements.RemoveRange(start, end - start);
            this._sizes.RemoveRange(start, end - start);

            // If the remove started before and ended within our realized elements, then our new
            // first index will be the index where the remove started. Mark StartU as unstable
            // because we can't rely on it now to estimate element heights.
            if (startIndex <= 0 && end < last)
            {
                this._firstIndex = first = index;
            }

            // Update the indexes of the elements after the removed range.
            end = this._elements.Count;
            int newIndex = first + start;
            for (int i = start; i < end; ++i)
            {
                if (this._elements[i] is { } element)
                {
                    updateElementIndex(element, newIndex + count, newIndex);
                    this._elementToIndex[element] = newIndex;
                }

                ++newIndex;
            }
        }
    }

    /// <summary>
    /// Updates the elements in response to items being replaced in the source collection.
    /// </summary>
    /// <param name="index">The index in the source collection of the remove.</param>
    /// <param name="count">The number of items removed.</param>
    /// <param name="recycleElement">A method used to recycle elements.</param>
    public void ItemsReplaced(int index, int count, Action<Control> recycleElement)
    {
        if (index < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(index));
        }

        int elementCount = this._elements.Count;
        if (elementCount == 0)
        {
            return;
        }

        // Get the index within the realized _elements collection.
        int startIndex = index - this._firstIndex;
        int endIndex = Math.Min(startIndex + count, elementCount);

        if (startIndex >= 0 && endIndex > startIndex)
        {
            for (int i = startIndex; i < endIndex; ++i)
            {
                if (this._elements[i] is { } element)
                {
                    recycleElement(element);
                    this._elementToIndex.Remove(element);
                    this._elements[i] = null;
                    this._sizes[i] = Size.Infinity;
                }
            }
        }
    }

    /// <summary>
    /// Recycles all elements in response to the source collection being reset.
    /// </summary>
    /// <param name="recycleElement">A method used to recycle elements.</param>
    public void ItemsReset(Action<Control> recycleElement)
    {
        int count = this._elements.Count;
        if (count == 0)
        {
            return;
        }

        for (int i = 0; i < count; i++)
        {
            if (this._elements[i] is { } e)
            {
                this._elements[i] = null;
                this._elementToIndex.Remove(e);
                recycleElement(e);
            }
        }

        this._elements.Clear();
        this._sizes.Clear();
        this._elementToIndex.Clear();
    }

    /// <summary>
    /// Recycles elements before a specific index.
    /// </summary>
    /// <param name="index">The index in the source collection of new first element.</param>
    /// <param name="recycleElement">A method used to recycle elements.</param>
    public void RecycleElementsBefore(int index, Action<Control, int> recycleElement)
    {
        int count = this._elements.Count;
        int first = this._firstIndex;

        if (index <= first || count == 0)
        {
            return;
        }

        if (index > first + count - 1)
        {
            this.RecycleAllElements(recycleElement);
        }
        else
        {
            int endIndex = index - first;

            for (int i = 0; i < endIndex; ++i)
            {
                if (this._elements[i] is { } e)
                {
                    this._elements[i] = null;
                    this._elementToIndex.Remove(e);
                    recycleElement(e, i + first);
                }
            }

            this._elements.RemoveRange(0, endIndex);
            this._sizes.RemoveRange(0, endIndex);
            this._firstIndex = index;
        }
    }

    /// <summary>
    /// Recycles elements after a specific index.
    /// </summary>
    /// <param name="index">The index in the source collection of new last element.</param>
    /// <param name="recycleElement">A method used to recycle elements.</param>
    public void RecycleElementsAfter(int index, Action<Control, int> recycleElement)
    {
        int count = this._elements.Count;
        int first = this._firstIndex;

        if (index >= first + count - 1 || count == 0)
        {
            return;
        }

        if (index < first)
        {
            this.RecycleAllElements(recycleElement);
        }
        else
        {
            int startIndex = (index + 1) - first;

            for (int i = startIndex; i < count; ++i)
            {
                if (this._elements[i] is { } e)
                {
                    this._elements[i] = null;
                    this._elementToIndex.Remove(e);
                    recycleElement(e, i + first);
                }
            }

            int removeCount = count - startIndex;
            this._elements.RemoveRange(startIndex, removeCount);
            this._sizes.RemoveRange(startIndex, removeCount);
        }
    }

    /// <summary>
    /// Recycles all realized elements.
    /// </summary>
    /// <param name="recycleElement">A method used to recycle elements.</param>
    public void RecycleAllElements(Action<Control, int> recycleElement)
    {
        int count = this._elements.Count;
        if (count == 0)
        {
            return;
        }

        int first = this._firstIndex;
        for (int i = 0; i < count; i++)
        {
            if (this._elements[i] is { } e)
            {
                this._elements[i] = null;
                this._elementToIndex.Remove(e);
                recycleElement(e, i + first);
            }
        }

        this._firstIndex = 0;
        this._elements.Clear();
        this._sizes.Clear();
        this._elementToIndex.Clear();
    }

    /// <summary>
    /// Resets the element list and prepares it for reuse.
    /// </summary>
    public void ResetForReuse()
    {
        this._firstIndex = 0;
        this._elements.Clear();
        this._sizes.Clear();
        this._elementToIndex.Clear();
    }
}
