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
            Items = new List<int> { 1, 2, 3, 4, 5 }
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
            CustomItems = new CustomEnumerable<string>(new[] { "a", "b", "c" })
        };

        // Act
        var clone = original.DeepClone();

        // Assert
        clone.ShouldNotBeSameAs(original);
        clone.CustomItems.ShouldNotBeSameAs(original.CustomItems);
        clone.CustomItems.ToArray().ShouldBe(new[] { "a", "b", "c" });
    }

    [Fact]
    public void DeepClone_GenericTypeProperty_CreatesDeepCopy()
    {
        // Arrange
        var original = new ClassWithGenericsPattern<int>
        {
            GenericsProperty = new MyGenericsClass<int> { Value1 = 10, Value2 = 20 },
            NestedGenericsPattern = new List<MyGenericsClass<int>>()
        };

        // Act
        var clone = original.DeepClone();

        // Assert
        clone.ShouldNotBeSameAs(original);
        clone.GenericsProperty.ShouldNotBeSameAs(original.GenericsProperty);
        clone.GenericsProperty.Value1.ShouldBe(10);
        clone.GenericsProperty.Value2.ShouldBe(20);
    }

    [Fact]
    public void DeepClone_IEnumerableOfGenericType_CreatesDeepCopy()
    {
        // Arrange
        var original = new ClassWithGenericsPattern<string>
        {
            GenericsProperty = new MyGenericsClass<string> { Value1 = "test1", Value2 = "test2" },
            NestedGenericsPattern = new List<MyGenericsClass<string>>
            {
                new() { Value1 = "a", Value2 = "b" },
                new() { Value1 = "c", Value2 = "d" }
            }
        };

        // Act
        var clone = original.DeepClone();

        // Assert
        clone.ShouldNotBeSameAs(original);
        clone.NestedGenericsPattern.ShouldNotBeSameAs(original.NestedGenericsPattern);
        
        var clonedList = clone.NestedGenericsPattern.ToList();
        var originalList = original.NestedGenericsPattern.ToList();
        
        clonedList.Count.ShouldBe(2);
        clonedList[0].ShouldNotBeSameAs(originalList[0]);
        clonedList[0].Value1.ShouldBe("a");
        clonedList[0].Value2.ShouldBe("b");
        clonedList[1].ShouldNotBeSameAs(originalList[1]);
        clonedList[1].Value1.ShouldBe("c");
        clonedList[1].Value2.ShouldBe("d");
    }

    [Fact]
    public void DeepClone_ModifyingClone_DoesNotAffectOriginal()
    {
        // Arrange
        var original = new ClassWithGenericsPattern<MySampleClass>
        {
            GenericsProperty = new MyGenericsClass<MySampleClass>
            {
                Value1 = new MySampleClass { Id = 1, Name = "Original" },
                Value2 = new MySampleClass { Id = 2, Name = "Original2" }
            },
            NestedGenericsPattern = new List<MyGenericsClass<MySampleClass>>()
        };

        // Act
        var clone = original.DeepClone();
        clone.GenericsProperty.Value1!.Name = "Modified";
        clone.GenericsProperty.Value2.Name = "Modified2";

        // Assert
        original.GenericsProperty.Value1!.Name.ShouldBe("Original");
        original.GenericsProperty.Value2.Name.ShouldBe("Original2");
    }
}

// IEnumerable properties support
[DeepCloneable]
public partial class ClassWithIEnumerable<T>
{
    public IEnumerable<int> Items { get; set; } = [];
    public CustomEnumerable<string> CustomItems { get; set; } = new([]);
}

// Generic type properties support
[DeepCloneable]
public partial class ClassWithGenericsPattern<T>
{
    public required MyGenericsClass<T> GenericsProperty { get; set; }
    public required IEnumerable<MyGenericsClass<T>> NestedGenericsPattern { get; set; }
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
