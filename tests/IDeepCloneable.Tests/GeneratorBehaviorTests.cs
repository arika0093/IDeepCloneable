using IDeepCloneable;

namespace IDeepCloneable.Tests;

/// <summary>
/// Tests for generator behavior with partial classes and existing implementations.
/// </summary>
public class GeneratorBehaviorTests
{
    [Fact]
    public void PartialClass_WithAttribute_GeneratesMethod()
    {
        var original = new PartialTestClass { Name = "Test" };

        var clone = original.DeepClone();

        clone.ShouldNotBeSameAs(original);
        clone.Name.ShouldBe(original.Name);
    }

    [Fact]
    public void ManualImplementation_IsUsed()
    {
        var original = new ManualImplementationClass { Name = "Test", CustomValue = 42 };

        var clone = original.DeepClone();

        clone.ShouldNotBeSameAs(original);
        clone.Name.ShouldBe(original.Name);
        clone.CustomValue.ShouldBe(100);
    }

    [Fact]
    public void Attribute_GeneratesMethod()
    {
        var original = new BehaviorTestAttributeClass { Name = "Test" };

        var clone = original.DeepClone();

        clone.ShouldNotBeSameAs(original);
        clone.Name.ShouldBe(original.Name);
    }

    [Fact]
    public void AbstractClass_ImplementsInterface_GeneratesMethod()
    {
        var original = new BehaviorTestConcreteClass { Name = "Test", Value = 42 };

        var clone = (BehaviorTestConcreteClass)original.DeepClone();

        clone.ShouldNotBeSameAs(original);
        clone.Name.ShouldBe(original.Name);
        clone.Value.ShouldBe(original.Value);
    }
}

[DeepCloneable]
public partial class PartialTestClass
{
    public string Name { get; set; } = string.Empty;
}

[DeepCloneable]
public partial class ManualImplementationClass
{
    public string Name { get; set; } = string.Empty;
    public int CustomValue { get; set; }

    public ManualImplementationClass DeepClone()
    {
        return new ManualImplementationClass { Name = this.Name, CustomValue = 100 };
    }
}

[DeepCloneable]
public partial class BehaviorTestAttributeClass
{
    public string Name { get; set; } = string.Empty;
}

[DeepCloneable]
public abstract partial class AbstractBaseClass
{
    public string Name { get; set; } = string.Empty;

    public abstract AbstractBaseClass DeepClone();
}

[DeepCloneable]
public partial class BehaviorTestConcreteClass : AbstractBaseClass
{
    public int Value { get; set; }

    public override BehaviorTestConcreteClass DeepClone()
    {
        return new BehaviorTestConcreteClass { Name = this.Name, Value = this.Value };
    }
}
