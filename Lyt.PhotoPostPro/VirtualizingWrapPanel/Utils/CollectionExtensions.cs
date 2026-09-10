namespace Avalonia.Labs.Controls.Utils;

internal static class CollectionExtensions
{
    internal static void InsertMany<T>(this List<T> list, int index, T item, int count)
    {
        if (count < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(count));
        }

        if (count == 0)
        {
            return;
        }

        list.InsertRange(index, new RepeatCollection<T>(item, count));
    }

    private sealed class RepeatCollection<T> : ICollection<T>
    {
        private readonly T _item;

        public RepeatCollection(T item, int count)
        {
            if (count < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(count));
            }

            this._item = item;
            this.Count = count;
        }

        public int Count { get; }
        public bool IsReadOnly => true;

        public void Add(T item) => throw new NotSupportedException();
        public void Clear() => throw new NotSupportedException();
        public bool Contains(T item) => throw new NotSupportedException();
        public bool Remove(T item) => throw new NotSupportedException();

        IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();

        public IEnumerator<T> GetEnumerator()
        {
            for (int i = 0; i < this.Count; i++)
            {
                yield return this._item;
            }
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            if (array is null)
            {
                throw new ArgumentNullException(nameof(array));
            }

            if ((uint)arrayIndex > (uint)array.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(arrayIndex));
            }

            if (array.Length - arrayIndex < this.Count)
            {
                throw new ArgumentException("Destination array is not long enough.", nameof(array));
            }

            int end = arrayIndex + this.Count;
            for (int i = arrayIndex; i < end; i++)
            {
                array[i] = this._item;
            }
        }
    }
}