using System.Collections;

namespace Bubble.Core.Collections;

public class LimitedStack<T> : IEnumerable<T>
{
    private readonly int _capacity;
    private readonly List<T> _list;

    public int Count => _list.Count;

    public LimitedStack(int capacity)
    {
        if (capacity <= 0) throw new ArgumentOutOfRangeException(nameof(capacity), "Capacity must be greater than zero.");

        _capacity = capacity;
        _list = new List<T>(capacity);
    }

    public void Clear()
    {
        _list.Clear();
    }

    public bool IsEmpty()
    {
        return _list.Count == 0;
    }

    public T Peek()
    {
        if (_list.Count == 0) throw new InvalidOperationException("The stack is empty.");

        return _list[^1];
    }

    public T Pop()
    {
        if (_list.Count == 0) throw new InvalidOperationException("The stack is empty.");

        var lastIndex = _list.Count - 1;
        var item = _list[lastIndex];
        _list.RemoveAt(lastIndex);
        return item;
    }

    public void Push(T item)
    {
        if (_list.Count >= _capacity) _list.RemoveAt(0);

        _list.Add(item);
    }

    public T[] ToArray()
    {
        return _list.ToArray();
    }

    public IEnumerator<T> GetEnumerator()
    {
        return _list.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}