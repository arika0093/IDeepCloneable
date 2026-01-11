using System;
using System.Collections.Generic;

namespace IDeepCloneable.Tests;

/// <summary>
/// Tests for generic class support as specified in docs/plan/support-generics-class.md
/// </summary>
public class GenericClassTests
{
    [Fact]
    public void DeepClone_GenericClassWithConstraint_CreatesDeepCopy()
    {
        // Arrange
        var original = new GenericClassWithConstraint<GenericTestClass>
        {
            Id = 42,
            Value = new GenericTestClass { Id = 1, Name = "Original" },
            Names = new List<string> { "a", "b", "c" },
            Items = new List<GenericTestClass>
            {
                new() { Id = 2, Name = "Item1" },
                new() { Id = 3, Name = "Item2" },
            }
        };

        // Act
        var clone = original.DeepClone();

        // Assert
        clone.ShouldNotBeSameAs(original);
        clone.Id.ShouldBe(42);
        
        // Value should be deep cloned
        clone.Value.ShouldNotBeSameAs(original.Value);
        clone.Value.Id.ShouldBe(1);
        clone.Value.Name.ShouldBe("Original");
        
        // Names should be deep cloned
        clone.Names.ShouldNotBeSameAs(original.Names);
        clone.Names.ShouldBe(new[] { "a", "b", "c" });
        
        // Items should be deep cloned
        clone.Items.ShouldNotBeSameAs(original.Items);
        clone.Items.Count.ShouldBe(2);
        clone.Items[0].ShouldNotBeSameAs(original.Items[0]);
        clone.Items[0].Id.ShouldBe(2);
        clone.Items[0].Name.ShouldBe("Item1");
        clone.Items[1].ShouldNotBeSameAs(original.Items[1]);
        clone.Items[1].Id.ShouldBe(3);
        clone.Items[1].Name.ShouldBe("Item2");
    }

    [Fact]
    public void DeepClone_ModifyingClone_DoesNotAffectOriginal()
    {
        // Arrange
        var original = new GenericClassWithConstraint<GenericTestClass>
        {
            Id = 100,
            Value = new GenericTestClass { Id = 10, Name = "Value" },
            Names = new List<string> { "x", "y" },
            Items = new List<GenericTestClass>
            {
                new() { Id = 20, Name = "A" },
            }
        };

        // Act
        var clone = original.DeepClone();
        clone.Id = 200;
        clone.Value.Name = "Modified";
        clone.Names.Add("z");
        clone.Items[0].Name = "B";
        clone.Items.Add(new GenericTestClass { Id = 30, Name = "C" });

        // Assert
        original.Id.ShouldBe(100);
        original.Value.Name.ShouldBe("Value");
        original.Names.Count.ShouldBe(2);
        original.Items.Count.ShouldBe(1);
        original.Items[0].Name.ShouldBe("A");
    }
}

/// <summary>
/// Generic class with IDeepCloneable constraint
/// </summary>
[DeepCloneable]
public partial class GenericClassWithConstraint<T> where T : IDeepCloneable<T>
{
    public int Id { get; set; }
    public T Value { get; set; } = default!;
    public List<string> Names { get; set; } = [];
    public List<T> Items { get; set; } = [];
}

/// <summary>
/// Simple class for testing generic type parameters
/// </summary>
[DeepCloneable]
public partial class GenericTestClass
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}
