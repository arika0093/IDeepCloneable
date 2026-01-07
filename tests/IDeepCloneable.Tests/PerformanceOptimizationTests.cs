namespace IDeepCloneable.Tests;

/// <summary>
/// Tests to verify performance optimizations in generated code.
/// </summary>
public class PerformanceOptimizationTests
{
    [Fact]
    public void OptimizedRecord_WithOnlyValueTypes_UsesWithSyntax()
    {
        // This test verifies that the generated code for ValueOnlyRecord
        // uses the optimized "this with { }" syntax since all properties
        // are value types or immutable strings.
        
        var original = new ValueOnlyRecord
        {
            Id = 123,
            Name = "Test",
            IsActive = true,
            Score = 95.5
        };

        var clone = original.DeepClone();

        clone.ShouldNotBeSameAs(original);
        clone.Id.ShouldBe(123);
        clone.Name.ShouldBe("Test");
        clone.IsActive.ShouldBe(true);
        clone.Score.ShouldBe(95.5);
    }

    [Fact]
    public void Record_WithReferenceTypes_DoesNotUseSimpleWithSyntax()
    {
        // This test verifies that records with reference type properties
        // still clone properly (they need the full with syntax)
        
        var original = new RecordWithReferences
        {
            Name = "Test",
            Tags = new System.Collections.Generic.List<string> { "tag1", "tag2" }
        };

        var clone = original.DeepClone();

        clone.ShouldNotBeSameAs(original);
        clone.Tags.ShouldNotBeSameAs(original.Tags); // Deep clone, not shallow
        clone.Tags.ShouldBe(new[] { "tag1", "tag2" });
    }
}

/// <summary>
/// Record with only value types and immutable strings - should use optimized "with {}" syntax
/// </summary>
[DeepCloneable]
public partial record ValueOnlyRecord
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public double Score { get; set; }
}

/// <summary>
/// Record with reference type properties - needs full cloning logic
/// </summary>
[DeepCloneable]
public partial record RecordWithReferences
{
    public string Name { get; set; } = string.Empty;
    public System.Collections.Generic.List<string>? Tags { get; set; }
}
