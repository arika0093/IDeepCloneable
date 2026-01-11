using System;
using System.Collections.Generic;
using System.Text;

namespace IDeepCloneable.Tests;

public class GenericsTypeWorkTests
{
    //[Fact]
    //public void DeepClone_IEnumerableCustomType_ClonesCorrectly()
    //{
    //    // Arrange: Create a class using IEnumerable
    //    var original = new ClassWithIEnumerable
    //    {
    //        Items = [1, 2, 3, 4, 5],
    //        CustomItems = new CustomEnumerable<string>(["foo", "bar", "buz"]),
    //    };

    //    // Act: Clone the object
    //    var clone = original.DeepClone();

    //    // Assert: Verify it's a different instance
    //    clone.ShouldNotBeSameAs(original);
    //    clone.Items.ShouldNotBeSameAs(original.Items);
    //    clone.CustomItems.ShouldNotBeSameAs(original.CustomItems);

    //    // Verify values
    //    var cloneList = new List<int>(clone.Items);
    //    var originalList = new List<int>(original.Items);
    //    cloneList.ShouldBe(originalList);
    //}
}

// TODO
// [DeepCloneable]
// public partial class ClassWithIEnumerable<T>
// {
//     public IEnumerable<int> Items { get; set; } = [];
//     public CustomEnumerable<string> CustomItems { get; set; } = new([]);
//     public MyGenericsClass<T> GenericsProperty { get; set; } = new();
// }

// Custom IEnumerable implementation for testing
public class CustomEnumerable<T> : IEnumerable<T>
{
    private readonly T[] _items;

    public CustomEnumerable(T[] items)
    {
        _items = items;
    }

    public IEnumerator<T> GetEnumerator()
    {
        foreach (var item in _items)
        {
            yield return item;
        }
    }

    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}

public class MyGenericsClass<T>
{
    public T? Value1 { get; set; }
    public required T Value2 { get; init; }
}

public class MySampleClass
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}
