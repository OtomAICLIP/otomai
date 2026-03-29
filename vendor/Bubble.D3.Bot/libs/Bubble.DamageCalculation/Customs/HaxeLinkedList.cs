using System.Collections;

namespace Bubble.DamageCalculation.Customs;

public class HaxeLinkedList<T> : IEnumerable<T>
{
    public HaxeLinkedListNode<T>? Tail { get; set; }
    public HaxeLinkedListNode<T>? Head { get; set; }

    public HaxeLinkedList()
    {
        Tail = null;
        Head = null;
    }

    /// <summary>
    /// Removes a node from the linked list.
    /// </summary>
    /// <param name="node">The node to remove.</param>
    public void Remove(HaxeLinkedListNode<T> node)
    {
        if (node.Previous != null)
        {
            node.Previous.Next = node.Next;
        }

        if (node.Next != null)
        {
            node.Next.Previous = node.Previous;
        }

        if (node == Head)
        {
            Head = node.Next;
        }

        if (node == Tail)
        {
            Tail = node.Previous;
        }
    }

    /// <summary>
    /// Removes a node from the linked list.
    /// </summary>
    /// <param name="item">The item to remove</param>
    public void Remove(T item)
    {
        var currentNode = Head;
        while (currentNode != null)
        {
            if (currentNode.Item != null && currentNode.Item.Equals(item))
            {
                Remove(currentNode);
                return;
            }

            currentNode = currentNode.Next;
        }
    }
    /// <summary>
    /// Creates a shallow copy of the linked list.
    /// </summary>
    /// <returns>A new LinkedList with the same elements.</returns>
    public HaxeLinkedList<T> Copy()
    {
        HaxeLinkedList<T> newList     = new();
        var               currentNode = Head;
        while (currentNode != null)
        {
            newList.Add(currentNode.Item);
            currentNode = currentNode.Next;
        }

        return newList;
    }

    public HaxeLinkedList<T> Concat(IEnumerable<T> second)
    {
        HaxeLinkedList<T> newList = new();
        foreach (var item in second)
        {
            newList.Add(item);
        }

        return newList;
    }

    public HaxeLinkedList<T> Concat(HaxeLinkedList<T> second)
    {
        HaxeLinkedList<T> newList = new();
        // need to add the current items
        var currentNode = Head;
        while (currentNode != null)
        {
            newList.Add(currentNode.Item);
            currentNode = currentNode.Next;
        }

        foreach (var item in second)
        {
            newList.Add(item);
        }

        return newList;
    }
    
    /// <summary>
    /// Clears the linked list.
    /// </summary>
    public void Clear()
    {
        Head = null;
        Tail = null;
    }

    /// <summary>
    /// Appends another linked list to this one.
    /// </summary>
    /// <param name="list">The linked list to append.</param>
    /// <returns>The modified linked list.</returns>
    public HaxeLinkedList<T> Append(HaxeLinkedList<T> list)
    {
        var currentNode = list.Head;
        while (currentNode != null)
        {
            Add(currentNode.Item);
            currentNode = currentNode.Next;
        }

        return this;
    }

    public HaxeLinkedList<T> Append(IList<T> list)
    {
        foreach (var item in list)
        {
            Add(item);
        }

        return this;
    }

    /// <summary>
    /// Adds an item to the linked list.
    /// </summary>
    /// <param name="item">The item to add.</param>
    /// <returns>The new LinkedListNode containing the item.</returns>
    public HaxeLinkedListNode<T> Add(T item)
    {
        var newNode = new HaxeLinkedListNode<T>(item);
        if (Head == null)
        {
            Head = newNode;
        }

        if (Tail == null)
        {
            Tail = newNode;
        }
        else
        {
            newNode.Previous = Tail;
            Tail.Next        = newNode;
            Tail             = newNode;
        }

        return newNode;
    }

    public bool Any(Func<T, bool> func)
    {
        var currentNode = Head;
        while (currentNode != null)
        {
            if (func(currentNode.Item))
            {
                return true;
            }

            currentNode = currentNode.Next;
        }

        return false;
    }

    /// <summary>
    /// Returns an enumerator that iterates through the linked list.
    /// </summary>
    /// <returns>An enumerator for the linked list.</returns>
    public IEnumerator<T> GetEnumerator()
    {
        var currentNode = Head;
        while (currentNode != null)
        {
            yield return currentNode.Item;
            currentNode = currentNode.Next;
        }
    }
    
    /// <summary>
    /// Returns an enumerator that iterates through the linked list (non-generic version).
    /// </summary>
    /// <returns>An enumerator for the linked list.</returns>
    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

}