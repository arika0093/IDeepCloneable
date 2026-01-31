using System;

namespace IDeepCloneable.Tests.FastClonerCompat;

public class ArrayCompatibilityTests
{
    [Fact]
    public void IntArray_Should_Be_Cloned()
    {
        var original = new ArrayWrapper { Ints = [1, 2, 3] };

        var clone = original.DeepClone();

        clone.Ints.ShouldNotBeSameAs(original.Ints);
        clone.Ints!.ShouldBe([1, 2, 3]);
    }

    [Fact]
    public void StringArray_Should_Be_Cloned()
    {
        var original = new ArrayWrapper { Strings = ["1", "2", "3"] };

        var clone = original.DeepClone();

        clone.Strings.ShouldNotBeSameAs(original.Strings);
        clone.Strings!.ShouldBe(["1", "2", "3"]);
        ReferenceEquals(original.Strings[1], clone.Strings![1]).ShouldBeTrue();
    }

    [Fact]
    public void ByteArray_Should_Be_Cloned()
    {
        var original = new ArrayWrapper { Bytes = "test"u8.ToArray() };

        var clone = original.DeepClone();

        clone.Bytes.ShouldNotBeSameAs(original.Bytes);
        clone.Bytes!.ShouldBe(original.Bytes);
    }

    [Fact]
    public void ClassArray_Should_Be_Cloned()
    {
        var original = new ArrayWrapper
        {
            ClassItems = [new ArrayClassItem(1), new ArrayClassItem(2)]
        };

        var clone = original.DeepClone();

        clone.ClassItems.ShouldNotBeSameAs(original.ClassItems);
        clone.ClassItems!.Length.ShouldBe(2);
        clone.ClassItems[0].Value.ShouldBe(1);
        clone.ClassItems[1].Value.ShouldBe(2);
        clone.ClassItems[0].ShouldNotBeSameAs(original.ClassItems[0]);
        clone.ClassItems[1].ShouldNotBeSameAs(original.ClassItems[1]);
    }

    [Fact]
    public void StructArray_Should_Be_Cloned()
    {
        var original = new ArrayWrapper { StructItems = [new SimpleStruct(1), new SimpleStruct(2)] };

        var clone = original.DeepClone();

        clone.StructItems!.ShouldBe([new SimpleStruct(1), new SimpleStruct(2)]);
    }

    [Fact]
    public void StructArray_With_Class_Should_Be_Cloned()
    {
        var original = new ArrayWrapper
        {
            StructWithClassItems =
            [
                new StructWithClass { Item = new ArrayClassItem(1) },
                new StructWithClass { Item = new ArrayClassItem(2) },
            ],
        };

        var clone = original.DeepClone();

        clone.StructWithClassItems!.Length.ShouldBe(2);
        clone.StructWithClassItems[0].Item!.Value.ShouldBe(1);
        clone.StructWithClassItems[1].Item!.Value.ShouldBe(2);
        clone.StructWithClassItems[0].Item.ShouldNotBeSameAs(original.StructWithClassItems[0].Item);
        clone.StructWithClassItems[1].Item.ShouldNotBeSameAs(original.StructWithClassItems[1].Item);
    }

    [Fact]
    public void NullArrays_Should_Be_Cloned_As_Null()
    {
        var original = new ArrayWrapper();

        var clone = original.DeepClone();

        clone.Ints.ShouldBeNull();
        clone.Strings.ShouldBeNull();
        clone.Bytes.ShouldBeNull();
        clone.ClassItems.ShouldBeNull();
    }

    [Fact]
    public void MultiDim_Array_Should_Be_Cloned()
    {
        var original = new ArrayWrapper
        {
            Multi2 = new[,]
            {
                { 1, 2 },
                { 3, 4 },
            },
            Multi3 = new[,,]
            {
                { { 1 }, { 2 } },
                { { 3 }, { 4 } },
            },
        };

        var clone = original.DeepClone();

        clone.Multi2.ShouldNotBeSameAs(original.Multi2);
        clone.Multi2![0, 0].ShouldBe(1);
        clone.Multi2[0, 1].ShouldBe(2);
        clone.Multi2[1, 0].ShouldBe(3);
        clone.Multi2[1, 1].ShouldBe(4);

        clone.Multi3.ShouldNotBeSameAs(original.Multi3);
        clone.Multi3![0, 0, 0].ShouldBe(1);
        clone.Multi3[0, 1, 0].ShouldBe(2);
        clone.Multi3[1, 0, 0].ShouldBe(3);
        clone.Multi3[1, 1, 0].ShouldBe(4);
    }
}

[DeepCloneable]
public partial class ArrayWrapper
{
    public int[]? Ints { get; set; }
    public string[]? Strings { get; set; }
    public byte[]? Bytes { get; set; }
    public ArrayClassItem[]? ClassItems { get; set; }
    public SimpleStruct[]? StructItems { get; set; }
    public StructWithClass[]? StructWithClassItems { get; set; }
    public int[,]? Multi2 { get; set; }
    public int[,,]? Multi3 { get; set; }
}

[DeepCloneable]
public partial class ArrayClassItem
{
    public ArrayClassItem() { }

    public ArrayClassItem(int value)
    {
        Value = value;
    }

    public int Value { get; set; }
}

[DeepCloneable]
public partial struct SimpleStruct
{
    public SimpleStruct(int value)
    {
        Value = value;
    }

    public int Value { get; set; }
}

[DeepCloneable]
public partial struct StructWithClass
{
    public ArrayClassItem? Item { get; set; }
}
