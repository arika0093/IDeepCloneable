using System;
using System.Collections.Generic;

namespace IDeepCloneable.Tests;

/// <summary>
/// Tests for edge cases including circular references, required properties, copy constructors, and array optimizations.
/// </summary>
public class EdgeCaseTests
{
    [Fact]
    public void DeepClone_ClassWithRequiredProperty_ClonesCorrectly()
    {
        // Arrange: Create a class with required properties
        var original = new ClassWithRequiredProperty
        {
            RequiredName = "Test",
            OptionalValue = 42,
        };

        // Act: Clone the object
        var clone = original.DeepClone();

        // Assert: Verify it's a different instance
        clone.ShouldNotBeSameAs(original);
        clone.RequiredName.ShouldBe("Test");
        clone.OptionalValue.ShouldBe(42);

        // Verify modifying clone doesn't affect original
        clone.RequiredName = "Modified";
        original.RequiredName.ShouldBe("Test");
    }

    [Fact]
    public void DeepClone_ClassWithCopyConstructor_UsesCopyConstructor()
    {
        // Arrange: Create a class with a copy constructor
        var original = new ClassWithCopyConstructor
        {
            Name = "Original",
            Value = 100,
        };

        // Act: Clone the object
        var clone = original.DeepClone();

        // Assert: Verify it's a different instance
        clone.ShouldNotBeSameAs(original);
        clone.Name.ShouldBe("Original");
        clone.Value.ShouldBe(100);

        // Verify modifying clone doesn't affect original
        clone.Name = "Modified";
        original.Name.ShouldBe("Original");
    }

    [Fact]
    public void DeepClone_CircularReferenceSimple_HandlesCorrectly()
    {
        // Arrange: Create a simple circular reference
        var node1 = new CircularNode { Name = "Node1", Value = 1 };
        var node2 = new CircularNode { Name = "Node2", Value = 2 };
        node1.Next = node2;
        node2.Next = node1;

        // Act: Clone the object
        var cloneNode1 = node1.DeepClone();

        // Assert: Verify structure is preserved
        cloneNode1.ShouldNotBeSameAs(node1);
        cloneNode1.Name.ShouldBe("Node1");
        cloneNode1.Value.ShouldBe(1);
        
        cloneNode1.Next.ShouldNotBeNull();
        cloneNode1.Next.ShouldNotBeSameAs(node2);
        cloneNode1.Next!.Name.ShouldBe("Node2");
        cloneNode1.Next.Value.ShouldBe(2);
        
        // Verify circular reference is preserved
        cloneNode1.Next.Next.ShouldNotBeNull();
        cloneNode1.Next.Next.ShouldBeSameAs(cloneNode1); // Should point back to cloneNode1
    }

    [Fact]
    public void DeepClone_CircularReferenceComplex_HandlesCorrectly()
    {
        // Arrange: Create a complex circular reference with three nodes
        var node1 = new CircularNode { Name = "Node1", Value = 1 };
        var node2 = new CircularNode { Name = "Node2", Value = 2 };
        var node3 = new CircularNode { Name = "Node3", Value = 3 };
        node1.Next = node2;
        node2.Next = node3;
        node3.Next = node1; // Circle back to node1

        // Act: Clone the object
        var cloneNode1 = node1.DeepClone();

        // Assert: Verify structure is preserved
        cloneNode1.ShouldNotBeSameAs(node1);
        cloneNode1.Name.ShouldBe("Node1");
        
        var cloneNode2 = cloneNode1.Next;
        cloneNode2.ShouldNotBeNull();
        cloneNode2.ShouldNotBeSameAs(node2);
        cloneNode2!.Name.ShouldBe("Node2");
        
        var cloneNode3 = cloneNode2.Next;
        cloneNode3.ShouldNotBeNull();
        cloneNode3.ShouldNotBeSameAs(node3);
        cloneNode3!.Name.ShouldBe("Node3");
        
        // Verify circular reference is preserved
        cloneNode3.Next.ShouldNotBeNull();
        cloneNode3.Next.ShouldBeSameAs(cloneNode1); // Should circle back to cloneNode1
    }

