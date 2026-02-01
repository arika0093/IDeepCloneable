using System;

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
        var original = new ClassWithRequiredProperty { RequiredName = "Test", OptionalValue = 42 };

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
        var original = new ClassWithCopyConstructor { Name = "Original", Value = 100 };

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
    public void DeepClone_PrimitiveArray_UsesOptimizedPath()
    {
        // Arrange: Create an array of primitives
        var original = new int[] { 1, 2, 3, 4, 5 };

        // Act: Clone the array (this should use AsSpan().ToArray() internally)
        var clone = new ClassWithPrimitiveArray { Numbers = original }
            .DeepClone()
            .Numbers;

        // Assert: Verify it's a different instance
        clone.ShouldNotBeSameAs(original);
        clone.ShouldBe(original);

        // Verify modifying clone doesn't affect original
        clone[0] = 999;
        original[0].ShouldBe(1);
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

    public ClassWithCopyConstructor() { }

    public ClassWithCopyConstructor(ClassWithCopyConstructor other)
    {
        Name = other.Name;
        Value = other.Value;
    }
}

[DeepCloneable]
public partial class ClassWithPrimitiveArray
{
    public int[] Numbers { get; set; } = [];
}

[DeepCloneable]
public partial class ClassWithMultipleRequired
{
    public required string RequiredName { get; set; }
    public required int RequiredId { get; set; }
    public string? OptionalDescription { get; set; }
}
