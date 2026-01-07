using Microsoft.CodeAnalysis;
using System.Text;

namespace IDeepCloneable.Generator.TypeHandlers;

/// <summary>
/// Base class for type handlers with common functionality
/// </summary>
internal abstract class TypeHandlerBase : ITypeHandler
{
    public abstract bool CanHandle(ITypeSymbol typeSymbol, IPropertySymbol property);
    public abstract int Priority { get; }
    public abstract void GenerateCloneStatements(
        StringBuilder sb,
        IPropertySymbol property,
        ITypeSymbol typeSymbol,
        string sourceVar,
        string targetVar,
        string indent);

    protected static bool IsNullable(IPropertySymbol property)
    {
        return property.NullableAnnotation == NullableAnnotation.Annotated;
    }

    protected static string GetFullTypeName(ITypeSymbol typeSymbol)
    {
        return typeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
    }

    protected void GenerateWithNullCheck(
        StringBuilder sb,
        IPropertySymbol property,
        string sourceVar,
        string targetVar,
        string indent,
        System.Action generateBody)
    {
        var propertyName = property.Name;
        var isNullable = IsNullable(property);

        if (isNullable)
        {
            sb.AppendLine($"{indent}if ({sourceVar}.{propertyName} != null)");
            sb.AppendLine($"{indent}{{");
            generateBody();
            sb.AppendLine($"{indent}}}");
            sb.AppendLine($"{indent}else");
            sb.AppendLine($"{indent}{{");
            sb.AppendLine($"{indent}    {targetVar}.{propertyName} = null;");
            sb.AppendLine($"{indent}}}");
        }
        else
        {
            generateBody();
        }
    }
}
