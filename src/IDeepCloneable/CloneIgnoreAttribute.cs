using System;

/// <summary>
/// Marks a field or property to be ignored during DeepClone operations.
/// Properties or fields marked with this attribute will remain at their default value in the cloned object.
/// </summary>
[AttributeUsage(
    AttributeTargets.Property | AttributeTargets.Field,
    Inherited = false,
    AllowMultiple = false
)]
public sealed class CloneIgnoreAttribute : Attribute { }
