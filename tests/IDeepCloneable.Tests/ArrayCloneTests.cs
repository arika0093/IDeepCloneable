using System.Linq;

namespace IDeepCloneable.Tests;

/// <summary>
/// Tests for array cloning functionality.
/// </summary>
public class ArrayCloneTests
{
    [Fact]
    public void DeepClone_IntArray_ClonesArray()
    {
        var original = new ClassWithIntArray { Name = "Test", Numbers = [1, 2, 3, 4, 5] };

        var clone = original.DeepClone();

        clone.ShouldNotBeSameAs(original);
        clone.Numbers.ShouldNotBeNull();
        clone.Numbers.ShouldNotBeSameAs(original.Numbers);
        clone.Numbers.ShouldBe([1, 2, 3, 4, 5]);
    }

    [Fact]
    public void DeepClone_StringArray_ClonesArray()
    {
        var original = new ClassWithStringArray { Name = "Test", Items = ["one", "two", "three"] };

        var clone = original.DeepClone();

        clone.ShouldNotBeSameAs(original);
        clone.Items.ShouldNotBeNull();
        clone.Items.ShouldNotBeSameAs(original.Items);
        clone.Items.ShouldBe(["one", "two", "three"]);
    }

    [Fact]
    public void DeepClone_ArrayOfCloneables_CreatesDeepCopy()
    {
        var original = new ClassWithCloneableArray
        {
            Name = "Parent",
            Items =
            [
                new SimpleClass { Name = "Item1", Age = 1 },
                new SimpleClass { Name = "Item2", Age = 2 },
            ],
        };

        var clone = original.DeepClone();

        clone.ShouldNotBeSameAs(original);
        clone.Items.ShouldNotBeNull();
        clone.Items.ShouldNotBeSameAs(original.Items);
        clone.Items.Length.ShouldBe(2);

        clone.Items[0].ShouldNotBeNull();
        clone.Items[0].ShouldNotBeSameAs(original.Items[0]);
        clone.Items[0]?.Name.ShouldBe("Item1");
        clone.Items[1].ShouldNotBeNull();
        clone.Items[1].ShouldNotBeSameAs(original.Items[1]);
        clone.Items[1]?.Name.ShouldBe("Item2");
    }

    [Fact]
    public void DeepClone_ArrayModification_DoesNotAffectOriginal()
    {
        var original = new ClassWithCloneableArray
        {
            Name = "Parent",
            Items = [new SimpleClass { Name = "Original", Age = 10 }],
        };

        var clone = original.DeepClone();
        clone.Items.ShouldNotBeNull();
        if (clone.Items[0] != null)
        {
            clone.Items[0].Name = "Modified";
        }

        original.Items[0].Name.ShouldBe("Original");
    }

    [Fact]
    public void DeepClone_NullArray_HandlesCorrectly()
    {
        var original = new ClassWithIntArray { Name = "Test", Numbers = null };

        var clone = original.DeepClone();

        clone.ShouldNotBeSameAs(original);
        clone.Numbers.ShouldBeNull();
    }

    [Fact]
    public void DeepClone_EmptyArray_HandlesCorrectly()
    {
        var original = new ClassWithIntArray { Name = "Test", Numbers = System.Array.Empty<int>() };

        var clone = original.DeepClone();

        clone.ShouldNotBeSameAs(original);
        clone.Numbers.ShouldNotBeNull();
        clone.Numbers.Length.ShouldBe(0);
    }

    [Fact]
    public void DeepClone_MultiDimensionalArray_ClonesArray()
    {
        var original = new ClassWithMultiDimensionalArray
        {
            Name = "Test",
            Matrix = new int[,]
            {
                { 1, 2 },
                { 3, 4 },
            },
        };

        var clone = original.DeepClone();

        clone.ShouldNotBeSameAs(original);
        clone.Matrix.ShouldNotBeNull();
        clone.Matrix.ShouldNotBeSameAs(original.Matrix);
        clone.Matrix[0, 0].ShouldBe(1);
        clone.Matrix[0, 1].ShouldBe(2);
        clone.Matrix[1, 0].ShouldBe(3);
        clone.Matrix[1, 1].ShouldBe(4);
    }
}

[DeepCloneable]
public partial class ClassWithIntArray
{
    public string Name { get; set; } = string.Empty;
    public int[]? Numbers { get; set; }
}

[DeepCloneable]
public partial class ClassWithStringArray
{
    public string Name { get; set; } = string.Empty;
    public string[]? Items { get; set; }
}

[DeepCloneable]
public partial class ClassWithCloneableArray
{
    public string Name { get; set; } = string.Empty;
    public SimpleClass?[]? Items { get; set; }
}

[DeepCloneable]
public partial class ClassWithMultiDimensionalArray
{
    public string Name { get; set; } = string.Empty;
    public int[,]? Matrix { get; set; }
}
