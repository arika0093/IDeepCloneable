using System.Linq;
using Microsoft.CodeAnalysis;

namespace IDeepCloneable.Generator;

/// <summary>
/// Captures environment details for source generation.
/// </summary>
internal readonly record struct GenerationEnvironment(bool SupportsArrayAsSpan)
{
    public static GenerationEnvironment Create(Compilation compilation)
    {
        var supportsArrayAsSpan = SupportsArrayAsSpanApi(compilation);

        return new GenerationEnvironment(supportsArrayAsSpan);
    }

    private static bool SupportsArrayAsSpanApi(Compilation compilation)
    {
        var memoryExtensions = compilation.GetTypeByMetadataName("System.MemoryExtensions");
        if (memoryExtensions is null)
        {
            return false;
        }

        var spanType = compilation.GetTypeByMetadataName("System.Span`1");
        var readOnlySpanType = compilation.GetTypeByMetadataName("System.ReadOnlySpan`1");

        if (spanType is null && readOnlySpanType is null)
        {
            return false;
        }

        var hasArrayAsSpan = memoryExtensions
            .GetMembers("AsSpan")
            .OfType<IMethodSymbol>()
            .Any(method =>
                method.Parameters.Length > 0
                && method.Parameters[0].Type is IArrayTypeSymbol
                && (
                    IsSpanType(method.ReturnType, spanType)
                    || IsSpanType(method.ReturnType, readOnlySpanType)
                )
            );
        if (!hasArrayAsSpan)
        {
            return false;
        }

        return HasSpanToArray(spanType, readOnlySpanType, memoryExtensions);
    }

    private static bool HasSpanToArray(
        INamedTypeSymbol? spanType,
        INamedTypeSymbol? readOnlySpanType,
        INamedTypeSymbol memoryExtensions
    )
    {
        if (HasToArrayMember(spanType) || HasToArrayMember(readOnlySpanType))
        {
            return true;
        }

        return memoryExtensions
            .GetMembers("ToArray")
            .OfType<IMethodSymbol>()
            .Any(method =>
                method.Parameters.Length == 1
                && (
                    IsSpanType(method.Parameters[0].Type, spanType)
                    || IsSpanType(method.Parameters[0].Type, readOnlySpanType)
                )
            );
    }

    private static bool HasToArrayMember(INamedTypeSymbol? spanType)
    {
        if (spanType is null)
        {
            return false;
        }

        return spanType
            .GetMembers("ToArray")
            .OfType<IMethodSymbol>()
            .Any(method => method.Parameters.Length == 0);
    }

    private static bool IsSpanType(ITypeSymbol typeSymbol, INamedTypeSymbol? spanType)
    {
        if (spanType is null)
        {
            return false;
        }

        return SymbolEqualityComparer.Default.Equals(typeSymbol.OriginalDefinition, spanType);
    }
}
