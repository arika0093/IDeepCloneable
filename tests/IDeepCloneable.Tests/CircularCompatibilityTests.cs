using System.Collections.Generic;

namespace IDeepCloneable.Tests;

public class CircularCompatibilityTests
{
    [Fact]
    public void CircularReference_Should_Be_Cloned()
    {
        var node1 = new CircularNode { Name = "Node1", Value = 1 };
        var node2 = new CircularNode { Name = "Node2", Value = 2 };
        node1.Next = node2;
        node2.Next = node1;

        var clone = node1.DeepClone();

        clone.ShouldNotBeSameAs(node1);
        clone.Name.ShouldBe("Node1");
        clone.Next.ShouldNotBeNull();
        clone.Next.ShouldNotBeSameAs(node2);
        clone.Next!.Name.ShouldBe("Node2");
        clone.Next.Value.ShouldBe(2);
        clone.Next.Next.ShouldBeSameAs(clone);
    }

    [Fact]
    public void CircularReference_Simple_HandlesCorrectly()
    {
        var node1 = new CircularNode { Name = "Node1", Value = 1 };
        var node2 = new CircularNode { Name = "Node2", Value = 2 };
        node1.Next = node2;
        node2.Next = node1;

        var cloneNode1 = node1.DeepClone();

        cloneNode1.ShouldNotBeSameAs(node1);
        cloneNode1.Name.ShouldBe("Node1");
        cloneNode1.Value.ShouldBe(1);

        cloneNode1.Next.ShouldNotBeNull();
        cloneNode1.Next.ShouldNotBeSameAs(node2);
        cloneNode1.Next!.Name.ShouldBe("Node2");
        cloneNode1.Next.Value.ShouldBe(2);

        cloneNode1.Next.Next.ShouldNotBeNull();
        cloneNode1.Next.Next.ShouldBeSameAs(cloneNode1);
    }

    [Fact]
    public void CircularReference_Complex_HandlesCorrectly()
    {
        var node1 = new CircularNode { Name = "Node1", Value = 1 };
        var node2 = new CircularNode { Name = "Node2", Value = 2 };
        var node3 = new CircularNode { Name = "Node3", Value = 3 };
        node1.Next = node2;
        node2.Next = node3;
        node3.Next = node1;

        var cloneNode1 = node1.DeepClone();

        cloneNode1.ShouldNotBeSameAs(node1);
        cloneNode1.Name.ShouldBe("Node1");

        var cloneNode2 = cloneNode1.Next;
        cloneNode2.ShouldNotBeNull();
        cloneNode2.ShouldNotBeSameAs(node2);
        cloneNode2!.Name.ShouldBe("Node2");

        var cloneNode3 = cloneNode2.Next;
        cloneNode3.ShouldNotBeNull();
        cloneNode3.ShouldNotBeSameAs(node3);
        cloneNode3!.Name.ShouldBe("Node3");

        cloneNode3.Next.ShouldNotBeNull();
        cloneNode3.Next.ShouldBeSameAs(cloneNode1);
    }

    [Fact]
    public void CircularReference_SelfReferencing_HandlesCorrectly()
    {
        var node = new CircularNode { Name = "SelfRef", Value = 42 };
        node.Next = node;

        var clone = node.DeepClone();

        clone.ShouldNotBeSameAs(node);
        clone.Name.ShouldBe("SelfRef");
        clone.Value.ShouldBe(42);
        clone.Next.ShouldNotBeNull();
        clone.Next.ShouldBeSameAs(clone);
    }

    [Fact]
    public void DeepClone_ComplexCircularReferenceWithRoot_HandlesCorrectly()
    {
        // Arrange: Create a more complex circular reference structure with 4 different types
        // Root -> Node1 -> Node2 -> Node3 -> Root (circular back to root)
        var root = new CircularRoot { Name = "Root", Value = 0 };
        var node1 = new CircularNode1 { Name = "Node1", Value = 1 };
        var node2 = new CircularNode2 { Name = "Node2", Value = 2 };
        var node3 = new CircularNode3 { Name = "Node3", Value = 3 };

        root.Next = node1;
        node1.Next = node2;
        node2.Next = node3;
        node3.Next = root; // Circle back to root

        // Act: Clone the root object
        var cloneRoot = root.DeepClone();

        // Assert: Verify structure is preserved
        cloneRoot.ShouldNotBeSameAs(root);
        cloneRoot.Name.ShouldBe("Root");
        cloneRoot.Value.ShouldBe(0);

        var cloneNode1 = cloneRoot.Next;
        cloneNode1.ShouldNotBeNull();
        cloneNode1.ShouldNotBeSameAs(node1);
        cloneNode1!.Name.ShouldBe("Node1");

        var cloneNode2 = cloneNode1.Next;
        cloneNode2.ShouldNotBeNull();
        cloneNode2.ShouldNotBeSameAs(node2);
        cloneNode2!.Name.ShouldBe("Node2");

        var cloneNode3 = cloneNode2.Next;
        cloneNode3.ShouldNotBeNull();
        cloneNode3.ShouldNotBeSameAs(node3);
        cloneNode3!.Name.ShouldBe("Node3");

        // Verify circular reference back to root is preserved
        cloneNode3.Next.ShouldNotBeNull();
        cloneNode3.Next.ShouldBeSameAs(cloneRoot); // Should circle back to cloned root
    }

    [Fact]
    public void CircularReference_In_Collections_Should_Be_Cloned()
    {
        var parent = new CircularParent { Name = "Parent" };
        var child1 = new CircularChild { Name = "Child1", Parent = parent };
        var child2 = new CircularChild { Name = "Child2", Parent = parent };
        parent.Children.Add(child1);
        parent.Children.Add(child2);

        var clone = parent.DeepClone();

        clone.ShouldNotBeSameAs(parent);
        clone.Name.ShouldBe("Parent");
        clone.Children.ShouldNotBeNull();
        clone.Children.Count.ShouldBe(2);
        clone.Children[0].ShouldNotBeSameAs(parent.Children[0]);
        clone.Children[1].ShouldNotBeSameAs(parent.Children[1]);
        clone.Children[0].Name.ShouldBe("Child1");
        clone.Children[1].Name.ShouldBe("Child2");
        clone.Children[0].Parent.ShouldBeSameAs(clone);
        clone.Children[1].Parent.ShouldBeSameAs(clone);
    }
}

[DeepCloneable]
public partial class CircularNode
{
    public string Name { get; set; } = string.Empty;
    public int Value { get; set; }
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

// Classes for complex circular reference test with 4 different types
[DeepCloneable]
public partial class CircularRoot
{
    public string Name { get; set; } = string.Empty;
    public int Value { get; set; }
    public CircularNode1? Next { get; set; }
}

[DeepCloneable]
public partial class CircularNode1
{
    public string Name { get; set; } = string.Empty;
    public int Value { get; set; }
    public CircularNode2? Next { get; set; }
}

[DeepCloneable]
public partial class CircularNode2
{
    public string Name { get; set; } = string.Empty;
    public int Value { get; set; }
    public CircularNode3? Next { get; set; }
}

[DeepCloneable]
public partial class CircularNode3
{
    public string Name { get; set; } = string.Empty;
    public int Value { get; set; }
    public CircularRoot? Next { get; set; }
}
