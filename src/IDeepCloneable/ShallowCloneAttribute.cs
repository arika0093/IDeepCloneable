using System;

/// <summary>
/// Marks a field or property to be shallow-copied during DeepClone operations.
/// For reference types, the reference to the original object will be copied instead of creating a new instance.
/// </summary>
[AttributeUsage(
    AttributeTargets.Property | AttributeTargets.Field,
    Inherited = false,
    AllowMultiple = false
)]
public sealed class ShallowCloneAttribute : Attribute { }
