using Microsoft.CodeAnalysis;
using System.Collections.Generic;

namespace IDeepCloneable.Generator;

/// <summary>
/// Discovers all types reachable from a root type that need CloneInternal methods
/// </summary>
internal class TypeDiscovery
{
    private readonly HashSet<string> _visitedTypes = new HashSet<string>();
    private readonly TypeRegistry _registry;

    public TypeDiscovery(TypeRegistry registry)
    {
        _registry = registry;
    }

    public void DiscoverTypes(INamedTypeSymbol rootType, bool hasDeepCloneableAttribute)
    {
        var fullName = rootType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat).Replace("global::", "");
        
        if (_visitedTypes.Contains(fullName))
            return;

        _visitedTypes.Add(fullName);

        // Register the root type
        _registry.RegisterType(rootType, hasDeepCloneableAttribute);

        // Get all properties
        var properties = GetCloneableProperties(rootType);

        // Discover types from each property
        foreach (var property in properties)
        {
            DiscoverTypesFromSymbol(property.Type);
        }
    }

    private void DiscoverTypesFromSymbol(ITypeSymbol typeSymbol)
    {
        // Skip value types and immutable types
        if (IsValueOrImmutableType(typeSymbol))
            return;

        // Handle arrays
        if (typeSymbol is IArrayTypeSymbol arrayType)
        {
            DiscoverTypesFromSymbol(arrayType.ElementType);
            return;
        }

        // Handle named types
        if (typeSymbol is INamedTypeSymbol namedType)
        {
            var fullName = namedType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat).Replace("global::", "");
            
            if (_visitedTypes.Contains(fullName))
                return;

            // Skip system collection types - we don't generate CloneInternal for them
            if (IsSystemCollectionType(fullName))
            {
                // For collections, discover the element types
                if (namedType.TypeArguments.Length > 0)
                {
                    foreach (var typeArg in namedType.TypeArguments)
                    {
                        DiscoverTypesFromSymbol(typeArg);
                    }
                }
                return;
            }

            // Skip all System namespace types
            if (fullName.StartsWith("System."))
            {
                return;
            }

            // For reference types with properties, register and discover recursively
            if (!namedType.IsValueType && namedType.SpecialType != SpecialType.System_String)
            {
                var properties = GetCloneableProperties(namedType);
                if (properties.Count > 0)
                {
                    DiscoverTypes(namedType, false);
                }
            }

            // Discover generic type arguments
            if (namedType.TypeArguments.Length > 0)
            {
                foreach (var typeArg in namedType.TypeArguments)
                {
                    DiscoverTypesFromSymbol(typeArg);
                }
            }
        }
    }

    private static bool IsValueOrImmutableType(ITypeSymbol typeSymbol)
    {
        if (typeSymbol.IsValueType)
            return true;

        if (typeSymbol.SpecialType == SpecialType.System_String)
            return true;

        var fullName = typeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        return fullName is "global::System.DateTime"
            or "global::System.DateTimeOffset"
            or "global::System.TimeSpan"
            or "global::System.Guid";
    }

    private static bool IsSystemCollectionType(string fullName)
    {
        return fullName.StartsWith("global::System.Collections.Generic.List<")
            || fullName.StartsWith("global::System.Collections.Generic.Dictionary<")
            || fullName.StartsWith("global::System.Collections.Generic.HashSet<")
            || fullName.StartsWith("global::System.Collections.Generic.Stack<")
            || fullName.StartsWith("global::System.Collections.Generic.Queue<")
            || fullName.StartsWith("global::System.Collections.Generic.SortedSet<")
            || fullName.StartsWith("global::System.Collections.ObjectModel.")
            || fullName.StartsWith("global::System.Collections.Immutable.");
    }

    private static List<IPropertySymbol> GetCloneableProperties(INamedTypeSymbol typeSymbol)
    {
        var properties = new List<IPropertySymbol>();

        foreach (var member in typeSymbol.GetMembers())
        {
            if (member is IPropertySymbol property
                && !property.IsStatic
                && !property.IsIndexer
                && property.SetMethod != null
                && property.SetMethod.DeclaredAccessibility >= Accessibility.Internal)
            {
                properties.Add(property);
            }
        }

        return properties;
    }
}
