using System.Collections.Generic;
using System.Linq;

namespace IDeepCloneable.Tests;

/// <summary>
/// Tests for complex nested cloning scenarios including triple-nested lists and complex object structures.
/// These tests ensure deep cloning works correctly even with deeply nested and complex structures.
/// </summary>
public class ComplexNestedCloneTests
{
    [Fact]
    public void DeepClone_TripleNestedList_CreatesIndependentCopy()
    {
        // Arrange: Create a triple-nested list structure
        var original = new ClassWithTripleNestedList
        {
            Name = "Root",
            Items =
            [
                new()
                {
                    new List<int> { 1, 2, 3 },
                    new List<int> { 4, 5, 6 },
                },
                new()
                {
                    new List<int> { 7, 8, 9 },
                    new List<int> { 10, 11, 12 },
                },
            ],
        };

        // Act: Clone the object
        var clone = original.DeepClone();

        // Assert: Verify it's a different instance at all levels
        clone.ShouldNotBeSameAs(original);
        clone.Items.ShouldNotBeSameAs(original.Items);
        clone.Items[0].ShouldNotBeSameAs(original.Items[0]);
        clone.Items[0][0].ShouldNotBeSameAs(original.Items[0][0]);

        // Verify values are correct
        clone.Name.ShouldBe("Root");
        clone.Items.Count.ShouldBe(2);
        clone.Items[0].Count.ShouldBe(2);
        clone.Items[0][0].ShouldBe([1, 2, 3]);

        // Verify modifying clone doesn't affect original
        clone.Items[0][0][0] = 999;
        original.Items[0][0][0].ShouldBe(1);
    }

    [Fact]
    public void DeepClone_TripleNestedCloneableObjects_CreatesDeepCopy()
    {
        // Arrange: Create a structure with three levels of nested cloneable objects
        var original = new Level1
        {
            Name = "Level1",
            Value = 100,
            Level2 = new Level2
            {
                Name = "Level2",
                Value = 200,
                Level3 = new Level3 { Name = "Level3", Value = 300 },
            },
        };

        // Act: Clone the object
        var clone = original.DeepClone();

        // Assert: Verify all levels are different instances
        clone.ShouldNotBeSameAs(original);
        clone.Level2.ShouldNotBeSameAs(original.Level2);
        clone.Level2.Level3.ShouldNotBeSameAs(original.Level2.Level3);

        // Verify values are correct
        clone.Name.ShouldBe("Level1");
        clone.Value.ShouldBe(100);
        clone.Level2.Name.ShouldBe("Level2");
        clone.Level2.Value.ShouldBe(200);
        clone.Level2.Level3.Name.ShouldBe("Level3");
        clone.Level2.Level3.Value.ShouldBe(300);

        // Verify modifying clone doesn't affect original
        clone.Level2.Level3.Value = 999;
        original.Level2.Level3.Value.ShouldBe(300);
    }

    [Fact]
    public void DeepClone_ComplexMixedStructure_CreatesDeepCopy()
    {
        // Arrange: Create a complex structure with objects containing lists containing objects
        var original = new ComplexStructure
        {
            Name = "Root",
            Items =
            [
                new()
                {
                    Id = "Item1",
                    SubItems =
                    [
                        new() { Value = "Sub1-1", Data = 10 },
                        new() { Value = "Sub1-2", Data = 20 },
                    ],
                },
                new()
                {
                    Id = "Item2",
                    SubItems =
                    [
                        new() { Value = "Sub2-1", Data = 30 },
                        new() { Value = "Sub2-2", Data = 40 },
                    ],
                },
            ],
        };

        // Act: Clone the object
        var clone = original.DeepClone();

        // Assert: Verify it's a different instance at all levels
        clone.ShouldNotBeSameAs(original);
        clone.Items.ShouldNotBeSameAs(original.Items);
        clone.Items[0].ShouldNotBeSameAs(original.Items[0]);
        clone.Items[0].SubItems.ShouldNotBeSameAs(original.Items[0].SubItems);
        clone.Items[0].SubItems[0].ShouldNotBeSameAs(original.Items[0].SubItems[0]);

        // Verify values are correct
        clone.Name.ShouldBe("Root");
        clone.Items[0].Id.ShouldBe("Item1");
        clone.Items[0].SubItems[0].Value.ShouldBe("Sub1-1");
        clone.Items[0].SubItems[0].Data.ShouldBe(10);

        // Verify modifying clone doesn't affect original
        clone.Items[0].SubItems[0].Data = 999;
        original.Items[0].SubItems[0].Data.ShouldBe(10);
    }

