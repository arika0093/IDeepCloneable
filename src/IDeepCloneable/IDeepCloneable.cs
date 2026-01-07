/// <summary>
/// Interface for types that support deep cloning.
/// When implemented on a partial type, the source generator will automatically generate the DeepClone method.
/// </summary>
/// <typeparam name="T">The type of the cloneable object.</typeparam>
public interface IDeepCloneable<T>
{
    /// <summary>
    /// Creates a deep clone of the current instance.
    /// All properties and nested objects are cloned recursively.
    /// </summary>
    /// <returns>A deep clone of the current instance.</returns>
    T DeepClone();
}
