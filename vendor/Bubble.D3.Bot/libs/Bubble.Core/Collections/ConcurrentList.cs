using System.Collections;

namespace Bubble.Core.Collections;

public sealed class ConcurrentList<T> : IList<T>
{
    private const int DefaultTimeout = -1;

    private readonly List<T> _innerList;
    private readonly Lazy<ReaderWriterLockSlim> _locker;

    private ReaderWriterLockSlim Locker => _locker.Value;

    public T this[int index]
    {
        get
        {
            Locker.TryEnterReadLock(DefaultTimeout);
            try
            {
                return _innerList[index];
            }
            finally
            {
                Locker.ExitReadLock();
            }
        }
        set
        {
            Locker.TryEnterWriteLock(DefaultTimeout);
            try
            {
                _innerList[index] = value;
            }
            finally
            {
                Locker.ExitWriteLock();
            }
        }
    }

    public int Count
    {
        get
        {
            Locker.TryEnterReadLock(DefaultTimeout);
            try
            {
                return _innerList.Count;
            }
            finally
            {
                Locker.ExitReadLock();
            }
        }
    }

    public bool IsReadOnly
    {
        get
        {
            Locker.TryEnterReadLock(DefaultTimeout);
            try
            {
                return ((ICollection<T>)_innerList).IsReadOnly;
            }
            finally
            {
                Locker.ExitReadLock();
            }
        }
    }

    public ConcurrentList()
    {
        _locker = new Lazy<ReaderWriterLockSlim>(() => new ReaderWriterLockSlim());
        _innerList = [];
    }

    public ConcurrentList(List<T> list)
    {
        _locker = new Lazy<ReaderWriterLockSlim>(() => new ReaderWriterLockSlim());
        _innerList = list;
    }

    public void AddRange(IEnumerable<T> range)
    {
        Locker.TryEnterWriteLock(DefaultTimeout);
        try
        {
            _innerList.AddRange(range);
        }
        finally
        {
            Locker.ExitWriteLock();
        }
    }

    public void RemoveAll(Predicate<T> match)
    {
        Locker.TryEnterReadLock(DefaultTimeout);
        try
        {
            _innerList.RemoveAll(match);
        }
        finally
        {
            Locker.ExitReadLock();
        }
    }

    public int IndexOf(T item)
    {
        Locker.TryEnterReadLock(DefaultTimeout);
        try
        {
            return _innerList.IndexOf(item);
        }
        finally
        {
            Locker.ExitReadLock();
        }
    }

    public void Insert(int index, T item)
    {
        Locker.TryEnterWriteLock(DefaultTimeout);
        try
        {
            _innerList.Insert(index, item);
        }
        finally
        {
            Locker.ExitWriteLock();
        }
    }

    public void RemoveAt(int index)
    {
        Locker.TryEnterWriteLock(DefaultTimeout);
        try
        {
            _innerList.RemoveAt(index);
        }
        finally
        {
            Locker.ExitWriteLock();
        }
    }

    public void Add(T item)
    {
        Locker.TryEnterWriteLock(DefaultTimeout);
        try
        {
            _innerList.Add(item);
        }
        finally
        {
            Locker.ExitWriteLock();
        }
    }


    public void Clear()
    {
        Locker.TryEnterWriteLock(DefaultTimeout);
        try
        {
            _innerList.Clear();
        }
        finally
        {
            Locker.ExitWriteLock();
        }
    }

    public bool Contains(T item)
    {
        Locker.TryEnterReadLock(DefaultTimeout);
        try
        {
            return _innerList.Contains(item);
        }
        finally
        {
            Locker.ExitReadLock();
        }
    }

    public void CopyTo(T[] array, int arrayIndex)
    {
        Locker.TryEnterWriteLock(DefaultTimeout);
        try
        {
            _innerList.CopyTo(array, arrayIndex);
        }
        finally
        {
            Locker.ExitWriteLock();
        }
    }

    public bool Remove(T item)
    {
        Locker.TryEnterWriteLock(DefaultTimeout);
        try
        {
            return _innerList.Remove(item);
        }
        finally
        {
            Locker.ExitWriteLock();
        }
    }

    public IEnumerator<T> GetEnumerator()
    {
        Locker.TryEnterReadLock(DefaultTimeout);
        try
        {
            foreach (var item in _innerList) yield return item;
        }
        finally
        {
            Locker.ExitReadLock();
        }
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        Locker.TryEnterReadLock(DefaultTimeout);
        try
        {
            foreach (var item in _innerList) yield return item;
        }
        finally
        {
            Locker.ExitReadLock();
        }
    }
}