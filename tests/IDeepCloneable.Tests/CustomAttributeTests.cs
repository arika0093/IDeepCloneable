using System.Collections.Generic;

namespace IDeepCloneable.Tests;

/// <summary>
/// Tests for [CloneIgnore] and [ShallowClone] attributes.
/// </summary>
public class CustomAttributeTests
{
    [Fact]
    public void DeepClone_WithCloneIgnoreAttribute_IgnoresProperty()
    {
        // Test that properties marked with [CloneIgnore] remain at their default value
        var original = new ClassWithCloneIgnore
        {
            Name = "Test",
            IgnoredField = "This should be ignored",
            Age = 25,
        };

        var clone = original.DeepClone();

        clone.ShouldNotBeSameAs(original);
        clone.Name.ShouldBe(original.Name);
        clone.Age.ShouldBe(original.Age);
        // CloneIgnore property should be default (null or empty string)
        clone.IgnoredField.ShouldBeNullOrEmpty();
    }

    [Fact]
    public void DeepClone_WithShallowCloneAttribute_ShallowCopiesReference()
    {
        // Test that properties marked with [ShallowClone] are shallow-copied (reference copied)
        var nestedObject = new NestedObject { Value = "Shared" };
        var original = new ClassWithShallowClone
        {
            Name = "Test",
            ShallowCopiedNested = nestedObject,
            Age = 30,
        };

        var clone = original.DeepClone();

        clone.ShouldNotBeSameAs(original);
        clone.Name.ShouldBe(original.Name);
        clone.Age.ShouldBe(original.Age);
        // ShallowClone property should reference the same object
        clone.ShallowCopiedNested.ShouldBeSameAs(original.ShallowCopiedNested);
        // Modifying the shared object affects both
        clone.ShallowCopiedNested!.Value = "Modified";
        original.ShallowCopiedNested.Value.ShouldBe("Modified");
    }

    [Fact]
    public void DeepClone_WithBothDeepAndShallowProperties_ClonesCorrectly()
    {
        // Test class with both deep-cloned and shallow-cloned properties
        var sharedNested = new NestedObject { Value = "Shared" };
        var deepNested = new NestedObject { Value = "Deep" };

        var original = new ClassWithMixedCloning
        {
            Name = "Test",
            DeepClonedNested = deepNested,
            ShallowClonedNested = sharedNested,
            IgnoredNested = new NestedObject { Value = "Ignored" },
        };

        var clone = original.DeepClone();

        clone.ShouldNotBeSameAs(original);
        clone.Name.ShouldBe(original.Name);

        // Deep cloned property should be a different instance
        clone.DeepClonedNested.ShouldNotBeSameAs(original.DeepClonedNested);
        clone.DeepClonedNested!.Value.ShouldBe(original.DeepClonedNested.Value);

        // Shallow cloned property should be the same instance
        clone.ShallowClonedNested.ShouldBeSameAs(original.ShallowClonedNested);

        // Ignored property should be null
        clone.IgnoredNested.ShouldBeNull();
    }

    [Fact]
    public void DeepClone_WithCloneIgnoreOnValueType_SetsToDefault()
    {
        // Test that value type properties marked with [CloneIgnore] are set to default
        var original = new ClassWithIgnoredValueType
        {
            Name = "Test",
            IgnoredAge = 99,
            IncludedAge = 25,
        };

        var clone = original.DeepClone();

        clone.ShouldNotBeSameAs(original);
        clone.Name.ShouldBe(original.Name);
        clone.IgnoredAge.ShouldBe(0); // Default value for int
        clone.IncludedAge.ShouldBe(original.IncludedAge);
    }

