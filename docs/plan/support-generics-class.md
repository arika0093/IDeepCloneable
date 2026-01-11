
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
partial class GenericClass<T> : IDeepCloneable<GenericClass<T>>
{
    public GenericClass<T> DeepClone() => DeepCloneableExtensions.GenericClassCloneInternal(this);
}

internal static partial class DeepCloneableExtensions
{
    private static Dictionary<Type, bool> _deepCloneableTypeCache = new();

    private static bool IsDeepCloneableType<T>()
    {
        var type = typeof(T);
        if(!_deepCloneableTypeCache.TryGetValue(type, out var isDeepCloneable))
        {
            isDeepCloneable = typeof(IDeepCloneable<T>).IsAssignableFrom(type);
            _deepCloneableTypeCache[type] = isDeepCloneable;
        }
        return isDeepCloneable;
    }

    internal static GenericClass<T> GenericClassCloneInternal<T>(this GenericClass<T> original)
    {
        if (original == null) return null;
        // check if T implements IDeepCloneable<T>
        var usableDeepCloneable = IsDeepCloneableType<T>();
        var clone = new GenericClass<T>();
        // primitive type can be copied directly
        clone.Id = original.Id;
        if(usableDeepCloneable)
        {
            // if T implements IDeepCloneable<T>, so we can call DeepClone on it
            clone.Value = (original.Value as IDeepCloneable<T>)?.DeepClone();
        }
        else
        {
            // otherwise, throw exception
            throw new InvalidOperationException($"Type '{typeof(T).FullName}' does not implement IDeepCloneable<{typeof(T).FullName}>");
        }
        // use already defined List<string> cloning
        clone.Names = CloneList_System_Collections_Generic_List_string_(original.Names);
        // list cloning is same as above
        clone.Items = ListTCloneInternal(original.Items);
        return clone;
    }

    private static List<T> ListTCloneInternal<T>(this List<T> original)
    {
        if (original == null) return null;
        var usableDeepCloneable = IsDeepCloneableType<T>();
        if(!usableDeepCloneable)
        {
            throw new InvalidOperationException($"Type '{typeof(T).FullName}' does not implement IDeepCloneable<{typeof(T).FullName}>");
        }
        var clone = new List<T>(original.Count);
        foreach(var item in original)
        {
            clone.Add((item as IDeepCloneable<T>)?.DeepClone());
        }
        return clone;
    }
}
```

If `where T : IDeepCloneable<T>` is provided, the type argument `T` is guaranteed to implement `IDeepCloneable<T>`, so type checks are unnecessary. In that case, the generated code would be as follows.

```csharp
// user
[DeepCloneable]
public partial class GenericClass<T> where T : IDeepCloneable<T>
{
    public int Id { get; set; }
    public T Value { get; set; }
    public List<string> Names { get; set; }
    public List<T> Items { get; set; }
}

// generated
partial class GenericClass<T> where T : IDeepCloneable<GenericClass<T>>
{
    public GenericClass<T> DeepClone() => DeepCloneableExtensions.GenericClassCloneInternal(this);
}

internal static partial class DeepCloneableExtensions
{
    internal static GenericClass<T> GenericClassCloneInternal_TCloneable<T>(this GenericClass<T> original) where T : IDeepCloneable<T>
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