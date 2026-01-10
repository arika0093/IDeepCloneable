using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace IDeepCloneable.Tests;

/// <summary>
/// Performance comparison tests for edge case optimizations.
/// </summary>
public class PerformanceImprovementTests
{
    [Fact]
    public void ArrayClone_PrimitiveArray_IsFasterWithAsSpan()
    {
        // Arrange: Create a large array of primitives
        var original = new int[100000];
        for (int i = 0; i < original.Length; i++)
        {
            original[i] = i;
        }

        var wrapper = new ClassWithPrimitiveArray { Numbers = original };

        // Act: Clone using the optimized path (AsSpan().ToArray())
        var sw = Stopwatch.StartNew();
        var clone = wrapper.DeepClone();
        sw.Stop();

        // Assert: Verify correctness
        clone.Numbers.ShouldNotBeSameAs(original);
        clone.Numbers.Length.ShouldBe(original.Length);
        clone.Numbers[0].ShouldBe(0);
        clone.Numbers[99999].ShouldBe(99999);

        // The optimized version should complete quickly
        // This is a sanity check, not a precise benchmark
        sw.ElapsedMilliseconds.ShouldBeLessThan(50); // Should be very fast
    }

    [Fact]
    public void CircularReference_Performance_IsReasonable()
    {
        // Arrange: Create a circular reference chain
        var nodes = new CircularNode[100];
        for (int i = 0; i < nodes.Length; i++)
        {
            nodes[i] = new CircularNode { Name = $"Node{i}", Value = i };
            if (i > 0)
            {
                nodes[i - 1].Next = nodes[i];
            }
        }
        // Close the circle
        nodes[nodes.Length - 1].Next = nodes[0];

        // Act: Clone with circular reference handling
        var sw = Stopwatch.StartNew();
        var cloneStart = nodes[0].DeepClone();
        sw.Stop();

        // Assert: Verify correctness
        cloneStart.ShouldNotBeSameAs(nodes[0]);
        
        // Walk the cloned chain
        var current = cloneStart;
        int count = 0;
        var visited = new HashSet<CircularNode>();
        while (current != null && !visited.Contains(current))
        {
            visited.Add(current);
            current = current.Next;
            count++;
        }
        
        count.ShouldBe(100); // Should visit all 100 nodes
        current.ShouldBeSameAs(cloneStart); // Should circle back to start

        // Should complete in reasonable time even with circular references
        sw.ElapsedMilliseconds.ShouldBeLessThan(100);
    }

    [Fact]
    public void RequiredProperties_Performance_NoOverhead()
    {
        // Arrange: Create a class with required properties
        var original = new ClassWithMultipleRequired
        {
            RequiredName = "Performance Test",
            RequiredId = 12345,
            OptionalDescription = "Testing required properties performance",
        };

        // Act: Clone many times
        var sw = Stopwatch.StartNew();
        for (int i = 0; i < 10000; i++)
        {
            var clone = original.DeepClone();
        }
        sw.Stop();

        // Assert: Should complete quickly
        // Object initializer syntax should have minimal overhead
        sw.ElapsedMilliseconds.ShouldBeLessThan(100);
    }

    [Fact]
    public void IEnumerable_Fallback_Works()
    {
        // Arrange: Create a custom IEnumerable
        var items = Enumerable.Range(0, 1000).ToArray();
        var original = new ClassWithIEnumerable
        {
            Items = new CustomEnumerable<int>(items),
        };

        // Act: Clone
        var sw = Stopwatch.StartNew();
        var clone = original.DeepClone();
        sw.Stop();

        // Assert: Verify correctness
        clone.Items.ShouldNotBeSameAs(original.Items);
        var cloneList = clone.Items.ToList();
        var originalList = original.Items.ToList();
        cloneList.ShouldBe(originalList);

        // Should complete in reasonable time
        sw.ElapsedMilliseconds.ShouldBeLessThan(50);
    }
}