    [Fact]
    public void DeepClone_WithShallowCloneOnList_SharesSameListReference()
    {
        // Test that collections marked with [ShallowClone] are shallow-copied
        var sharedList = new List<string> { "A", "B", "C" };
        var original = new ClassWithShallowList { Name = "Test", ShallowList = sharedList };

        var clone = original.DeepClone();

        clone.ShouldNotBeSameAs(original);
        clone.Name.ShouldBe(original.Name);
        // The list reference should be the same
        clone.ShallowList.ShouldBeSameAs(original.ShallowList);

        // Modifying the list affects both
        clone.ShallowList.Add("D");
        original.ShallowList.Count.ShouldBe(4);
        original.ShallowList.ShouldContain("D");
    }

    [Fact]
    public void DeepClone_RecordWithCloneIgnore_IgnoresProperty()
    {
        // Test [CloneIgnore] on record types
        var original = new RecordWithCloneIgnore
        {
            Name = "Test",
            IgnoredValue = "Should be ignored",
            Age = 30,
        };

        var clone = original.DeepClone();

        clone.ShouldNotBeSameAs(original);
        clone.Name.ShouldBe(original.Name);
        clone.Age.ShouldBe(original.Age);
        clone.IgnoredValue.ShouldBeNullOrEmpty();
    }

    [Fact]
    public void DeepClone_RecordWithShallowClone_ShallowCopiesReference()
    {
        // Test [ShallowClone] on record types
        var nestedObject = new NestedObject { Value = "Shared" };
        var original = new RecordWithShallowClone { Name = "Test", ShallowNested = nestedObject };

        var clone = original.DeepClone();

        clone.ShouldNotBeSameAs(original);
        clone.Name.ShouldBe(original.Name);
        clone.ShallowNested.ShouldBeSameAs(original.ShallowNested);
    }

    [Fact]
    public void DeepClone_WithNullShallowClonedProperty_HandlesNull()
    {
        // Test that null shallow-cloned properties are handled correctly
        var original = new ClassWithShallowClone
        {
            Name = "Test",
            ShallowCopiedNested = null,
            Age = 25,
        };

        var clone = original.DeepClone();

        clone.ShouldNotBeSameAs(original);
        clone.ShallowCopiedNested.ShouldBeNull();
    }
}

// Test classes

[DeepCloneable]
public partial class ClassWithCloneIgnore
{
    public string Name { get; set; } = string.Empty;

    [CloneIgnore]
    public string IgnoredField { get; set; } = string.Empty;

    public int Age { get; set; }
}

[DeepCloneable]
public partial class ClassWithShallowClone
{
    public string Name { get; set; } = string.Empty;

    [ShallowClone]
    public NestedObject? ShallowCopiedNested { get; set; }

    public int Age { get; set; }
}

[DeepCloneable]
public partial class ClassWithMixedCloning
{
    public string Name { get; set; } = string.Empty;

    public NestedObject? DeepClonedNested { get; set; }

    [ShallowClone]
    public NestedObject? ShallowClonedNested { get; set; }

    [CloneIgnore]
    public NestedObject? IgnoredNested { get; set; }
}

[DeepCloneable]
public partial class ClassWithIgnoredValueType
{
    public string Name { get; set; } = string.Empty;

    [CloneIgnore]
    public int IgnoredAge { get; set; }

    public int IncludedAge { get; set; }
}

[DeepCloneable]
public partial class ClassWithShallowList
{
    public string Name { get; set; } = string.Empty;

    [ShallowClone]
    public List<string> ShallowList { get; set; } = new();
}

[DeepCloneable]
public partial record RecordWithCloneIgnore
{
    public string Name { get; set; } = string.Empty;

    [CloneIgnore]
    public string IgnoredValue { get; set; } = string.Empty;

    public int Age { get; set; }
}

[DeepCloneable]
public partial record RecordWithShallowClone
{
    public string Name { get; set; } = string.Empty;

    [ShallowClone]
    public NestedObject? ShallowNested { get; set; }
}

[DeepCloneable]
public partial class NestedObject
{
    public string Value { get; set; } = string.Empty;
}
