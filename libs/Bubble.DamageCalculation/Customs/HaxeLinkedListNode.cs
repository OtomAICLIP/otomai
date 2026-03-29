namespace Bubble.DamageCalculation.Customs;

public class HaxeLinkedListNode<T>
{
    public HaxeLinkedListNode<T>? Previous { get; set; }
    public HaxeLinkedListNode<T>? Next { get; set; }
    public T Item { get; set; }

    public HaxeLinkedListNode(T item)
    {
        Next     = null;
        Previous = null;
        Item     = item;
    }
}