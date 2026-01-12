
## Feature
For example, we will implement DeepClone for the following class.

```csharp
[DeepCloneable]
public partial class GenericClass<T>
{
    public int Id { get; set; }
    public T Value { get; set; }
    public List<string> Names { get; set; }
    public List<T> Items { get; set; }
}
```

The generated code is expected to be as follows.

```csharp
// generated
partial class GenericClass<T> where T : IDeepCloneable<GenericClass<T>>
{
    public GenericClass<T> DeepClone() => DeepCloneableExtensions.GenericClassCloneInternal(this);
}

internal static partial class DeepCloneableExtensions
{
    internal static GenericClass<T> GenericClassCloneInternal_TCloneable<T>(this GenericClass<T> original)
        where T : IDeepCloneable<T> // add constraint for T
    {
        if (original == null) return null;
        var clone = new GenericClass<T>();
        // primitive type can be copied directly
        clone.Id = original.Id;
        // if T implements IDeepCloneable<T>, so we can call DeepClone on it
        clone.Value = original.Value.DeepClone();
        // use already defined List<string> cloning
        clone.Names = CloneList_System_Collections_Generic_List_string_(original.Names);
        // list cloning is same as above
        clone.Items = ListTCloneInternal_TCloneable(original.Items);
        return clone;
    }

    private static List<T> ListTCloneInternal_TCloneable<T>(this List<T> original) where T : IDeepCloneable<T>
    {
        if (original == null) return null;
        var clone = new List<T>(original.Count);
        foreach(var item in original)
        {
            clone.Add(item.DeepClone());
        }
        return clone;
    }
}
```
