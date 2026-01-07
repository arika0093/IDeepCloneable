using Microsoft.CodeAnalysis;

namespace IDeepCloneable.Generator.TypeHandlers;

/// <summary>
/// Interface for type handlers that determine how to clone specific types
/// </summary>
internal interface ITypeHandler
{
    /// <summary>
    /// Checks if this handler can handle the given type
    /// </summary>
    bool CanHandle(ITypeSymbol typeSymbol, IPropertySymbol property);
    
    /// <summary>
    /// Gets the priority of this handler (lower values = higher priority)
    /// </summary>
    int Priority { get; }
    
    /// <summary>
    /// Generates clone statements for the property
    /// </summary>
    void GenerateCloneStatements(
        System.Text.StringBuilder sb,
        IPropertySymbol property,
        ITypeSymbol typeSymbol,
        string sourceVar,
        string targetVar,
        string indent);
}
