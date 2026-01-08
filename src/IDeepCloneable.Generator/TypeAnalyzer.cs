using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace IDeepCloneable.Generator;

/// <summary>
/// Analyzes types and extracts metadata needed for deep clone generation.
/// </summary>
internal static class TypeAnalyzer
{
    private const string DeepCloneableAttributeMetadataName = "DeepCloneableAttribute";

    public static EquatableArray<ClassInfo>? GetRelationalAllClassInfo(GeneratorAttributeSyntaxContext context)
    {
        // Extracts information from types marked with [DeepCloneable] and all reachable types.
        // "Reachable" is defined as:
        //   * The class itself marked with [DeepCloneable] (A)
        //   * All classes that inherit from A
        //   * All classes referenced by properties/fields of A (recursively)
        
        try
        {
            var targetSymbol = context.TargetSymbol as INamedTypeSymbol;
            if (targetSymbol == null)
                return null;

            var classInfoList = new List<ClassInfo>();
            var processedTypes = new HashSet<string>();
            var typesToProcess = new Queue<INamedTypeSymbol>();
            
            typesToProcess.Enqueue(targetSymbol);

            while (typesToProcess.Count > 0)
            {
                var currentType = typesToProcess.Dequeue();
                var fullName = GetFullTypeName(currentType);
                
                if (processedTypes.Contains(fullName))
                    continue;
                    
                processedTypes.Add(fullName);

                var classInfo = CreateClassInfo(currentType, context.SemanticModel.Compilation, out var childTypes);
                if (classInfo != null)
                {
                    classInfoList.Add(classInfo);
                    
                    // Enqueue child types discovered during property analysis
                    foreach (var childType in childTypes)
                    {
                        if (!processedTypes.Contains(GetFullTypeName(childType)))
                        {
                            typesToProcess.Enqueue(childType);
                        }
                    }
                }
            }

            return new EquatableArray<ClassInfo>(classInfoList);
        }
        catch
        {
            return null;
        }
    }
    
    private static string ExtractBaseTypeName(string typeFullName)
    {
        // Remove nullable marker if present
        var cleanTypeName = typeFullName.TrimEnd('?');
        
        if (cleanTypeName.Contains("System.Collections.Generic.List<"))
        {
            return ExtractGenericArgument(cleanTypeName, 0);
        }
        
        if (cleanTypeName.Contains("System.Collections.Generic.Dictionary<"))
        {
            // For dictionary, extract the value type (index 1)
            return ExtractGenericArgument(cleanTypeName, 1);
        }
        
        if (cleanTypeName.EndsWith("[]"))
        {
            return cleanTypeName.Substring(0, cleanTypeName.Length - 2);
        }
        
        return cleanTypeName;
    }
    
    private static string ExtractDictionaryKeyType(string typeFullName)
    {
        var cleanTypeName = typeFullName.TrimEnd('?');
        if (cleanTypeName.Contains("System.Collections.Generic.Dictionary<"))
        {
            return ExtractGenericArgument(cleanTypeName, 0);
        }
        return cleanTypeName;
    }
    
    private static string ExtractGenericArgument(string typeFullName, int index)
    {
        var startIndex = typeFullName.IndexOf('<');
        var endIndex = typeFullName.LastIndexOf('>');
        if (startIndex >= 0 && endIndex > startIndex)
        {
            var args = typeFullName.Substring(startIndex + 1, endIndex - startIndex - 1);
            
            // Handle nested generics by splitting carefully
            var parts = new List<string>();
            var depth = 0;
            var current = new StringBuilder();
            
            foreach (var c in args)
            {
                if (c == '<')
                {
                    depth++;
                    current.Append(c);
                }
                else if (c == '>')
                {
                    depth--;
                    current.Append(c);
                }
                else if (c == ',' && depth == 0)
                {
                    parts.Add(current.ToString().Trim().TrimEnd('?'));
                    current.Clear();
                }
                else
                {
                    current.Append(c);
                }
            }
            
            if (current.Length > 0)
            {
                parts.Add(current.ToString().Trim().TrimEnd('?'));
            }
            
            if (index < parts.Count)
            {
                return parts[index];
            }
        }
        return typeFullName;
    }

