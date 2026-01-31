using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace IDeepCloneable.Tests;

/// <summary>
/// Tests for collection cloning functionality.
/// </summary>
public class CollectionCloneTests
{
    [Fact]
    public void DeepClone_ListOfCloneables_CreatesDeepCopy()
    {
        var original = new ClassWithList
        {
            Name = "Parent",
            Items = [new() { Value = "Item1" }, new() { Value = "Item2" }],
        };

        var clone = original.DeepClone();

        clone.ShouldNotBeSameAs(original);
        clone.Items!.ShouldNotBeSameAs(original.Items);
        clone.Items!.Count.ShouldBe(2);

        clone.Items![0].ShouldNotBeSameAs(original.Items![0]);
        clone.Items![0].Value.ShouldBe("Item1");
        clone.Items![1].ShouldNotBeSameAs(original.Items[1]);
        clone.Items![1].Value.ShouldBe("Item2");
    }

    [Fact]
    public void DeepClone_ListModification_DoesNotAffectOriginal()
    {
        var original = new ClassWithList
        {
            Name = "Parent",
            Items = [new() { Value = "Original" }],
        };

        var clone = original.DeepClone();
        clone.Items![0].Value = "Modified";

        original.Items![0].Value.ShouldBe("Original");
    }

    [Fact]
    public void DeepClone_NullList_HandlesCorrectly()
    {
        var original = new ClassWithList { Name = "Parent", Items = null };

        var clone = original.DeepClone();

        clone.ShouldNotBeSameAs(original);
        clone.Items.ShouldBeNull();
    }

    [Fact]
    public void DeepClone_ListOfValueTypes_CreatesNewList()
    {
        var original = new ClassWithValueList { Name = "Parent", Numbers = [1, 2, 3] };

        var clone = original.DeepClone();

        clone.ShouldNotBeSameAs(original);
        clone.Numbers!.ShouldNotBeSameAs(original.Numbers);
        clone.Numbers!.ShouldBe([1, 2, 3]);
    }

    [Fact]
    public void DeepClone_PriorityQueue_CreatesDeepCopy()
    {
        var original = new ClassWithPriorityQueue { Items = new PriorityQueue<string, int>() };
        original.Items.Enqueue("Low", 10);
        original.Items.Enqueue("High", 1);
        original.Items.Enqueue("Medium", 5);

        var clone = original.DeepClone();

        clone.ShouldNotBeSameAs(original);
        clone.Items.ShouldNotBeNull();
        clone.Items.ShouldNotBeSameAs(original.Items);
        clone.Items.Count.ShouldBe(3);
        clone.Items.Dequeue().ShouldBe("High");
    }

    [Fact]
    public void DeepClone_BlockingCollection_CreatesDeepCopy()
    {
        var original = new ClassWithBlockingCollection { Items = new BlockingCollection<int>() };
        original.Items.Add(1);
        original.Items.Add(2);
        original.Items.Add(3);

        var clone = original.DeepClone();

        clone.ShouldNotBeSameAs(original);
        clone.Items.ShouldNotBeNull();
        clone.Items.ShouldNotBeSameAs(original.Items);
        clone.Items.Count.ShouldBe(3);
        clone.Items.Take().ShouldBe(1);
    }

    [Fact]
    public void DeepClone_ConcurrentStack_CreatesDeepCopy()
    {
        var original = new ClassWithConcurrentStack { Items = new ConcurrentStack<int>() };
        original.Items.Push(1);
        original.Items.Push(2);
        original.Items.Push(3);

        var clone = original.DeepClone();

        clone.ShouldNotBeSameAs(original);
        clone.Items.ShouldNotBeNull();
        clone.Items.ShouldNotBeSameAs(original.Items);
        clone.Items.Count.ShouldBe(3);
    }

    [Fact]
    public void DeepClone_ConcurrentQueue_CreatesDeepCopy()
    {
        var original = new ClassWithConcurrentQueue { Items = new ConcurrentQueue<int>() };
        original.Items.Enqueue(1);
        original.Items.Enqueue(2);
        original.Items.Enqueue(3);

        var clone = original.DeepClone();

        clone.ShouldNotBeSameAs(original);
        clone.Items.ShouldNotBeNull();
        clone.Items.ShouldNotBeSameAs(original.Items);
        clone.Items.Count.ShouldBe(3);
    }

    [Fact]
    public void DeepClone_LinkedList_CreatesDeepCopy()
    {
        var original = new ClassWithLinkedList { Items = new LinkedList<int>() };
        original.Items.AddLast(1);
        original.Items.AddLast(2);
        original.Items.AddLast(3);

        var clone = original.DeepClone();

        clone.ShouldNotBeSameAs(original);
        clone.Items.ShouldNotBeNull();
        clone.Items.ShouldNotBeSameAs(original.Items);
        clone.Items.Count.ShouldBe(3);
    }
}

[DeepCloneable]
public partial class ClassWithList
{
    public string Name { get; set; } = string.Empty;
    public List<NestedClass>? Items { get; set; }
}

[DeepCloneable]
public partial class ClassWithValueList
{
    public string Name { get; set; } = string.Empty;
    public List<int>? Numbers { get; set; }
}

[DeepCloneable]
public partial class ClassWithPriorityQueue
{
    public PriorityQueue<string, int>? Items { get; set; }
}

[DeepCloneable]
public partial class ClassWithBlockingCollection
{
    public BlockingCollection<int>? Items { get; set; }
}

[DeepCloneable]
public partial class ClassWithConcurrentStack
{
    public ConcurrentStack<int>? Items { get; set; }
}

[DeepCloneable]
public partial class ClassWithConcurrentQueue
{
    public ConcurrentQueue<int>? Items { get; set; }
}

[DeepCloneable]
public partial class ClassWithLinkedList
{
    public LinkedList<int>? Items { get; set; }
}