    [Fact]
    public void DeepClone_SelfReferencing_HandlesCorrectly()
    {
        // Arrange: Create a self-referencing node
        var node = new CircularNode { Name = "SelfRef", Value = 42 };
        node.Next = node; // Points to itself

        // Act: Clone the object
        var clone = node.DeepClone();

        // Assert: Verify it's a different instance but still self-referencing
        clone.ShouldNotBeSameAs(node);
        clone.Name.ShouldBe("SelfRef");
        clone.Value.ShouldBe(42);
        clone.Next.ShouldNotBeNull();
        clone.Next.ShouldBeSameAs(clone); // Should point to itself
    }

    [Fact]
    public void DeepClone_PrimitiveArray_UsesOptimizedPath()
    {
        // Arrange: Create an array of primitives
        var original = new int[] { 1, 2, 3, 4, 5 };

        // Act: Clone the array (this should use AsSpan().ToArray() internally)
        var clone = new ClassWithPrimitiveArray { Numbers = original }.DeepClone().Numbers;

        // Assert: Verify it's a different instance
        clone.ShouldNotBeSameAs(original);
        clone.ShouldBe(original);

        // Verify modifying clone doesn't affect original
        clone[0] = 999;
        original[0].ShouldBe(1);
    }

    [Fact]
    public void DeepClone_IEnumerableCustomType_ClonesCorrectly()
    {
        // Arrange: Create a class using IEnumerable
        var original = new ClassWithIEnumerable
        {
            Items = new CustomEnumerable<int>(new[] { 1, 2, 3, 4, 5 }),
        };

        // Act: Clone the object
        var clone = original.DeepClone();

        // Assert: Verify it's a different instance
        clone.ShouldNotBeSameAs(original);
        clone.Items.ShouldNotBeSameAs(original.Items);
        
        // Verify values
        var cloneList = new List<int>(clone.Items);
        var originalList = new List<int>(original.Items);
        cloneList.ShouldBe(originalList);
    }

    [Fact]
    public void DeepClone_MultipleRequiredProperties_ClonesCorrectly()
    {
        // Arrange: Create a class with multiple required properties
        var original = new ClassWithMultipleRequired
        {
            RequiredName = "Test",
            RequiredId = 123,
            OptionalDescription = "Description",
        };

        // Act: Clone the object
        var clone = original.DeepClone();

        // Assert: Verify all properties are cloned correctly
        clone.ShouldNotBeSameAs(original);
        clone.RequiredName.ShouldBe("Test");
        clone.RequiredId.ShouldBe(123);
        clone.OptionalDescription.ShouldBe("Description");
    }
}

// Test classes

[DeepCloneable]
public partial class ClassWithRequiredProperty
{
    public required string RequiredName { get; set; }
    public int OptionalValue { get; set; }
}

[DeepCloneable]
public partial class ClassWithCopyConstructor
{
    public string Name { get; set; } = string.Empty;
    public int Value { get; set; }

    public ClassWithCopyConstructor()
    {
    }

    public ClassWithCopyConstructor(ClassWithCopyConstructor other)
    {
        Name = other.Name;
        Value = other.Value;
    }
}

[DeepCloneable]
public partial class CircularNode
{
    public string Name { get; set; } = string.Empty;
    public int Value { get; set; }
    public CircularNode? Next { get; set; }
}

[DeepCloneable]
public partial class ClassWithPrimitiveArray
{
    public int[] Numbers { get; set; } = Array.Empty<int>();
}

[DeepCloneable]
public partial class ClassWithIEnumerable
{
    public IEnumerable<int> Items { get; set; } = Array.Empty<int>();
}

// Custom IEnumerable implementation for testing
public class CustomEnumerable<T> : IEnumerable<T>
{
    private readonly T[] _items;

    public CustomEnumerable(T[] items)
    {
        _items = items;
    }

    public IEnumerator<T> GetEnumerator()
    {
        foreach (var item in _items)
        {
            yield return item;
        }
    }

    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}

[DeepCloneable]
public partial class ClassWithMultipleRequired
{
    public required string RequiredName { get; set; }
    public required int RequiredId { get; set; }
    public string? OptionalDescription { get; set; }
}
