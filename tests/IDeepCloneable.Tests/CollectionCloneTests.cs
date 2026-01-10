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
        clone.Items.ShouldNotBeSameAs(original.Items);
        clone.Items.Count.ShouldBe(2);

        clone.Items[0].ShouldNotBeSameAs(original.Items[0]);
        clone.Items[0].Value.ShouldBe("Item1");
        clone.Items[1].ShouldNotBeSameAs(original.Items[1]);
        clone.Items[1].Value.ShouldBe("Item2");
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
        clone.Items[0].Value = "Modified";

        original.Items[0].Value.ShouldBe("Original");
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
        clone.Numbers.ShouldNotBeSameAs(original.Numbers);
        clone.Numbers.ShouldBe([1, 2, 3]);
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
