using System;

namespace IDeepCloneable.Generator;

/// <summary>
/// Represents metadata about a class that needs deep cloning support.
/// </summary>
internal record ClassInfo : IEquatable<ClassInfo>
{
    /// <summary>Simple class name without namespace.</summary>
    public required string ClassName { get; init; }
    
    /// <summary>Fully qualified class name starting with global::.</summary>
    public required string FullClassName { get; init; }
    
    /// <summary>Namespace of the class.</summary>
    public required string Namespace { get; init; }
    
    /// <summary>Containing type names for nested classes (empty for non-nested).</summary>
    public required EquatableArray<string> ContainingTypeNames { get; init; }
    
    /// <summary>List of child property/field type names (full names). Only direct children, not grandchildren.</summary>
    public required EquatableArray<PropertyInfo> Properties { get; init; }
    
    /// <summary>Whether the type is nullable.</summary>
    public required bool IsNullable { get; init; }
    
    /// <summary>Whether the type is a record.</summary>
    public required bool IsRecord { get; init; }
    
    /// <summary>Whether the type is a value type.</summary>
    public required bool IsValueType { get; init; }
    
    /// <summary>Whether all nested types are value types or immutable types (like string).</summary>
    public required bool IsAllImmutable { get; init; }
    
    /// <summary>Whether the type is a collection (has collection initializer).</summary>
    public required bool IsCollection { get; init; }
    
    /// <summary>Whether the type has [DeepCloneable] attribute or inherits from a [DeepCloneable] class.</summary>
    public required bool NeedsDeepCloneMethod { get; init; }
    
    /// <summary>Whether the type is abstract.</summary>
    public required bool IsAbstract { get; init; }
    
    /// <summary>Whether the type is sealed.</summary>
    public required bool IsSealed { get; init; }
    
    /// <summary>Whether the base type has DeepClone method.</summary>
    public required bool BaseHasDeepClone { get; init; }
    
    /// <summary>Whether this type already has a DeepClone method defined (manually or generated).</summary>
    public required bool AlreadyHasDeepClone { get; init; }
}
