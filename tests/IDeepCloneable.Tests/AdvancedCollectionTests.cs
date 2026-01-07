using System.Collections.Generic;
using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.Linq;

namespace IDeepCloneable.Tests;

/// <summary>
/// Tests for advanced collection types (Stack, Queue, HashSet, Immutable collections, etc.).
/// </summary>
public class AdvancedCollectionTests
{
    [Fact]
    public void DeepClone_Stack_ClonesCorrectly()
    {
        var original = new ClassWithStack
        {
            Name = "Test",
            Items = new Stack<int>(new[] { 1, 2, 3 }),
        };

        var clone = original.DeepClone();

        clone.ShouldNotBeSameAs(original);
        clone.Items.ShouldNotBeNull();
        clone.Items.ShouldNotBeSameAs(original.Items);
        clone.Items.Count.ShouldBe(3);

        clone.Items.Pop().ShouldBe(3);
        clone.Items.Pop().ShouldBe(2);
        clone.Items.Pop().ShouldBe(1);
    }

    [Fact]
    public void DeepClone_Queue_ClonesCorrectly()
    {
        var original = new ClassWithQueue
        {
            Name = "Test",
            Items = new Queue<int>(new[] { 1, 2, 3 }),
        };

        var clone = original.DeepClone();

        clone.ShouldNotBeSameAs(original);
        clone.Items.ShouldNotBeNull();
        clone.Items.ShouldNotBeSameAs(original.Items);
        clone.Items.Count.ShouldBe(3);

        clone.Items.Dequeue().ShouldBe(1);
        clone.Items.Dequeue().ShouldBe(2);
        clone.Items.Dequeue().ShouldBe(3);
    }

    [Fact]
    public void DeepClone_HashSet_ClonesCorrectly()
    {
        var original = new ClassWithHashSet
        {
            Name = "Test",
            Items = new HashSet<int> { 1, 2, 3, 4, 5 },
        };

        var clone = original.DeepClone();

        clone.ShouldNotBeSameAs(original);
        clone.Items.ShouldNotBeNull();
        clone.Items.ShouldNotBeSameAs(original.Items);
        clone.Items.Count.ShouldBe(5);
        clone.Items.ShouldBe(new HashSet<int> { 1, 2, 3, 4, 5 });
    }

    [Fact]
    public void DeepClone_SortedSet_ClonesCorrectly()
    {
        var original = new ClassWithSortedSet
        {
            Name = "Test",
            Items = new SortedSet<int> { 3, 1, 4, 1, 5, 9 },
        };

        var clone = original.DeepClone();

        clone.ShouldNotBeSameAs(original);
        clone.Items.ShouldNotBeNull();
        clone.Items.ShouldNotBeSameAs(original.Items);
        clone.Items.Count.ShouldBe(5);
        clone.Items.ToArray().ShouldBe(new[] { 1, 3, 4, 5, 9 });
    }

    [Fact]
    public void DeepClone_ObservableCollection_ClonesCorrectly()
    {
        var original = new ClassWithObservableCollection
        {
            Name = "Test",
            Items = new ObservableCollection<int> { 1, 2, 3 },
        };

        var clone = original.DeepClone();

        clone.ShouldNotBeSameAs(original);
        clone.Items.ShouldNotBeNull();
        clone.Items.ShouldNotBeSameAs(original.Items);
        clone.Items.Count.ShouldBe(3);
        clone.Items.ShouldBe(new[] { 1, 2, 3 });
    }

    [Fact]
    public void DeepClone_ReadOnlyCollection_ClonesCorrectly()
    {
        var original = new ClassWithReadOnlyCollection
        {
            Name = "Test",
            Items = new ReadOnlyCollection<int>(new List<int> { 1, 2, 3 }),
        };

        var clone = original.DeepClone();

        clone.ShouldNotBeSameAs(original);
        clone.Items.ShouldNotBeNull();
        clone.Items.ShouldNotBeSameAs(original.Items);
        clone.Items.Count.ShouldBe(3);
        clone.Items.ShouldBe(new[] { 1, 2, 3 });
    }

    [Fact]
    public void DeepClone_ImmutableList_ClonesCorrectly()
    {
        var original = new ClassWithImmutableList
        {
            Name = "Test",
            Items = ImmutableList.Create(1, 2, 3),
        };

        var clone = original.DeepClone();

        clone.ShouldNotBeSameAs(original);
        clone.Items.ShouldBeSameAs(original.Items);
        clone.Items.ShouldNotBeNull();
        clone.Items.Count.ShouldBe(3);
        clone.Items.ShouldBe(new[] { 1, 2, 3 });
    }

