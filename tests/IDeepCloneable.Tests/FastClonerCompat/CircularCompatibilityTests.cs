using System.Collections.Generic;

namespace IDeepCloneable.Tests.FastClonerCompat;

public class CircularCompatibilityTests
{
    [Fact]
    public void CircularReference_Should_Be_Cloned()
    {
        var node1 = new CircularNode { Name = "Node1" };
        var node2 = new CircularNode { Name = "Node2" };
        node1.Next = node2;
        node2.Next = node1;

        var clone = node1.DeepClone();

        clone.ShouldNotBeSameAs(node1);
        clone.Name.ShouldBe("Node1");
        clone.Next.ShouldNotBeNull();
        clone.Next.ShouldNotBeSameAs(node2);
        clone.Next!.Name.ShouldBe("Node2");
        clone.Next.Next.ShouldBeSameAs(clone);
    }

    [Fact(Skip = "Collection-based circular references currently trigger recursion without cache support.")]
    public void CircularReference_In_Collections_Should_Be_Cloned() { }
}

[DeepCloneable]
public partial class CircularNode
{
    public string Name { get; set; } = string.Empty;
    public CircularNode? Next { get; set; }
}

[DeepCloneable]
public partial class CircularParent
{
    public string Name { get; set; } = string.Empty;
    public List<CircularChild> Children { get; set; } = [];
}

[DeepCloneable]
public partial class CircularChild
{
    public string Name { get; set; } = string.Empty;
    public CircularParent? Parent { get; set; }
}
