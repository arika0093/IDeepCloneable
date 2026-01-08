using System;
using System.Collections.Generic;
using System.Linq;
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

                var classInfo = CreateClassInfo(currentType, context.SemanticModel.Compilation);
                if (classInfo != null)
                {
                    classInfoList.Add(classInfo);
                    
                    foreach (var prop in classInfo.Properties)
                    {
                        if (prop.NeedsDeepClone && !prop.IsImmutable)
                        {
                            var typeName = ExtractBaseTypeName(prop.TypeFullName);
                            var childType = FindTypeByFullName(context.SemanticModel.Compilation, typeName);
                            if (childType != null && !processedTypes.Contains(GetFullTypeName(childType)))
                            {
                                typesToProcess.Enqueue(childType);
                            }
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
        if (typeFullName.Contains("System.Collections.Generic.List<"))
        {
            return ExtractGenericArgument(typeFullName, 0);
        }
        
        if (typeFullName.Contains("System.Collections.Generic.Dictionary<"))
        {
            return ExtractGenericArgument(typeFullName, 1);
        }
        
        if (typeFullName.EndsWith("[]"))
        {
            return typeFullName.Substring(0, typeFullName.Length - 2);
        }
        
        return typeFullName;
    }
    
    private static string ExtractGenericArgument(string typeFullName, int index)
    {
        var startIndex = typeFullName.IndexOf('<');
        var endIndex = typeFullName.LastIndexOf('>');
        if (startIndex >= 0 && endIndex > startIndex)
        {
            var args = typeFullName.Substring(startIndex + 1, endIndex - startIndex - 1);
            var parts = args.Split(',');
            if (index < parts.Length)
            {
                return parts[index].Trim();
            }
        }
        return typeFullName;
    }

    private static ClassInfo? CreateClassInfo(INamedTypeSymbol typeSymbol, Compilation compilation)
    {
        var properties = GetProperties(typeSymbol, compilation);
        var fullName = GetFullTypeName(typeSymbol);
        
        var hasDeepCloneableAttribute = typeSymbol.GetAttributes()
            .Any(attr => attr.AttributeClass?.Name == DeepCloneableAttributeMetadataName);

        var baseHasDeepClone = false;
        var current = typeSymbol.BaseType;
        while (current != null && current.SpecialType != SpecialType.System_Object)
        {
            if (current.GetAttributes().Any(attr => attr.AttributeClass?.Name == DeepCloneableAttributeMetadataName))
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
            Properties = new EquatableArray<PropertyInfo>(properties),
            IsNullable = typeSymbol.NullableAnnotation == NullableAnnotation.Annotated,
            IsRecord = typeSymbol.IsRecord,
            IsValueType = typeSymbol.IsValueType,
            IsAllImmutable = properties.All(p => p.IsImmutable),
            IsCollection = IsCollectionType(typeSymbol),
            NeedsDeepCloneMethod = hasDeepCloneableAttribute || baseHasDeepClone,
            IsAbstract = typeSymbol.IsAbstract,
            IsSealed = typeSymbol.IsSealed,
            BaseHasDeepClone = baseHasDeepClone
        };
    }

    private static List<PropertyInfo> GetProperties(INamedTypeSymbol typeSymbol, Compilation compilation)
    {
        var properties = new List<PropertyInfo>();

        foreach (var member in typeSymbol.GetMembers())
        {
            ITypeSymbol? memberType = null;
            string? memberName = null;

            if (member is IPropertySymbol propSymbol && !propSymbol.IsStatic)
            {
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
            }
        }

        return properties;
    }

    private static string GetFullTypeName(ITypeSymbol typeSymbol)
    {
        return "global::" + typeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat.WithGlobalNamespaceStyle(SymbolDisplayGlobalNamespaceStyle.Omitted));
    }

    private static string GetNamespace(INamedTypeSymbol typeSymbol)
    {
        return typeSymbol.ContainingNamespace?.ToDisplayString() ?? string.Empty;
    }

    private static bool IsImmutableType(ITypeSymbol typeSymbol)
    {
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
        var typeName = fullTypeName.Replace("global::", "");
        
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
                    var symbolFullName = GetFullTypeName(symbol).Replace("global::", "");
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
