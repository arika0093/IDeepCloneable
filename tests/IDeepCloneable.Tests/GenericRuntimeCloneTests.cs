using System.Collections.Generic;

namespace IDeepCloneable.Tests;

/// <summary>
/// Tests for runtime cloning of generic type parameters.
/// </summary>
public class GenericRuntimeCloneTests
{
    [Fact]
    public void DeepClone_GenericType_UsesIDeepCloneableImplementation()
    {
        var original = new GenericBox<RuntimeCloneable> { Value = new RuntimeCloneable { Id = 7 } };

        var clone = original.DeepClone();

        clone.ShouldNotBeSameAs(original);
        clone.Value.ShouldNotBeSameAs(original.Value);
        clone.Value!.Id.ShouldBe(7);
    }

    [Fact]
    public void DeepClone_GenericType_UsesRegisteredCloneLogic()
    {
        var original = new GenericBox<ExternalCloneTarget>
        {
            Value = new ExternalCloneTarget
            {
                Id = 42,
                Child = new ExternalChild { Name = "child" },
            },
        };

        var clone = original.DeepClone();

        clone.ShouldNotBeSameAs(original);
        clone.Value.ShouldNotBeSameAs(original.Value);
        clone.Value!.Id.ShouldBe(42);
        clone.Value.Child.ShouldNotBeSameAs(original.Value.Child);
        clone.Value.Child!.Name.ShouldBe("child");
    }

    [Fact]
    public void DeepClone_GenericTypeParameters_ClonesBothTypes()
    {
        var original = new GenericTwoBox<RuntimeCloneable, ExternalCloneTarget>
        {
            Value1 = new RuntimeCloneable { Id = 1 },
            Value2 = new ExternalCloneTarget
            {
                Id = 2,
                Child = new ExternalChild { Name = "child2" },
            },
        };
        var clone = original.DeepClone();
        clone.ShouldNotBeSameAs(original);
        clone.Value1.ShouldNotBeSameAs(original.Value1);
        clone.Value1!.Id.ShouldBe(1);
        clone.Value2.ShouldNotBeSameAs(original.Value2);
        clone.Value2!.Id.ShouldBe(2);
        clone.Value2.Child.ShouldNotBeSameAs(original.Value2.Child);
        clone.Value2.Child!.Name.ShouldBe("child2");
    }

    [Fact]
    public void DeepClone_GenericCollection_ClonesItems()
    {
        var original = new GenericCollectionBox<RuntimeCloneable>
        {
            Items = [new RuntimeCloneable { Id = 10 }, new RuntimeCloneable { Id = 20 }],
        };

        var clone = original.DeepClone();

        clone.ShouldNotBeSameAs(original);
        clone.Items.ShouldNotBeSameAs(original.Items);
        clone.Items!.Count.ShouldBe(2);
        clone.Items[0].ShouldNotBeSameAs(original.Items[0]);
        clone.Items[0].Id.ShouldBe(10);
        clone.Items[1].ShouldNotBeSameAs(original.Items[1]);
        clone.Items[1].Id.ShouldBe(20);
    }
}

[DeepCloneable]
[GenerateDeepCloneable(typeof(ExternalCloneTarget))]
public partial class GenericBox<T>
{
    public T? Value { get; set; }
}

[DeepCloneable]
public partial class GenericTwoBox<T1, T2>
{
    public T1? Value1 { get; set; }
    public T2? Value2 { get; set; }
}

[DeepCloneable]
public partial class GenericCollectionBox<T>
{
    public List<T>? Items { get; set; }
}

[DeepCloneable]
public partial class RuntimeCloneable
{
    public int Id { get; set; }
}

public class ExternalCloneTarget
{
    public int Id { get; set; }
    public ExternalChild? Child { get; set; }
}

public class ExternalChild
{
    public string Name { get; set; } = string.Empty;
}
