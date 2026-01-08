using Microsoft.CodeAnalysis;
using System.Collections.Generic;
using System.Linq;

namespace IDeepCloneable.Generator;

/// <summary>
/// Registry that tracks all types discovered during analysis that need CloneInternal methods
/// </summary>
internal class TypeRegistry
{
    private readonly Dictionary<string, TypeInfo> _types = new Dictionary<string, TypeInfo>();

    public void RegisterType(INamedTypeSymbol typeSymbol, bool hasDeepCloneableAttribute, bool isCollectionHelper = false)
    {
        var fullName = typeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat).Replace("global::", "");
        
        if (!_types.ContainsKey(fullName))
        {
            _types[fullName] = new TypeInfo(fullName, typeSymbol, hasDeepCloneableAttribute, isCollectionHelper);
        }
    }

    public bool HasCloneInternal(string fullTypeName)
    {
        return _types.ContainsKey(fullTypeName);
    }

    public IEnumerable<TypeInfo> GetAllTypes()
    {
        return _types.Values;
    }

    public TypeInfo? GetTypeInfo(string fullTypeName)
    {
        return _types.TryGetValue(fullTypeName, out var typeInfo) ? typeInfo : null;
    }
}

internal record TypeInfo(
    string FullName,
    INamedTypeSymbol Symbol,
    bool HasDeepCloneableAttribute,
    bool IsCollectionHelper = false
);
