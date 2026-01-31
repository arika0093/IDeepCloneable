using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace IDeepCloneable.Tests;

/// <summary>
/// Tests for generic type patterns and IEnumerable support.
/// </summary>
public class GenericsTypeWorkTests
{
    [Fact]
    public void DeepClone_IEnumerableInt_CreatesDeepCopy()
    {
        // Arrange
        var original = new ClassWithIEnumerable<string>
        {
            Items = new List<int> { 1, 2, 3, 4, 5 },
        };

        // Act
        var clone = original.DeepClone();

        // Assert
        clone.ShouldNotBeSameAs(original);
        clone.Items.ShouldNotBeSameAs(original.Items);
        clone.Items.ShouldBe(new[] { 1, 2, 3, 4, 5 });
    }

    [Fact]
    public void DeepClone_CustomEnumerable_CreatesDeepCopy()
    {
        // Arrange
        var original = new ClassWithIEnumerable<string>
        {
            CustomItems = new CustomEnumerable<string>(new[] { "a", "b", "c" }),
        };

        // Act
        var clone = original.DeepClone();

        // Assert
        clone.ShouldNotBeSameAs(original);
        clone.CustomItems.ShouldNotBeSameAs(original.CustomItems);
        clone.CustomItems.ToArray().ShouldBe(new[] { "a", "b", "c" });
    }
}

// IEnumerable properties support
[DeepCloneable]
public partial class ClassWithIEnumerable<T>
{
    public IEnumerable<int> Items { get; set; } = [];
    public CustomEnumerable<string> CustomItems { get; set; } = new([]);
}

// Custom IEnumerable implementation for testing
[DeepCloneable]
public partial class CustomEnumerable<T> : IEnumerable<T>
{
    public T[] Items { get; }

    public CustomEnumerable(T[] items)
    {
        Items = items;
    }

    // Copy constructor for DeepClone support
    public CustomEnumerable(CustomEnumerable<T> other)
    {
        Items = other.Items;
    }

    public IEnumerator<T> GetEnumerator()
    {
        foreach (var item in Items)
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
public partial class MyGenericsClass<T>
{
    public T? Value1 { get; set; }
    public required T Value2 { get; init; }
}

[DeepCloneable]
public partial class MySampleClass
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

// Generic type properties support
//[DeepCloneable]
public partial class ClassWithGenericsPattern<T>
{
    public required MyGenericsClass<T> GenericsProperty { get; set; }
    public required List<MyGenericsClass<T>> NestedGenericsPattern { get; set; }
}
