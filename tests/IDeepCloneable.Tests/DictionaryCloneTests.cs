using System.Collections.Generic;
using System.Linq;
namespace IDeepCloneable.Tests;

/// <summary>
/// Tests for Dictionary cloning functionality.
/// </summary>
public class DictionaryCloneTests
{
    [Fact]
    public void DeepClone_DictionaryOfValueTypes_CreatesNewDictionary()
    {
        var original = new ClassWithValueDictionary
        {
            Name = "Test",
            Scores = new Dictionary<string, int>
            {
                { "Alice", 100 },
                { "Bob", 85 },
                { "Charlie", 92 },
            },
        };

        var clone = original.DeepClone();

        clone.ShouldNotBeSameAs(original);
        clone.Scores.ShouldNotBeNull();
        clone.Scores.ShouldNotBeSameAs(original.Scores);
        clone.Scores.Count.ShouldBe(3);
        clone.Scores["Alice"].ShouldBe(100);
        clone.Scores["Bob"].ShouldBe(85);
        clone.Scores["Charlie"].ShouldBe(92);
    }

    [Fact]
    public void DeepClone_DictionaryOfCloneables_CreatesDeepCopy()
    {
        var original = new ClassWithCloneableDictionary
        {
            Name = "Test",
            Items = new Dictionary<string, SimpleClass>
            {
                {
                    "first",
                    new SimpleClass { Name = "Item1", Age = 1 }
                },
                {
                    "second",
                    new SimpleClass { Name = "Item2", Age = 2 }
                },
            },
        };

        var clone = original.DeepClone();

        clone.ShouldNotBeSameAs(original);
        clone.Items.ShouldNotBeNull();
        clone.Items.ShouldNotBeSameAs(original.Items);
        clone.Items.Count.ShouldBe(2);

        clone.Items["first"].ShouldNotBeNull();
        clone.Items["first"].ShouldNotBeSameAs(original.Items["first"]);
        clone.Items["first"].Name.ShouldBe("Item1");
        clone.Items["second"].ShouldNotBeNull();
        clone.Items["second"].ShouldNotBeSameAs(original.Items["second"]);
        clone.Items["second"].Name.ShouldBe("Item2");
    }

    [Fact]
    public void DeepClone_DictionaryModification_DoesNotAffectOriginal()
    {
        var original = new ClassWithCloneableDictionary
        {
            Name = "Test",
            Items = new Dictionary<string, SimpleClass>
            {
                {
                    "key",
                    new SimpleClass { Name = "Original", Age = 10 }
                },
            },
        };

        var clone = original.DeepClone();
        clone.Items.ShouldNotBeNull();
        clone.Items["key"].Name = "Modified";

        original.Items["key"].Name.ShouldBe("Original");
    }

    [Fact]
    public void DeepClone_NullDictionary_HandlesCorrectly()
    {
        var original = new ClassWithValueDictionary { Name = "Test", Scores = null };

        var clone = original.DeepClone();

        clone.ShouldNotBeSameAs(original);
        clone.Scores.ShouldBeNull();
    }

    [Fact]
    public void DeepClone_EmptyDictionary_HandlesCorrectly()
    {
        var original = new ClassWithValueDictionary
        {
            Name = "Test",
            Scores = new Dictionary<string, int>(),
        };

        var clone = original.DeepClone();

        clone.ShouldNotBeSameAs(original);
        clone.Scores.ShouldNotBeNull();
        clone.Scores.Count.ShouldBe(0);
    }

    [Fact]
    public void DeepClone_DictionaryWithNullValues_HandlesCorrectly()
    {
        var original = new ClassWithCloneableDictionary
        {
            Name = "Test",
            Items = new Dictionary<string, SimpleClass>
            {
                { "null-key", null! },
                {
                    "valid-key",
                    new SimpleClass { Name = "Valid", Age = 5 }
                },
            },
        };

        var clone = original.DeepClone();

        clone.ShouldNotBeSameAs(original);
        clone.Items.ShouldNotBeNull();
        clone.Items.Count.ShouldBe(2);
        clone.Items["null-key"].ShouldBeNull();
        clone.Items["valid-key"].ShouldNotBeNull();
        clone.Items["valid-key"].Name.ShouldBe("Valid");
    }
}

[DeepCloneable]
public partial class ClassWithValueDictionary
{
    public string Name { get; set; } = string.Empty;
    public Dictionary<string, int>? Scores { get; set; }
}

[DeepCloneable]
public partial class ClassWithCloneableDictionary
{
    public string Name { get; set; } = string.Empty;
    public Dictionary<string, SimpleClass>? Items { get; set; }
}