    [Fact]
    public void DeepClone_ListOfListsOfCloneableObjects_CreatesDeepCopy()
    {
        // Arrange: Create a list of lists containing cloneable objects
        var original = new ClassWithNestedListOfObjects
        {
            Name = "Container",
            Groups =
            [
                new()
                {
                    new SimpleCloneableItem { Id = 1, Name = "Item1-1" },
                    new SimpleCloneableItem { Id = 2, Name = "Item1-2" },
                },
                new()
                {
                    new SimpleCloneableItem { Id = 3, Name = "Item2-1" },
                    new SimpleCloneableItem { Id = 4, Name = "Item2-2" },
                },
            ],
        };

        // Act: Clone the object
        var clone = original.DeepClone();

        // Assert: Verify all levels are different instances
        clone.ShouldNotBeSameAs(original);
        clone.Groups.ShouldNotBeSameAs(original.Groups);
        clone.Groups[0].ShouldNotBeSameAs(original.Groups[0]);
        clone.Groups[0][0].ShouldNotBeSameAs(original.Groups[0][0]);

        // Verify values are correct
        clone.Groups[0][0].Id.ShouldBe(1);
        clone.Groups[0][0].Name.ShouldBe("Item1-1");

        // Verify modifying clone doesn't affect original
        clone.Groups[0][0].Name = "Modified";
        original.Groups[0][0].Name.ShouldBe("Item1-1");
    }

    [Fact]
    public void DeepClone_DeeplyNestedDictionary_CreatesDeepCopy()
    {
        // Arrange: Create a dictionary containing objects with nested lists
        var original = new ClassWithNestedDictionary
        {
            Name = "DictContainer",
            Data = new Dictionary<string, DataContainer>
            {
                {
                    "key1",
                    new DataContainer
                    {
                        Items =
                        [
                            new() { Id = 1, Name = "Item1" },
                            new() { Id = 2, Name = "Item2" },
                        ],
                    }
                },
                {
                    "key2",
                    new DataContainer
                    {
                        Items =
                        [
                            new() { Id = 3, Name = "Item3" },
                            new() { Id = 4, Name = "Item4" },
                        ],
                    }
                },
            },
        };

        // Act: Clone the object
        var clone = original.DeepClone();

        // Assert: Verify all levels are different instances
        clone.ShouldNotBeSameAs(original);
        clone.Data.ShouldNotBeSameAs(original.Data);
        clone.Data["key1"].ShouldNotBeSameAs(original.Data["key1"]);
        clone.Data["key1"].Items.ShouldNotBeSameAs(original.Data["key1"].Items);
        clone.Data["key1"].Items[0].ShouldNotBeSameAs(original.Data["key1"].Items[0]);

        // Verify values are correct
        clone.Data["key1"].Items[0].Id.ShouldBe(1);
        clone.Data["key1"].Items[0].Name.ShouldBe("Item1");

        // Verify modifying clone doesn't affect original
        clone.Data["key1"].Items[0].Name = "Modified";
        original.Data["key1"].Items[0].Name.ShouldBe("Item1");
    }

