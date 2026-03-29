using System.Collections;

namespace Bubble.Core.Collections;

public interface IPriorityQueue<T>
{
    T? Peek();
    T Pop();
    int Push(T item);
    void Update(int i);
}

public class PriorityQueueB<T> : IPriorityQueue<T>, IEnumerable<T>
{
    private readonly IComparer<T> _comparer;
    private readonly List<T> _innerList = [];

    public int Count => _innerList.Count;

    public T this[int index]
    {
        get => _innerList[index];
        set
        {
            _innerList[index] = value;
            Update(index);
        }
    }

    public PriorityQueueB()
    {
        _comparer = Comparer<T>.Default;
    }

    public PriorityQueueB(IComparer<T> comparer)
    {
        _comparer = comparer;
    }

    public PriorityQueueB(IComparer<T> comparer, int capacity)
    {
        _comparer = comparer;
        _innerList.Capacity = capacity;
    }

    public void Clear()
    {
        _innerList.Clear();
    }

    protected virtual int OnCompare(int i, int j)
    {
        return _comparer.Compare(_innerList[i], _innerList[j]);
    }

    public void Remove(T item)
    {
        var index = -1;
        for (var i = 0; i < _innerList.Count; i++)
            if (_comparer.Compare(_innerList[i], item) == 0)
                index = i;

        if (index != -1) _innerList.RemoveAt(index);
    }

    public void SwitchElements(int i, int j)
    {
        (_innerList[i], _innerList[j]) = (_innerList[j], _innerList[i]);
    }

    public IEnumerator<T> GetEnumerator()
    {
        return _innerList.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    public int Push(T item)
    {
        var p = _innerList.Count;
        _innerList.Add(item); // E[p] = O
        do
        {
            if (p == 0) break;

            var p2 = (p - 1) / 2;
            if (OnCompare(p, p2) < 0)
            {
                SwitchElements(p, p2);
                p = p2;
            }
            else
                break;
        }
        while (true);

        return p;
    }

    public T Pop()
    {
        var result = _innerList[0];
        var p = 0;
        _innerList[0] = _innerList[^1];
        _innerList.RemoveAt(_innerList.Count - 1);
        do
        {
            var pn = p;
            var p1 = 2 * p + 1;
            var p2 = 2 * p + 2;

            if (_innerList.Count > p1 && OnCompare(p, p1) > 0) p = p1;

            if (_innerList.Count > p2 && OnCompare(p, p2) > 0) p = p2;

            if (p == pn) break;

            SwitchElements(p, pn);
        }
        while (true);

        return result;
    }

    public void Update(int i)
    {
        var p = i;
        int p2;
        do
        {
            if (p == 0) break;

            p2 = (p - 1) / 2;
            if (OnCompare(p, p2) < 0)
            {
                SwitchElements(p, p2);
                p = p2;
            }
            else
                break;
        }
        while (true);

        if (p < i) return;

        do
        {
            var pn = p;
            var p1 = 2 * p + 1;
            p2 = 2 * p + 2;
            if (_innerList.Count > p1 && OnCompare(p, p1) > 0) p = p1;

            if (_innerList.Count > p2 && OnCompare(p, p2) > 0) p = p2;

            if (p == pn) break;

            SwitchElements(p, pn);
        }
        while (true);
    }

    public T? Peek()
    {
        return _innerList.Count > 0 ? _innerList[0] : default;
    }
}