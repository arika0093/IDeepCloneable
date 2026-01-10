using System;

namespace IDeepCloneable.Generator;

/// <summary>
/// Represents metadata about a property or field.
/// </summary>
public record PropertyInfo : IEquatable<PropertyInfo>
{
    /// <summary>Name of the property/field.</summary>
    public required string Name { get; init; }

    /// <summary>Fully qualified type name.</summary>
    public required string TypeFullName { get; init; }

    /// <summary>Whether the property/field is nullable.</summary>
    public required bool IsNullable { get; init; }

    /// <summary>Whether the type needs deep cloning.</summary>
    public required bool NeedsDeepClone { get; init; }

    /// <summary>Whether this is a value type or immutable type.</summary>
    public required bool IsImmutable { get; init; }
}
