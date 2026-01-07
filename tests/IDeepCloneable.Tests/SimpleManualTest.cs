using System.Collections.Generic;
using System.Linq;

namespace IDeepCloneable.Tests;

/// <summary>
/// Simple manual test to verify the test logic works with manual DeepClone implementation.
/// This serves as a proof that the test patterns in ComplexNestedCloneTests are correct.
/// </summary>
public class SimpleManualTest
{
    [Fact]
    public void ManualDeepClone_TripleNestedList_CreatesIndependentCopy()
    {
        // Arrange
        var original = new ManualTripleNestedList
        {
            Name = "Root",
            Items = new List<List<List<int>>>
            {
                new List<List<int>>
                {
                    new List<int> { 1, 2, 3 },
                    new List<int> { 4, 5, 6 },
                },
            },
        };

        // Act
        var clone = original.DeepClone();

        // Assert
        clone.ShouldNotBeSameAs(original);
        clone.Items.ShouldNotBeSameAs(original.Items);
        clone.Items[0].ShouldNotBeSameAs(original.Items[0]);
        clone.Items[0][0].ShouldNotBeSameAs(original.Items[0][0]);

        // Verify modifying clone doesn't affect original
        clone.Items[0][0][0] = 999;
        original.Items[0][0][0].ShouldBe(1);
    }

    [Fact]
    public void ManualDeepClone_ThreeLevelNesting_CreatesDeepCopy()
    {
        // Arrange
        var original = new ManualLevel1
        {
            Name = "L1",
            Value = 100,
            Level2 = new ManualLevel2
            {
                Name = "L2",
                Value = 200,
                Level3 = new ManualLevel3 { Name = "L3", Value = 300 },
            },
        };

        // Act
        var clone = original.DeepClone();

        // Assert
        clone.ShouldNotBeSameAs(original);
        clone.Level2.ShouldNotBeSameAs(original.Level2);
        clone.Level2.Level3.ShouldNotBeSameAs(original.Level2.Level3);

        // Verify values
        clone.Level2.Level3.Value.ShouldBe(300);

        // Verify modifying clone doesn't affect original
        clone.Level2.Level3.Value = 999;
        original.Level2.Level3.Value.ShouldBe(300);
    }
}

// Manual implementations to prove the test logic works
public class ManualTripleNestedList : IDeepCloneable<ManualTripleNestedList>
{
    public string Name { get; set; } = string.Empty;
    public List<List<List<int>>>? Items { get; set; }

    public ManualTripleNestedList DeepClone()
    {
        return new ManualTripleNestedList
        {
            Name = this.Name,
            Items = this
                .Items?.Select(level1 => level1.Select(level2 => level2.ToList()).ToList())
                .ToList(),
        };
    }
}

public class ManualLevel1 : IDeepCloneable<ManualLevel1>
{
    public string Name { get; set; } = string.Empty;
    public int Value { get; set; }
    public ManualLevel2? Level2 { get; set; }

    public ManualLevel1 DeepClone()
    {
        return new ManualLevel1
        {
            Name = this.Name,
            Value = this.Value,
            Level2 = this.Level2?.DeepClone(),
        };
    }
}

public class ManualLevel2 : IDeepCloneable<ManualLevel2>
{
    public string Name { get; set; } = string.Empty;
    public int Value { get; set; }
    public ManualLevel3? Level3 { get; set; }

    public ManualLevel2 DeepClone()
    {
        return new ManualLevel2
        {
            Name = this.Name,
            Value = this.Value,
            Level3 = this.Level3?.DeepClone(),
        };
    }
}

public class ManualLevel3 : IDeepCloneable<ManualLevel3>
{
    public string Name { get; set; } = string.Empty;
    public int Value { get; set; }

    public ManualLevel3 DeepClone()
    {
        return new ManualLevel3 { Name = this.Name, Value = this.Value };
    }
}