    [Fact]
    public void DeepClone_CircularReferenceInSameLevel_HandlesCorrectly()
    {
        // Arrange: Create a structure where multiple properties reference same nested object
        var sharedLevel3 = new Level3 { Name = "Shared", Value = 100 };
        var original = new ComplexWithSharedReferences
        {
            Name = "Root",
            Reference1 = new Level2
            {
                Name = "Ref1",
                Value = 10,
                Level3 = sharedLevel3,
            },
            Reference2 = new Level2
            {
                Name = "Ref2",
                Value = 20,
                Level3 = sharedLevel3,
            },
        };

        // Act: Clone the object
        var clone = original.DeepClone();

        // Assert: Verify all objects are cloned
        clone.ShouldNotBeSameAs(original);
        clone.Reference1.ShouldNotBeSameAs(original.Reference1);
        clone.Reference2.ShouldNotBeSameAs(original.Reference2);
        clone.Reference1.Level3.ShouldNotBeSameAs(original.Reference1.Level3);
        clone.Reference2.Level3.ShouldNotBeSameAs(original.Reference2.Level3);

        // Note: In the original, Reference1.Level3 and Reference2.Level3 point to the same object
        // After cloning, they should be separate instances (standard deep clone behavior)
        clone.Reference1.Level3.ShouldNotBeSameAs(clone.Reference2.Level3);

        // Verify values are correct
        clone.Reference1.Level3.Name.ShouldBe("Shared");
        clone.Reference2.Level3.Name.ShouldBe("Shared");

        // Verify modifying one doesn't affect the other after cloning
        clone.Reference1.Level3.Value = 999;
        clone.Reference2.Level3.Value.ShouldBe(100);
    }

    [Fact]
    public void DeepClone_NullValuesInNestedStructure_HandlesCorrectly()
    {
        // Arrange: Create a structure with null values at various levels
        var original = new ComplexStructure
        {
            Name = "Root",
            Items =
            [
                new() { Id = "Item1", SubItems = null },
                new() { Id = "Item2", SubItems = [new() { Value = "Sub2-1", Data = 30 }] },
            ],
        };

        // Act: Clone the object
        var clone = original.DeepClone();

        // Assert: Verify structure is correct
        clone.ShouldNotBeSameAs(original);
        clone.Items[0].SubItems.ShouldBeNull();
        clone.Items[1].SubItems.ShouldNotBeNull();
        clone.Items[1].SubItems.ShouldNotBeSameAs(original.Items[1].SubItems);
    }
}

// Test classes for triple-nested list
[DeepCloneable]
public partial class ClassWithTripleNestedList
{
    public string Name { get; set; } = string.Empty;
    public List<List<List<int>>>? Items { get; set; }
}

// Test classes for multi-level nesting
[DeepCloneable]
public partial class Level1
{
    public string Name { get; set; } = string.Empty;
    public int Value { get; set; }
    public Level2? Level2 { get; set; }
}

public partial class Level2
{
    public string Name { get; set; } = string.Empty;
    public int Value { get; set; }
    public Level3? Level3 { get; set; }
}

public partial class Level3
{
    public string Name { get; set; } = string.Empty;
    public int Value { get; set; }
}

// Test classes for complex mixed structures
[DeepCloneable]
public partial class ComplexStructure
{
    public string Name { get; set; } = string.Empty;
    public List<ItemWithNestedData>? Items { get; set; }
}

public partial class ItemWithNestedData
{
    public string Id { get; set; } = string.Empty;
    public List<SubItem>? SubItems { get; set; }
}

public partial class SubItem
{
    public string Value { get; set; } = string.Empty;
    public int Data { get; set; }
}

// Test classes for list of lists of objects
[DeepCloneable]
public partial class ClassWithNestedListOfObjects
{
    public string Name { get; set; } = string.Empty;
    public List<List<SimpleCloneableItem>>? Groups { get; set; }
}

[DeepCloneable]
public partial class SimpleCloneableItem
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

// Test classes for nested dictionary
[DeepCloneable]
public partial class ClassWithNestedDictionary
{
    public string Name { get; set; } = string.Empty;
    public Dictionary<string, DataContainer>? Data { get; set; }
}

public partial class DataContainer
{
    public List<SimpleCloneableItem>? Items { get; set; }
}

// Test classes for shared references
[DeepCloneable]
public partial class ComplexWithSharedReferences
{
    public string Name { get; set; } = string.Empty;
    public Level2? Reference1 { get; set; }
    public Level2? Reference2 { get; set; }
}
