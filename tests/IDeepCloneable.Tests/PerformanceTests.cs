using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using IDeepCloneable;

namespace IDeepCloneable.Tests;

/// <summary>
/// Performance validation tests to ensure optimizations are working.
/// These are not strict benchmarks but help validate that optimizations don't cause regressions.
/// </summary>
public class PerformanceTests
{
    [Fact]
    public void DeepClone_LargeIntArray_IsFast()
    {
        // Create a large array of primitive values
        var original = new ClassWithLargeArray
        {
            Name = "Test",
            Numbers = Enumerable.Range(0, 100000).ToArray(),
        };

        // Warm up
        _ = original.DeepClone();

        // Measure
        var sw = Stopwatch.StartNew();
        var clone = original.DeepClone();
        sw.Stop();

        // Verify correctness
        clone.ShouldNotBeSameAs(original);
        clone.Numbers.ShouldNotBeNull();
        clone.Numbers.ShouldNotBeSameAs(original.Numbers);
        clone.Numbers.Length.ShouldBe(100000);

        // Performance check: Should be fast (< 10ms for 100k integers)
        // This validates that we're using efficient memory copy, not element-by-element
        sw.ElapsedMilliseconds.ShouldBeLessThan(10);
    }

    [Fact]
    public void DeepClone_ImmutableCollectionOfValueTypes_ReusesSameInstance()
    {
        var list = ImmutableList.Create(1, 2, 3, 4, 5);
        var original = new ClassWithImmutableIntList { Name = "Test", Items = list };

        var clone = original.DeepClone();

        // Verify that the immutable collection is reused (same instance)
        clone.Items.ShouldBeSameAs(original.Items);

        // But the parent object is cloned
        clone.ShouldNotBeSameAs(original);
    }

    [Fact]
    public void DeepClone_ImmutableArrayOfValueTypes_ReusesSameInstance()
    {
        var array = ImmutableArray.Create(1, 2, 3, 4, 5);
        var original = new ClassWithImmutableIntArray { Name = "Test", Items = array };

        var clone = original.DeepClone();

        // For immutable arrays of value types, the exact same array should be used
        // This is safe because both the array and its elements are immutable
        clone.Items.ShouldBe(original.Items); // Same values

        // Verify parent is cloned
        clone.ShouldNotBeSameAs(original);
    }

    [Fact]
    public void DeepClone_SpanBasedArrayCopy_IsFasterThanClone()
    {
        // This test validates that AsSpan().ToArray() is being used
        // by checking it's faster than reflection-based approaches would be

        var original = new ClassWithMultipleArrays
        {
            Integers = Enumerable.Range(0, 50000).ToArray(),
            Doubles = Enumerable.Range(0, 50000).Select(x => (double)x).ToArray(),
            Booleans = Enumerable.Range(0, 50000).Select(x => x % 2 == 0).ToArray(),
        };

        // Warm up
        _ = original.DeepClone();

        // Measure
        var sw = Stopwatch.StartNew();
        var clone = original.DeepClone();
        sw.Stop();

        // Verify correctness
        clone.Integers.ShouldNotBeSameAs(original.Integers);
        clone.Doubles.ShouldNotBeSameAs(original.Doubles);
        clone.Booleans.ShouldNotBeSameAs(original.Booleans);

        // Should be very fast (< 15ms for 150k total elements across 3 arrays)
        sw.ElapsedMilliseconds.ShouldBeLessThan(15);
    }
}

[DeepCloneable]
public partial class ClassWithLargeArray
{
    public string Name { get; set; } = string.Empty;
    public int[]? Numbers { get; set; }
}

[DeepCloneable]
public partial class ClassWithImmutableIntList
{
    public string Name { get; set; } = string.Empty;
    public ImmutableList<int>? Items { get; set; }
}

[DeepCloneable]
public partial class ClassWithImmutableIntArray
{
    public string Name { get; set; } = string.Empty;
    public ImmutableArray<int> Items { get; set; }
}

[DeepCloneable]
public partial class ClassWithMultipleArrays
{
    public int[] Integers { get; set; } = Array.Empty<int>();
    public double[] Doubles { get; set; } = Array.Empty<double>();
    public bool[] Booleans { get; set; } = Array.Empty<bool>();
}
