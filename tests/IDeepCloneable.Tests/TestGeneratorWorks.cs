namespace IDeepCloneable.Tests;

public partial class TestGeneratorWorksTest
{
    [Fact]
    public void DeepClone_MethodIsGenerated_ForVariousClassTypes()
    {
        // TestClass
        var testClassOriginal = new TestClass { Name = "Test" };
        var testClassClone = testClassOriginal.DeepClone();
        testClassClone.ShouldNotBeSameAs(testClassOriginal);

        // DerivedInterfaceClass
        var derivedInterfaceOriginal = new DerivedInterfaceClass { Name = "InterfaceTest" };
        var derivedInterfaceClone = derivedInterfaceOriginal.DeepClone();
        derivedInterfaceClone.ShouldNotBeSameAs(derivedInterfaceOriginal);

        // ConcreteClass
        var concreteOriginal = new ConcreteClass { Name = "ConcreteTest", Value = 42 };
        var concreteClone = concreteOriginal.DeepClone();
        concreteClone.GetType().ShouldBe(typeof(ConcreteClass));
        concreteClone.ShouldNotBeSameAs(concreteOriginal);

        var abstractClone = (AbstractBaseClass)concreteOriginal.DeepClone();
        abstractClone.GetType().ShouldBe(typeof(ConcreteClass));
        abstractClone.ShouldNotBeSameAs(concreteOriginal);
    }

    [DeepCloneable]
    public partial class TestClass
    {
        public string Name { get; set; } = "";
    }

    [DeepCloneable]
    public partial class DerivedInterfaceClass
    {
        public string Name { get; set; } = string.Empty;
        // should be generated DeepClone method
    }

    [DeepCloneable]
    public abstract partial class AbstractBaseClass
    {
        public string Name { get; set; } = string.Empty;
        // public abstract AbstractBaseClass DeepClone(); should be generated
    }

    [DeepCloneable]
    public partial class ConcreteClass : AbstractBaseClass
    {
        public new string Name { get; set; } = string.Empty;
        public int Value { get; set; }
        // should be generated DeepClone
        // public ConcreteClass DeepClone() { ... }
        // and
        // public override AbstractBaseClass IDeepCloneable<AbstractBaseClass>.DeepClone() => DeepClone();
    }
}