    private static ClassInfo? CreateClassInfo(INamedTypeSymbol typeSymbol, Compilation compilation, out List<INamedTypeSymbol> childTypes)
    {
        var properties = GetProperties(typeSymbol, compilation, out childTypes);
        var fullName = GetFullTypeName(typeSymbol);
        
        var hasDeepCloneableAttribute = typeSymbol.GetAttributes()
            .Any(attr => attr.AttributeClass?.Name == DeepCloneableAttributeMetadataName);

        // Check if this type already has a DeepClone method
        var alreadyHasDeepClone = typeSymbol.GetMembers("DeepClone")
            .OfType<IMethodSymbol>()
            .Any(m => m.Parameters.Length == 0 && m.ReturnType.Equals(typeSymbol, SymbolEqualityComparer.Default));

        // Check if base class has DeepClone method (either via attribute or manual implementation)
        var baseHasDeepClone = false;
        var current = typeSymbol.BaseType;
        while (current != null && current.SpecialType != SpecialType.System_Object)
        {
            // Check for [DeepCloneable] attribute
            if (current.GetAttributes().Any(attr => attr.AttributeClass?.Name == DeepCloneableAttributeMetadataName))
            {
                baseHasDeepClone = true;
                break;
            }
            
            // Check for manual DeepClone method implementation
            if (current.GetMembers("DeepClone")
                .OfType<IMethodSymbol>()
                .Any(m => m.Parameters.Length == 0))
            {
                baseHasDeepClone = true;
                break;
            }
            
            current = current.BaseType;
        }

        return new ClassInfo
        {
            ClassName = typeSymbol.Name,
            FullClassName = fullName,
            Namespace = GetNamespace(typeSymbol),
            ContainingTypeNames = new EquatableArray<string>(GetContainingTypeNames(typeSymbol)),
            Properties = new EquatableArray<PropertyInfo>(properties),
            IsNullable = typeSymbol.NullableAnnotation == NullableAnnotation.Annotated,
            IsRecord = typeSymbol.IsRecord,
            IsValueType = typeSymbol.IsValueType,
            IsAllImmutable = properties.All(p => p.IsImmutable),
            IsCollection = IsCollectionType(typeSymbol),
            NeedsDeepCloneMethod = hasDeepCloneableAttribute || baseHasDeepClone,
            IsAbstract = typeSymbol.IsAbstract,
            IsSealed = typeSymbol.IsSealed,
            BaseHasDeepClone = baseHasDeepClone,
            AlreadyHasDeepClone = alreadyHasDeepClone
        };
    }

    private static List<PropertyInfo> GetProperties(INamedTypeSymbol typeSymbol, Compilation compilation, out List<INamedTypeSymbol> childTypes)
    {
        var properties = new List<PropertyInfo>();
        childTypes = new List<INamedTypeSymbol>();

        foreach (var member in typeSymbol.GetMembers())
        {
            ITypeSymbol? memberType = null;
            string? memberName = null;

            if (member is IPropertySymbol propSymbol && !propSymbol.IsStatic)
            {
                // Skip indexers (this[])
                if (propSymbol.IsIndexer)
                    continue;
                    
                // Skip explicitly implemented interface properties (have dots in MetadataName)
                if (propSymbol.MetadataName.Contains("."))
                    continue;
                    
                // Skip properties without a setter (can't be cloned into)
                if (propSymbol.SetMethod == null)
                    continue;
                    
                memberType = propSymbol.Type;
                memberName = propSymbol.Name;
            }
            else if (member is IFieldSymbol fieldSymbol && !fieldSymbol.IsStatic && !fieldSymbol.IsConst && !fieldSymbol.IsImplicitlyDeclared)
            {
                memberType = fieldSymbol.Type;
                memberName = fieldSymbol.Name;
            }

            if (memberType != null && memberName != null)
            {
                var isImmutable = IsImmutableType(memberType);
                var needsDeepClone = !isImmutable;

                properties.Add(new PropertyInfo
                {
                    Name = memberName,
                    TypeFullName = GetFullTypeName(memberType),
                    IsNullable = memberType.NullableAnnotation == NullableAnnotation.Annotated,
                    NeedsDeepClone = needsDeepClone,
                    IsImmutable = isImmutable
                });
                
                // Extract child types for further processing
                if (needsDeepClone)
                {
                    ExtractChildTypes(memberType, childTypes);
                }
            }
        }

        return properties;
    }
    
    private static void ExtractChildTypes(ITypeSymbol typeSymbol, List<INamedTypeSymbol> childTypes)
    {
        // Handle generic types (List<T>, Dictionary<TKey, TValue>, etc.)
        if (typeSymbol is INamedTypeSymbol namedType && namedType.IsGenericType)
        {
            // Add all type arguments
            foreach (var typeArg in namedType.TypeArguments)
            {
                if (typeArg is INamedTypeSymbol argNamedType && !IsImmutableType(typeArg))
                {
                    childTypes.Add(argNamedType);
                }
            }
        }
        // Handle arrays
        else if (typeSymbol is IArrayTypeSymbol arrayType)
        {
            if (arrayType.ElementType is INamedTypeSymbol elementType && !IsImmutableType(arrayType.ElementType))
            {
                childTypes.Add(elementType);
            }
        }
        // Handle regular types (including nullable reference types)
        // For nullable reference types (ContactInfo?), the type symbol is still ContactInfo
        else if (typeSymbol is INamedTypeSymbol regularType && !IsImmutableType(typeSymbol))
        {
            childTypes.Add(regularType);
        }
    }