    [Fact]
    public void DeepClone_ImmutableArray_ClonesCorrectly()
    {
        var original = new ClassWithImmutableArray
        {
            Name = "Test",
            Items = ImmutableArray.Create(1, 2, 3),
        };

        var clone = original.DeepClone();

        clone.ShouldNotBeSameAs(original);
        clone.Items.ShouldNotBeSameAs(original.Items);
        clone.Items.IsDefault.ShouldBeFalse();
        clone.Items.Length.ShouldBe(3);
        clone.Items.ShouldBe(new[] { 1, 2, 3 });
    }

    [Fact]
    public void DeepClone_ImmutableHashSet_ClonesCorrectly()
    {
        var original = new ClassWithImmutableHashSet
        {
            Name = "Test",
            Items = ImmutableHashSet.Create(1, 2, 3),
        };

        var clone = original.DeepClone();

        clone.ShouldNotBeSameAs(original);
        clone.Items.ShouldBeSameAs(original.Items);
        clone.Items.ShouldNotBeNull();
        clone.Items.Count.ShouldBe(3);
        clone.Items.OrderBy(x => x).ShouldBe(new[] { 1, 2, 3 });
    }

    [Fact]
    public void DeepClone_ImmutableDictionary_ClonesCorrectly()
    {
        var original = new ClassWithImmutableDictionary
        {
            Name = "Test",
            Items = ImmutableDictionary
                .Create<string, int>()
                .Add("one", 1)
                .Add("two", 2)
                .Add("three", 3),
        };

        var clone = original.DeepClone();

        clone.ShouldNotBeSameAs(original);
        clone.Items.ShouldBeSameAs(original.Items);
        clone.Items.ShouldNotBeNull();
        clone.Items.Count.ShouldBe(3);
        clone.Items["one"].ShouldBe(1);
        clone.Items["two"].ShouldBe(2);
        clone.Items["three"].ShouldBe(3);
    }

    [Fact]
    public void DeepClone_StackOfCloneables_CreatesDeepCopy()
    {
        var original = new ClassWithCloneableStack
        {
            Name = "Test",
            Items = new Stack<SimpleClass>(
                new[]
                {
                    new SimpleClass { Name = "Item3", Age = 3 },
                    new SimpleClass { Name = "Item2", Age = 2 },
                    new SimpleClass { Name = "Item1", Age = 1 },
                }
            ),
        };

        var clone = original.DeepClone();

        clone.ShouldNotBeSameAs(original);
        clone.Items.ShouldNotBeNull();
        clone.Items.ShouldNotBeSameAs(original.Items);

        var clonedItem = clone.Items.Pop();
        clonedItem.ShouldNotBeNull();
        clonedItem.Name.ShouldBe("Item1");

        clonedItem.Name = "Modified";

        original.Items.Peek().Name.ShouldBe("Item1");
    }
}

[DeepCloneable]
public partial class ClassWithStack
{
    public string Name { get; set; } = string.Empty;
    public Stack<int>? Items { get; set; }
}

[DeepCloneable]
public partial class ClassWithQueue
{
    public string Name { get; set; } = string.Empty;
    public Queue<int>? Items { get; set; }
}

[DeepCloneable]
public partial class ClassWithHashSet
{
    public string Name { get; set; } = string.Empty;
    public HashSet<int>? Items { get; set; }
}

[DeepCloneable]
public partial class ClassWithSortedSet
{
    public string Name { get; set; } = string.Empty;
    public SortedSet<int>? Items { get; set; }
}

[DeepCloneable]
public partial class ClassWithObservableCollection
{
    public string Name { get; set; } = string.Empty;
    public ObservableCollection<int>? Items { get; set; }
}

[DeepCloneable]
public partial class ClassWithReadOnlyCollection
{
    public string Name { get; set; } = string.Empty;
    public ReadOnlyCollection<int>? Items { get; set; }
}

[DeepCloneable]
public partial class ClassWithImmutableList
{
    public string Name { get; set; } = string.Empty;
    public ImmutableList<int>? Items { get; set; }
}

[DeepCloneable]
public partial class ClassWithImmutableArray
{
    public string Name { get; set; } = string.Empty;
    public ImmutableArray<int> Items { get; set; }
}

[DeepCloneable]
public partial class ClassWithImmutableHashSet
{
    public string Name { get; set; } = string.Empty;
    public ImmutableHashSet<int>? Items { get; set; }
}

[DeepCloneable]
public partial class ClassWithImmutableDictionary
{
    public string Name { get; set; } = string.Empty;
    public ImmutableDictionary<string, int>? Items { get; set; }
}

[DeepCloneable]
public partial class ClassWithCloneableStack
{
    public string Name { get; set; } = string.Empty;
    public Stack<SimpleClass>? Items { get; set; }
}
