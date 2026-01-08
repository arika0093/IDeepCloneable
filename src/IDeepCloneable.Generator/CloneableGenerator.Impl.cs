using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace IDeepCloneable.Generator;

public partial class CloneableGenerator
{
    /// <summary>
    /// Represents metadata about a class that needs deep cloning support.
    /// </summary>
    private record ClassInfo : IEquatable<ClassInfo>
    {
        /// <summary>Simple class name without namespace.</summary>
        public required string ClassName { get; init; }
        
        /// <summary>Fully qualified class name starting with global::.</summary>
        public required string FullClassName { get; init; }
        
        /// <summary>Namespace of the class.</summary>
        public required string Namespace { get; init; }
        
        /// <summary>List of child property/field type names (full names). Only direct children, not grandchildren.</summary>
        public required EquatableArray<PropertyInfo> Properties { get; init; }
        
        /// <summary>Whether the type is nullable.</summary>
        public required bool IsNullable { get; init; }
        
        /// <summary>Whether the type is a record.</summary>
        public required bool IsRecord { get; init; }
        
        /// <summary>Whether the type is a value type.</summary>
        public required bool IsValueType { get; init; }
        
        /// <summary>Whether all nested types are value types or immutable types (like string).</summary>
        public required bool IsAllImmutable { get; init; }
        
        /// <summary>Whether the type is a collection (has collection initializer).</summary>
        public required bool IsCollection { get; init; }
        
        /// <summary>Whether the type has [DeepCloneable] attribute or inherits from a [DeepCloneable] class.</summary>
        public required bool NeedsDeepCloneMethod { get; init; }
        
        /// <summary>Whether the type is abstract.</summary>
        public required bool IsAbstract { get; init; }
        
        /// <summary>Whether the type is sealed.</summary>
        public required bool IsSealed { get; init; }
        
        /// <summary>Whether the base type has DeepClone method.</summary>
        public required bool BaseHasDeepClone { get; init; }
    }

    /// <summary>
    /// Represents metadata about a property or field.
    /// </summary>
    private record PropertyInfo : IEquatable<PropertyInfo>
    {
        /// <summary>Name of the property/field.</summary>
        public required string Name { get; init; }
        
        /// <summary>Fully qualified type name.</summary>
        public required string TypeFullName { get; init; }
        
        /// <summary>Whether the property/field is nullable.</summary>
        public required bool IsNullable { get; init; }
        
        /// <summary>Whether the type needs deep cloning.</summary>
        public required bool NeedsDeepClone { get; init; }
        
        /// <summary>Whether this is a value type or immutable type.</summary>
        public required bool IsImmutable { get; init; }
    }

    private static EquatableArray<ClassInfo>? GetRelationalAllClassInfo(GeneratorAttributeSyntaxContext context)
    {
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
                    
                    // Enqueue child types for processing
                    foreach (var prop in classInfo.Properties)
                    {
                        if (prop.NeedsDeepClone && !prop.IsImmutable)
                        {
                            // Extract the actual type name from collections
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
        // Extract inner type from List<T>, Dictionary<TKey, TValue>, arrays, etc.
        if (typeFullName.Contains("System.Collections.Generic.List<"))
        {
            return ExtractGenericArgument(typeFullName, 0);
        }
        
        if (typeFullName.Contains("System.Collections.Generic.Dictionary<"))
        {
            // For dictionary, we care about both types but focus on value type for cloning
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
                // Only include explicitly declared fields, not compiler-generated backing fields
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
        // Enums are immutable
        if (typeSymbol.TypeKind == TypeKind.Enum)
            return true;

        // Value types are immutable by default (excluding structs with mutable fields)
        if (typeSymbol.IsValueType && typeSymbol.SpecialType != SpecialType.None)
            return true;

        // String is immutable
        if (typeSymbol.SpecialType == SpecialType.System_String)
            return true;

        // Check for primitive types
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

        // Check for known immutable types
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
        // Remove global:: prefix
        var typeName = fullTypeName.Replace("global::", "");
        
        // Try to get the type by metadata name
        var type = compilation.GetTypeByMetadataName(typeName);
        if (type != null)
            return type;
        
        // For types in the current assembly, search through all syntax trees
        foreach (var tree in compilation.SyntaxTrees)
        {
            var model = compilation.GetSemanticModel(tree);
            var root = tree.GetRoot();
            
            var classDeclarations = root.DescendantNodes()
                .OfType<Microsoft.CodeAnalysis.CSharp.Syntax.BaseTypeDeclarationSyntax>();
            
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