    private static string GetFullTypeName(ITypeSymbol typeSymbol)
    {
        // Handle array types specially
        if (typeSymbol is IArrayTypeSymbol arrayType)
        {
            var elementTypeName = GetFullTypeName(arrayType.ElementType);
            return $"{elementTypeName}[]";
        }
        
        // For primitive types, use the CLR type name instead of the C# keyword
        // because global::int is invalid (must be global::System.Int32)
        var displayString = typeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat.WithGlobalNamespaceStyle(SymbolDisplayGlobalNamespaceStyle.Omitted));
        
        // Map C# keywords to CLR type names for use with global::
        displayString = displayString switch
        {
            "bool" => "System.Boolean",
            "byte" => "System.Byte",
            "sbyte" => "System.SByte",
            "char" => "System.Char",
            "decimal" => "System.Decimal",
            "double" => "System.Double",
            "float" => "System.Single",
            "int" => "System.Int32",
            "uint" => "System.UInt32",
            "long" => "System.Int64",
            "ulong" => "System.UInt64",
            "short" => "System.Int16",
            "ushort" => "System.UInt16",
            "object" => "System.Object",
            "string" => "System.String",
            _ => displayString
        };
        
        return "global::" + displayString;
    }

    private static string GetNamespace(INamedTypeSymbol typeSymbol)
    {
        // Return only the actual namespace, not containing types
        // Return empty string for global namespace
        if (typeSymbol.ContainingNamespace == null || typeSymbol.ContainingNamespace.IsGlobalNamespace)
            return string.Empty;
            
        return typeSymbol.ContainingNamespace.ToDisplayString();
    }
    
    private static List<string> GetContainingTypeNames(INamedTypeSymbol typeSymbol)
    {
        var containingTypes = new List<string>();
        var containingType = typeSymbol.ContainingType;
        while (containingType != null)
        {
            containingTypes.Insert(0, containingType.Name);
            containingType = containingType.ContainingType;
        }
        return containingTypes;
    }

    private static bool IsImmutableType(ITypeSymbol typeSymbol)
    {
        // System.Object cannot be cloned (it's the base type)
        if (typeSymbol.SpecialType == SpecialType.System_Object)
            return true;
            
        if (typeSymbol.TypeKind == TypeKind.Enum)
            return true;

        if (typeSymbol.IsValueType && typeSymbol.SpecialType != SpecialType.None)
            return true;

        if (typeSymbol.SpecialType == SpecialType.System_String)
            return true;

        switch (typeSymbol.SpecialType)
        {
            case SpecialType.System_Boolean:
            case SpecialType.System_Char:
            case SpecialType.System_SByte:
            case SpecialType.System_Byte:
            case SpecialType.System_Int16:
            case SpecialType.System_UInt16:
            case SpecialType.System_Int32:
            case SpecialType.System_UInt32:
            case SpecialType.System_Int64:
            case SpecialType.System_UInt64:
            case SpecialType.System_Decimal:
            case SpecialType.System_Single:
            case SpecialType.System_Double:
            case SpecialType.System_DateTime:
                return true;
        }

        var fullName = GetFullTypeName(typeSymbol);
        if (fullName.StartsWith("global::System.DateTimeOffset") ||
            fullName.StartsWith("global::System.TimeSpan") ||
            fullName.StartsWith("global::System.Guid"))
        {
            return true;
        }

        return false;
    }

    private static bool IsCollectionType(ITypeSymbol typeSymbol)
    {
        var fullName = GetFullTypeName(typeSymbol);
        return fullName.Contains("System.Collections.Generic.List<") ||
               fullName.Contains("System.Collections.Generic.Dictionary<") ||
               fullName.Contains("System.Collections.Generic.HashSet<") ||
               fullName.Contains("[]");
    }

    private static INamedTypeSymbol? FindTypeByFullName(Compilation compilation, string fullTypeName)
    {
        var typeName = fullTypeName.Replace("global::", "").TrimEnd('?');
        
        var type = compilation.GetTypeByMetadataName(typeName);
        if (type != null)
            return type;
        
        foreach (var tree in compilation.SyntaxTrees)
        {
            var model = compilation.GetSemanticModel(tree);
            var root = tree.GetRoot();
            
            var classDeclarations = root.DescendantNodes()
                .OfType<BaseTypeDeclarationSyntax>();
            
            foreach (var classDecl in classDeclarations)
            {
                var symbol = model.GetDeclaredSymbol(classDecl) as INamedTypeSymbol;
                if (symbol != null)
                {
                    var symbolFullName = GetFullTypeName(symbol).Replace("global::", "").TrimEnd('?');
                    if (symbolFullName == typeName)
                    {
                        return symbol;
                    }
                }
            }
        }
        
        return null;
    }
}
