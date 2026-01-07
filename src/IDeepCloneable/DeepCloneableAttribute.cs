using System;

/// <summary>
/// Marks a partial type for DeepClone() source generation.
/// </summary>
[AttributeUsage(
    AttributeTargets.Class | AttributeTargets.Struct,
    Inherited = false,
    AllowMultiple = false
)]
public sealed class DeepCloneableAttribute : Attribute { }
