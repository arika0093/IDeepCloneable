using System;
using System.Collections.Generic;
using System.Text;

namespace IDeepCloneable.Tests;

public class GenericsTypeWorkTests { }

// TODO
// [DeepCloneable]
public partial class ClassWithIEnumerable<T>
{
    public IEnumerable<int> Items { get; set; } = [];
    public CustomEnumerable<string> CustomItems { get; set; } = new([]);
}

// TODO
// [DeepCloneable]
public partial class ClassWithGenericsPattern<T>
{
    public required MyGenericsClass<T> GenericsProperty { get; set; }
    public required IEnumerable<MyGenericsClass<T>> NestedGenericsPattern { get; set; }
}

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
