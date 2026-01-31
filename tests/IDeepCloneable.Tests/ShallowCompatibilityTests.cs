using System.Collections.Generic;

namespace IDeepCloneable.Tests;

public class ShallowCompatibilityTests
{
    [Fact]
    public void ShallowAttribute_On_Property_Should_Preserve_Reference()
    {
        var parent = new ShallowParent { Name = "Parent" };
        var child = new ShallowChild { Name = "Child", Parent = parent };

        var clone = child.DeepClone();

        clone.ShouldNotBeSameAs(child);
        clone.Parent.ShouldBeSameAs(parent);
    }

    [Fact]
    public void ShallowAttribute_On_Field_Should_Preserve_Reference()
    {
        var shared = new SharedState { Value = "Shared" };
        var original = new ShallowFieldHolder { Name = "Field", SharedField = shared };

        var clone = original.DeepClone();

        clone.ShouldNotBeSameAs(original);
        clone.SharedField.ShouldBeSameAs(shared);
    }

    [Fact]
    public void ShallowAttribute_On_Collection_Should_Preserve_Reference()
    {
        var sharedList = new List<int> { 1, 2, 3 };
        var original = new ShallowCollectionHolder
        {
            SharedList = sharedList,
            OwnedList = new List<int> { 10, 20 },
        };

        var clone = original.DeepClone();

        clone.ShouldNotBeSameAs(original);
        clone.SharedList.ShouldBeSameAs(sharedList);
        clone.OwnedList.ShouldNotBeSameAs(original.OwnedList);
        clone.OwnedList.ShouldBe([10, 20]);
    }

    [Fact]
    public void CloneIgnore_Should_Reset_To_Default()
    {
        var original = new IgnoreHolder
        {
            Name = "Test",
            Ignored = "IgnoreMe",
            Included = "KeepMe",
            IgnoredValue = 123,
        };

        var clone = original.DeepClone();

        clone.Name.ShouldBe("Test");
        clone.Ignored.ShouldBeNullOrEmpty();
        clone.Included.ShouldBe("KeepMe");
        clone.IgnoredValue.ShouldBe(0);
    }
}

[DeepCloneable]
public partial class ShallowParent
{
    public string Name { get; set; } = string.Empty;
}

[DeepCloneable]
public partial class ShallowChild
{
    public string Name { get; set; } = string.Empty;

    [ShallowClone]
    public ShallowParent? Parent { get; set; }
}

[DeepCloneable]
public partial class SharedState
{
    public string Value { get; set; } = string.Empty;
}

[DeepCloneable]
public partial class ShallowFieldHolder
{
    public string Name { get; set; } = string.Empty;

    [ShallowClone]
    public SharedState? SharedField;
}

[DeepCloneable]
public partial class ShallowCollectionHolder
{
    [ShallowClone]
    public List<int>? SharedList { get; set; }

    public List<int>? OwnedList { get; set; }
}

[DeepCloneable]
public partial class IgnoreHolder
{
    public string Name { get; set; } = string.Empty;

    [CloneIgnore]
    public string Ignored { get; set; } = string.Empty;

    public string Included { get; set; } = string.Empty;

    [CloneIgnore]
    public int IgnoredValue { get; set; }
}
