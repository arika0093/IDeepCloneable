namespace IDeepCloneable.Generator;

/// <summary>
/// Options for the Cloneable source generator.
/// </summary>
public interface ICloneableGeneratorOptions
{
    /// <summary>
    /// The metadata name of the attribute that marks types for DeepClone generation.
    /// e.g. "DeepCloneableAttribute"
    /// </summary>
    string AttributeMetadataName { get; }

    /// <summary>
    /// The header comment to include in generated files.
    /// </summary>
    string GeneratedHeaderComment { get; }

    /// <summary>
    /// The name of the file to export the extension methods to.
    /// </summary>
    string ExtensionsExportFileName { get; }

    /// <summary>
    /// The namespace for the generated extension methods.
    /// </summary>
    string ExtensionsNamespace { get; }

    /// <summary>
    /// The class name for the generated extension methods.
    /// </summary>
    string ExtensionsClassName { get; }

    /// <summary>
    /// The name of the interface that the generated DeepClone methods will implement.
    /// e.g. "global::IDeepCloneable"
    /// </summary>
    string ImplementedInterfaceName { get; }

    /// <summary>
    /// The name of the DeepClone method to implement.
    /// e.g. "DeepClone"
    /// </summary>
    string ImplementsMethodName { get; }
}
