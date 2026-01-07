using Microsoft.CodeAnalysis;
using System.Text;

namespace IDeepCloneable.Generator.TypeHandlers;

/// <summary>
/// Handler for value types and immutable types (primitives, string, DateTime, etc.)
/// </summary>
internal class ValueTypeHandler : TypeHandlerBase
{
    public override int Priority => 10; // High priority - check first

    public override bool CanHandle(ITypeSymbol typeSymbol, IPropertySymbol property)
    {
        return IsValueOrImmutableType(typeSymbol);
    }

    public override void GenerateCloneStatements(
        StringBuilder sb,
        IPropertySymbol property,
        ITypeSymbol typeSymbol,
        string sourceVar,
        string targetVar,
        string indent)
    {
        // Simple assignment for value types and immutable types
        sb.AppendLine($"{indent}{targetVar}.{property.Name} = {sourceVar}.{property.Name};");
    }

    private static bool IsValueOrImmutableType(ITypeSymbol typeSymbol)
    {
        // Value types
        if (typeSymbol.IsValueType)
            return true;

        // String is immutable
        if (typeSymbol.SpecialType == SpecialType.System_String)
            return true;

        // Other known immutable types
        var fullName = typeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        return fullName is "global::System.DateTime"
            or "global::System.DateTimeOffset"
            or "global::System.TimeSpan"
            or "global::System.Guid";
    }
}
