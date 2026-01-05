using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace IDeepCloneable.Generator;

[Generator]
public class CloneableGenerator : IIncrementalGenerator
{
    private const string DeepCloneMethodName = "DeepClone";

    // Indentation constants for generated code
    // These represent the final indentation after raw string literal baseline removal (12 spaces)
    private const string PropertyIndent = "                "; // 16 spaces (4 levels: namespace/class/method/initializer)
    private const string StatementIndent = "            "; // 12 spaces (3 levels: namespace/class/method)

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // Find all partial types that implement IDeepCloneable<T>
        var classDeclarations = context
            .SyntaxProvider.CreateSyntaxProvider(
                predicate: static (s, _) => IsCandidateType(s),
                transform: static (ctx, _) => GetSemanticTargetForGeneration(ctx)
            )
            .Where(static m => m is not null);

        context.RegisterSourceOutput(
            classDeclarations,
            static (spc, source) => Execute(source!, spc)
        );
    }

    private static bool IsCandidateType(SyntaxNode node)
    {
        return (
                node is ClassDeclarationSyntax classDeclaration
                && classDeclaration.Modifiers.Any(SyntaxKind.PartialKeyword)
            )
            || (
                node is RecordDeclarationSyntax recordDeclaration
                && recordDeclaration.Modifiers.Any(SyntaxKind.PartialKeyword)
            )
            || (
                node is StructDeclarationSyntax structDeclaration
                && structDeclaration.Modifiers.Any(SyntaxKind.PartialKeyword)
            );
    }

    private static ClassInfo? GetSemanticTargetForGeneration(GeneratorSyntaxContext context)
    {
        var typeDeclaration = context.Node as TypeDeclarationSyntax;
        if (typeDeclaration is null)
            return null;

        var classSymbol = context.SemanticModel.GetDeclaredSymbol(typeDeclaration);

        if (classSymbol is null || classSymbol.IsAbstract)
            return null;

        var deepCloneableInterface = FindCloneableInterface(
            classSymbol,
            "IDeepCloneable.IDeepCloneable"
        );

        if (deepCloneableInterface is null)
            return null;

        bool hasDeepClone = HasMethodImplementation(classSymbol, DeepCloneMethodName);
        if (hasDeepClone)
            return null;

        string typeKeyword;
        if (classSymbol.IsRecord)
        {
            typeKeyword = classSymbol.IsValueType ? "record struct" : "record";
        }
        else
        {
            typeKeyword = classSymbol.IsValueType ? "struct" : "class";
        }

        return new ClassInfo(
            classSymbol.Name,
            GetNamespace(classSymbol),
            classSymbol,
            true,
            typeKeyword
        );
    }

    private static INamedTypeSymbol? FindCloneableInterface(
        INamedTypeSymbol classSymbol,
        string interfaceName
    )
    {
        // Check if any interface is IDeepCloneable.IDeepCloneable<T>
        return classSymbol.AllInterfaces.FirstOrDefault(i =>
            i.OriginalDefinition.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)
                .StartsWith("global::IDeepCloneable.IDeepCloneable<")
        );
    }

    private static bool IsCloneableType(ITypeSymbol typeSymbol)
    {
        if (typeSymbol is not INamedTypeSymbol namedType)
            return false;

        return namedType.AllInterfaces.Any(i =>
            i.OriginalDefinition.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)
                .StartsWith("global::IDeepCloneable.IDeepCloneable<")
        );
    }

    private static bool HasMethodImplementation(INamedTypeSymbol classSymbol, string methodName)
    {
        return classSymbol
            .GetMembers(methodName)
            .OfType<IMethodSymbol>()
            .Any(m => !m.IsAbstract && m.DeclaringSyntaxReferences.Any());
    }

    private static string? GetNamespace(ISymbol symbol)
    {
        var namespaceSymbol = symbol.ContainingNamespace;
        if (namespaceSymbol is null || namespaceSymbol.IsGlobalNamespace)
            return null;

        return namespaceSymbol
            .ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)
            .Replace("global::", "");
    }

    private static void Execute(ClassInfo classInfo, SourceProductionContext context)
    {
        var source = GenerateCloneMethod(classInfo);
        context.AddSource($"{classInfo.ClassName}.g.cs", SourceText.From(source, Encoding.UTF8));
    }

    private static string GenerateCloneMethod(ClassInfo classInfo)
    {
        var deepCloneMethod = classInfo.ShouldGenerateDeepClone
            ? GenerateDeepCloneMethod(classInfo)
            : string.Empty;

        if (classInfo.Namespace is not null)
        {
            return $$"""
                {{OutputFileHeaderParts}}
                namespace {{classInfo.Namespace}}
                {
                    partial {{classInfo.TypeKeyword}} {{classInfo.ClassName}} : IDeepCloneable.IDeepCloneable<{{classInfo.ClassName}}>
                    {
                {{deepCloneMethod}}
                    }
                }
                """;
        }
        else
        {
            return $$"""
                {{OutputFileHeaderParts}}
                partial {{classInfo.TypeKeyword}} {{classInfo.ClassName}} : IDeepCloneable.IDeepCloneable<{{classInfo.ClassName}}>
                {
                {{deepCloneMethod}}
                }
                """;
        }
    }

    private const string OutputFileHeaderParts = $"""
        // <auto-generated>
        // This file was generated by the IDeepCloneable source generator (ver: {ThisAssembly.AssemblyFileVersion}).
        // </auto-generated>
        #nullable disable
        #pragma warning disable

        using System;
        using System.Collections.Immutable;
        using System.Linq;

        """;

    private static string GenerateDeepCloneMethod(ClassInfo classInfo)
    {
        var properties = GetCloneableProperties(classInfo.ClassSymbol);

        bool hasInitOnlyProperties = properties.Any(p => p.SetMethod?.IsInitOnly == true);
        bool needsStatements = properties.Any(p =>
            p.Type is IArrayTypeSymbol arrayType && arrayType.Rank > 1
        );

        if (needsStatements || hasInitOnlyProperties)
        {
            if (classInfo.ClassSymbol.IsRecord && hasInitOnlyProperties)
            {
                var assignments = new List<string>();
                foreach (var property in properties)
                {
                    var expression = GenerateDeepCloneExpression(property);
                    assignments.Add($"{PropertyIndent}{property.Name} = {expression}");
                }

                var withAssignments = string.Join(",\n", assignments);

                return $$"""
                            /// <inheritdoc />
                            public {{classInfo.ClassName}} {{DeepCloneMethodName}}()
                            {
                                return this with
                                {
                    {{withAssignments}}
                                };
                            }
                    """;
            }
            else
            {
                var statements = new List<string>();
                statements.Add($"{StatementIndent}var clone = new {classInfo.ClassName}();");

                foreach (var property in properties)
                {
                    var expression = GenerateDeepCloneExpression(property);
                    statements.Add($"{StatementIndent}clone.{property.Name} = {expression};");
                }

                statements.Add($"{StatementIndent}return clone;");

                var methodBody = string.Join("\n", statements);

                return $$"""
                            /// <inheritdoc />
                            public {{classInfo.ClassName}} {{DeepCloneMethodName}}()
                            {
                    {{methodBody}}
                            }
                    """;
            }
        }
        else
        {
            var propertyAssignments = string.Join(
                ",\n",
                properties.Select(p =>
                    $"{PropertyIndent}{p.Name} = {GenerateDeepCloneExpression(p)}"
                )
            );

            return $$"""
                        /// <inheritdoc />
                        public {{classInfo.ClassName}} {{DeepCloneMethodName}}()
                        {
                            return new {{classInfo.ClassName}}
                            {
                {{propertyAssignments}}
                            };
                        }
                """;
        }
    }

    private static string GenerateDeepCloneExpression(IPropertySymbol property)
    {
        var typeSymbol = property.Type;

        if (typeSymbol is IArrayTypeSymbol arrayType)
        {
            return GenerateArrayDeepClone(property, arrayType);
        }

        if (typeSymbol is INamedTypeSymbol namedType)
        {
            var deepCloneableInterface = namedType.AllInterfaces.FirstOrDefault(i =>
                i.OriginalDefinition.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)
                    .StartsWith("global::IDeepCloneable.IDeepCloneable")
            );

            if (deepCloneableInterface is not null)
            {
                return $"this.{property.Name}?.{DeepCloneMethodName}()";
            }

            if (IsDictionaryType(namedType))
            {
                return GenerateDictionaryDeepClone(property, namedType);
            }

            if (IsCollectionType(namedType))
            {
                return GenerateCollectionDeepClone(property, namedType);
            }

            // Handle reference types with properties that need deep cloning
            if (!namedType.IsValueType && namedType.SpecialType != SpecialType.System_String)
            {
                return GenerateObjectDeepClone(property, namedType);
            }
        }

        if (typeSymbol.IsValueType || typeSymbol.SpecialType == SpecialType.System_String)
        {
            return $"this.{property.Name}";
        }

        return $"this.{property.Name}";
    }

    private static string GenerateObjectDeepClone(
        IPropertySymbol property,
        INamedTypeSymbol namedType
    )
    {
        var properties = GetCloneableProperties(namedType);

        // If no properties or all properties are just value types/strings, shallow copy is fine
        if (properties.Count == 0)
        {
            return $"this.{property.Name}";
        }

        var propertyName = property.Name;
        var typeName = namedType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

        // Generate object initializer with deep cloned properties
        var propertyAssignments = new List<string>();
        bool hasNestedObjects = false;
        foreach (var prop in properties)
        {
            var propCloneExpr = GeneratePropertyCloneExpressionForObject(prop, propertyName, 0);

            // Check if the expression contains newlines (indicating nested objects)
            if (propCloneExpr.Contains('\n'))
            {
                hasNestedObjects = true;
            }

            // Always add null-forgiving operator for ALL properties in object initializers to avoid CS8628
            // This is needed because C# 10's nullable context is very strict with object initializers
            propertyAssignments.Add($"{prop.Name} = {propCloneExpr}!");
        }

        if (propertyAssignments.Count == 0)
        {
            return $"this.{propertyName}";
        }

        // If there are nested objects, format across multiple lines with proper indentation
        if (hasNestedObjects)
        {
            var formattedAssignments = string.Join($",\n{PropertyIndent}    ", propertyAssignments);
            return $"this.{propertyName} != null ? new {typeName}\n{PropertyIndent}{{\n{PropertyIndent}    {formattedAssignments}\n{PropertyIndent}}} : null";
        }
        else
        {
            var assignments = string.Join(", ", propertyAssignments);
            return $"this.{propertyName} != null ? new {typeName} {{ {assignments} }} : null";
        }
    }

    private static string GeneratePropertyCloneExpressionForObject(
        IPropertySymbol property,
        string sourceObjectName,
        int nestingLevel
    )
    {
        var typeSymbol = property.Type;

        if (typeSymbol is IArrayTypeSymbol arrayType)
        {
            var elementType = arrayType.ElementType;
            var propertyName = property.Name;

            if (arrayType.Rank > 1)
            {
                var refArrayTypeName =
                    $"{elementType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}[{new string(',', arrayType.Rank - 1)}]";
                return $"{sourceObjectName}.{propertyName} != null ? ({refArrayTypeName}){sourceObjectName}.{propertyName}.Clone() : null";
            }

            bool isCloneable = IsCloneableType(elementType);
            if (isCloneable)
            {
                return $"{sourceObjectName}.{propertyName}?.Select(x => x?.{DeepCloneMethodName}()).ToArray()";
            }

            // Optimize for primitive and blittable types using span-based copy
            if (IsPrimitiveOrBlittableType(elementType))
            {
                return $"{sourceObjectName}.{propertyName} != null ? {sourceObjectName}.{propertyName}.AsSpan().ToArray() : null";
            }

            if (elementType.IsValueType || elementType.SpecialType == SpecialType.System_String)
            {
                return $"{sourceObjectName}.{propertyName} != null ? ({elementType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}[]){sourceObjectName}.{propertyName}.Clone() : null";
            }

            var refArrayTypeName2 =
                $"{elementType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}[]";
            return $"{sourceObjectName}.{propertyName} != null ? ({refArrayTypeName2}){sourceObjectName}.{propertyName}.Clone() : null";
        }

        if (typeSymbol is INamedTypeSymbol namedType)
        {
            if (IsCloneableType(namedType))
            {
                return $"{sourceObjectName}.{property.Name}?.{DeepCloneMethodName}()";
            }

            if (IsDictionaryType(namedType))
            {
                return GenerateDictionaryCloneForObject(property, namedType, sourceObjectName);
            }

            if (IsCollectionType(namedType))
            {
                return GenerateCollectionCloneForObject(property, namedType, sourceObjectName);
            }

            // Recursively handle nested reference types
            if (!namedType.IsValueType && namedType.SpecialType != SpecialType.System_String)
            {
                var nestedProperties = GetCloneableProperties(namedType);
                if (nestedProperties.Count > 0)
                {
                    var nestedAssignments = new List<string>();
                    foreach (var nestedProp in nestedProperties)
                    {
                        var nestedCloneExpr = GeneratePropertyCloneExpressionForObject(
                            nestedProp,
                            $"{sourceObjectName}.{property.Name}",
                            nestingLevel + 1
                        );

                        // Always add null-forgiving operator for ALL properties in object initializers to avoid CS8628
                        // This is needed because C# 10's nullable context is very strict with object initializers
                        nestedAssignments.Add($"{nestedProp.Name} = {nestedCloneExpr}!");
                    }

                    // Format nested object initializer across multiple lines with proper indentation
                    // Each nesting level adds 4 spaces of indentation beyond PropertyIndent
                    var additionalIndent = new string(' ', nestingLevel * 4);
                    var nestedIndent = PropertyIndent + additionalIndent;

                    var formattedAssignments = string.Join(
                        $",\n{nestedIndent}    ",
                        nestedAssignments
                    );
                    return $"{sourceObjectName}.{property.Name} != null ? new {namedType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}\n{nestedIndent}{{\n{nestedIndent}    {formattedAssignments}\n{nestedIndent}}} : null";
                }
            }
        }

        if (typeSymbol.IsValueType || typeSymbol.SpecialType == SpecialType.System_String)
        {
            return $"{sourceObjectName}.{property.Name}";
        }

        return $"{sourceObjectName}.{property.Name}";
    }

    private static string GenerateCollectionCloneForObject(
        IPropertySymbol property,
        INamedTypeSymbol collectionType,
        string sourceObjectName
    )
    {
        if (collectionType.TypeArguments.Length == 0)
            return $"{sourceObjectName}.{property.Name}";

        var elementType = collectionType.TypeArguments[0];
        var propertyName = property.Name;

        bool isCloneable = IsCloneableType(elementType);

        // Check if element type is a collection that needs deep cloning
        if (
            !isCloneable
            && elementType is INamedTypeSymbol namedElementType
            && IsCollectionType(namedElementType)
        )
        {
            var elementCloneExpr = GenerateNestedCollectionCloneExpression(namedElementType, "x");
            return $"{sourceObjectName}.{propertyName}?.Select(x => {elementCloneExpr}).ToList()";
        }

        if (isCloneable)
        {
            return $"{sourceObjectName}.{propertyName}?.Select(x => x?.{DeepCloneMethodName}()).ToList()";
        }

        // Check if element type is a reference type with properties that need deep cloning
        if (
            elementType is INamedTypeSymbol namedElementTypeRef
            && !namedElementTypeRef.IsValueType
            && namedElementTypeRef.SpecialType != SpecialType.System_String
        )
        {
            var properties = GetCloneableProperties(namedElementTypeRef);
            if (properties.Count > 0)
            {
                // Generate object initializer for deep cloning each element
                var propertyAssignments = new List<string>();
                foreach (var prop in properties)
                {
                    var propCloneExpr = GeneratePropertyCloneExpressionForObject(prop, "x", 0);
                    propertyAssignments.Add($"{prop.Name} = {propCloneExpr}!");
                }
                var assignmentsStr = string.Join(", ", propertyAssignments);
                return $"{sourceObjectName}.{propertyName}?.Select(x => x != null ? new {namedElementTypeRef.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)} {{ {assignmentsStr} }} : null).ToList()";
            }
        }

        return $"{sourceObjectName}.{propertyName} != null ? new System.Collections.Generic.List<{elementType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}>({sourceObjectName}.{propertyName}) : null";
    }

    private static string GenerateDictionaryCloneForObject(
        IPropertySymbol property,
        INamedTypeSymbol dictionaryType,
        string sourceObjectName
    )
    {
        if (dictionaryType.TypeArguments.Length < 2)
            return $"{sourceObjectName}.{property.Name}";

        var keyType = dictionaryType.TypeArguments[0];
        var valueType = dictionaryType.TypeArguments[1];
        var propertyName = property.Name;

        bool valueIsCloneable = IsCloneableType(valueType);

        if (valueIsCloneable)
        {
            return $"{sourceObjectName}.{propertyName}?.ToDictionary(kvp => kvp.Key, kvp => kvp.Value?.{DeepCloneMethodName}())";
        }

        // Check if value type is a reference type with properties that need deep cloning
        if (
            valueType is INamedTypeSymbol valueNamedTypeRef
            && !valueNamedTypeRef.IsValueType
            && valueNamedTypeRef.SpecialType != SpecialType.System_String
        )
        {
            var properties = GetCloneableProperties(valueNamedTypeRef);
            if (properties.Count > 0)
            {
                // Generate object initializer for deep cloning each value
                var propertyAssignments = new List<string>();
                foreach (var prop in properties)
                {
                    var propCloneExpr = GeneratePropertyCloneExpressionForObject(
                        prop,
                        "kvp.Value",
                        0
                    );
                    propertyAssignments.Add($"{prop.Name} = {propCloneExpr}!");
                }
                var assignmentsStr = string.Join(", ", propertyAssignments);
                return $"{sourceObjectName}.{propertyName}?.ToDictionary(kvp => kvp.Key, kvp => kvp.Value != null ? new {valueNamedTypeRef.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)} {{ {assignmentsStr} }} : null)";
            }
        }

        return $"{sourceObjectName}.{propertyName} != null ? new System.Collections.Generic.Dictionary<{keyType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}, {valueType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}>({sourceObjectName}.{propertyName}) : null";
    }

    private static bool IsDictionaryType(INamedTypeSymbol type)
    {
        return type.AllInterfaces.Any(i =>
            i.OriginalDefinition.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)
            == "global::System.Collections.Generic.IDictionary<TKey, TValue>"
        );
    }

    private static bool IsCollectionType(INamedTypeSymbol type)
    {
        return type.AllInterfaces.Any(i =>
            i.OriginalDefinition.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)
            == "global::System.Collections.Generic.IEnumerable<T>"
        );
    }

    private static bool IsPrimitiveOrBlittableType(ITypeSymbol type)
    {
        // Check for primitive types that can be efficiently copied with memory operations
        switch (type.SpecialType)
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
            case SpecialType.System_Single:
            case SpecialType.System_Double:
            case SpecialType.System_Decimal:
            case SpecialType.System_IntPtr:
            case SpecialType.System_UIntPtr:
                return true;
            default:
                break;
        }

        // Check for other common blittable types
        var fullName = type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        return fullName
            is "global::System.Guid"
                or "global::System.DateTime"
                or "global::System.TimeSpan"
                or "global::System.DateTimeOffset";

        // Note: For custom structs, we would need to recursively check all fields
        // For now, we're conservative and only optimize known types
    }

    private static string GenerateArrayDeepClone(
        IPropertySymbol property,
        IArrayTypeSymbol arrayType
    )
    {
        var elementType = arrayType.ElementType;
        var propertyName = property.Name;

        if (arrayType.Rank > 1)
        {
            var elementTypeName = elementType.ToDisplayString(
                SymbolDisplayFormat.FullyQualifiedFormat
            );
            var rankCommas = new string(',', arrayType.Rank - 1);
            var arrayTypeName = $"{elementTypeName}[{rankCommas}]";
            return $"this.{propertyName} != null ? ({arrayTypeName})this.{propertyName}.Clone() : null";
        }

        if (elementType is INamedTypeSymbol elementNamedType)
        {
            var deepCloneableInterface = elementNamedType.AllInterfaces.FirstOrDefault(i =>
                i.OriginalDefinition.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)
                    .StartsWith("global::IDeepCloneable.IDeepCloneable")
            );

            if (deepCloneableInterface is not null)
            {
                return $"this.{propertyName}?.Select(x => x?.{DeepCloneMethodName}()).ToArray()";
            }
        }

        // Optimize for primitive and blittable types using span-based copy
        if (IsPrimitiveOrBlittableType(elementType))
        {
            // Use span-based copy for better performance - this uses memory copy under the hood
            return $"this.{propertyName} != null ? this.{propertyName}.AsSpan().ToArray() : null";
        }

        // For string arrays, use Array.Clone (strings are immutable so shallow copy is safe)
        if (elementType.SpecialType == SpecialType.System_String)
        {
            var arrayTypeName =
                $"{elementType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}[]";
            return $"this.{propertyName} != null ? ({arrayTypeName})this.{propertyName}.Clone() : null";
        }

        // For other value types (structs that may contain references), use Array.Clone
        if (elementType.IsValueType)
        {
            var arrayTypeName =
                $"{elementType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}[]";
            return $"this.{propertyName} != null ? ({arrayTypeName})this.{propertyName}.Clone() : null";
        }

        var refArrayTypeName =
            $"{elementType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}[]";
        return $"this.{propertyName} != null ? ({refArrayTypeName})this.{propertyName}.Clone() : null";
    }

    private static string GenerateDictionaryDeepClone(
        IPropertySymbol property,
        INamedTypeSymbol dictionaryType
    )
    {
        if (dictionaryType.TypeArguments.Length < 2)
            return $"this.{property.Name}";

        var keyType = dictionaryType.TypeArguments[0];
        var valueType = dictionaryType.TypeArguments[1];
        var propertyName = property.Name;

        bool valueIsCloneable = false;
        if (valueType is INamedTypeSymbol valueNamedType)
        {
            var deepCloneableInterface = valueNamedType.AllInterfaces.FirstOrDefault(i =>
                i.OriginalDefinition.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)
                    .StartsWith("global::IDeepCloneable.IDeepCloneable")
            );
            valueIsCloneable = deepCloneableInterface is not null;
        }

        var typeName = dictionaryType.OriginalDefinition.ToDisplayString(
            SymbolDisplayFormat.FullyQualifiedFormat
        );

        // Check if value type is a reference type with properties that need deep cloning
        bool valueNeedsDeepClone = false;
        string valueCloneExpression = "";
        if (
            !valueIsCloneable
            && valueType is INamedTypeSymbol valueNamedTypeRef
            && !valueNamedTypeRef.IsValueType
            && valueNamedTypeRef.SpecialType != SpecialType.System_String
        )
        {
            var properties = GetCloneableProperties(valueNamedTypeRef);
            if (properties.Count > 0)
            {
                valueNeedsDeepClone = true;
                var propertyAssignments = new List<string>();
                foreach (var prop in properties)
                {
                    var propCloneExpr = GeneratePropertyCloneExpressionForObject(
                        prop,
                        "kvp.Value",
                        0
                    );
                    propertyAssignments.Add($"{prop.Name} = {propCloneExpr}!");
                }
                var assignmentsStr = string.Join(", ", propertyAssignments);
                valueCloneExpression =
                    $"kvp.Value != null ? new {valueNamedTypeRef.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)} {{ {assignmentsStr} }} : null";
            }
        }

        if (typeName.StartsWith("global::System.Collections.Immutable.ImmutableDictionary<"))
        {
            if (valueIsCloneable)
            {
                return $"this.{propertyName}?.ToImmutableDictionary(kvp => kvp.Key, kvp => kvp.Value?.{DeepCloneMethodName}())";
            }
            if (valueNeedsDeepClone)
            {
                return $"this.{propertyName}?.ToImmutableDictionary(kvp => kvp.Key, kvp => {valueCloneExpression})";
            }
            return $"this.{propertyName}";
        }

        if (typeName.StartsWith("global::System.Collections.ObjectModel.ReadOnlyDictionary<"))
        {
            if (valueIsCloneable)
            {
                return $"this.{propertyName} != null ? new System.Collections.ObjectModel.ReadOnlyDictionary<{keyType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}, {valueType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}>(this.{propertyName}.ToDictionary(kvp => kvp.Key, kvp => kvp.Value?.{DeepCloneMethodName}())) : null";
            }
            if (valueNeedsDeepClone)
            {
                return $"this.{propertyName} != null ? new System.Collections.ObjectModel.ReadOnlyDictionary<{keyType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}, {valueType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}>(this.{propertyName}.ToDictionary(kvp => kvp.Key, kvp => {valueCloneExpression})) : null";
            }
            // ReadOnlyDictionary wraps a mutable dictionary, so we need to clone it even for value types
            return $"this.{propertyName} != null ? new System.Collections.ObjectModel.ReadOnlyDictionary<{keyType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}, {valueType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}>(new System.Collections.Generic.Dictionary<{keyType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}, {valueType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}>(this.{propertyName})) : null";
        }

        if (valueIsCloneable)
        {
            return $"this.{propertyName}?.ToDictionary(kvp => kvp.Key, kvp => kvp.Value?.{DeepCloneMethodName}())";
        }

        if (valueNeedsDeepClone)
        {
            return $"this.{propertyName}?.ToDictionary(kvp => kvp.Key, kvp => {valueCloneExpression})";
        }

        return $"this.{propertyName} != null ? new System.Collections.Generic.Dictionary<{keyType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}, {valueType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}>(this.{propertyName}) : null";
    }

    private static string GenerateCollectionDeepClone(
        IPropertySymbol property,
        INamedTypeSymbol collectionType
    )
    {
        if (collectionType.TypeArguments.Length == 0)
            return $"this.{property.Name}";

        var elementType = collectionType.TypeArguments[0];
        var propertyName = property.Name;
        var typeName = collectionType.OriginalDefinition.ToDisplayString(
            SymbolDisplayFormat.FullyQualifiedFormat
        );

        bool isCloneable = false;
        if (elementType is INamedTypeSymbol elementNamedType)
        {
            var deepCloneableInterface = elementNamedType.AllInterfaces.FirstOrDefault(i =>
                i.OriginalDefinition.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)
                    .StartsWith("global::IDeepCloneable.IDeepCloneable")
            );
            isCloneable = deepCloneableInterface is not null;
        }

        if (typeName == "global::System.Collections.Generic.Stack<T>")
            return GenerateStackClone(propertyName, elementType, isCloneable);

        if (typeName == "global::System.Collections.Generic.Queue<T>")
            return GenerateQueueClone(propertyName, elementType, isCloneable);

        if (typeName == "global::System.Collections.Generic.HashSet<T>")
            return GenerateHashSetClone(propertyName, elementType, isCloneable);

        if (typeName == "global::System.Collections.Generic.SortedSet<T>")
            return GenerateSortedSetClone(propertyName, elementType, isCloneable);

        if (typeName == "global::System.Collections.ObjectModel.ObservableCollection<T>")
            return GenerateObservableCollectionClone(propertyName, elementType, isCloneable);

        if (typeName == "global::System.Collections.ObjectModel.ReadOnlyCollection<T>")
            return GenerateReadOnlyCollectionClone(propertyName, elementType, isCloneable);

        if (typeName.StartsWith("global::System.Collections.Immutable.ImmutableList<"))
            return GenerateImmutableListClone(propertyName, elementType, isCloneable);

        if (typeName.StartsWith("global::System.Collections.Immutable.ImmutableArray<"))
            return GenerateImmutableArrayClone(propertyName, elementType, isCloneable);

        if (typeName.StartsWith("global::System.Collections.Immutable.ImmutableHashSet<"))
            return GenerateImmutableHashSetClone(propertyName, elementType, isCloneable);

        if (typeName.StartsWith("global::System.Collections.Immutable.ImmutableQueue<"))
            return GenerateImmutableQueueClone(propertyName, elementType, isCloneable);

        if (typeName.StartsWith("global::System.Collections.Immutable.ImmutableStack<"))
            return GenerateImmutableStackClone(propertyName, elementType, isCloneable);

        return GenerateDefaultListClone(propertyName, elementType, isCloneable);
    }

    private static string GenerateStackClone(
        string propertyName,
        ITypeSymbol elementType,
        bool isCloneable
    )
    {
        if (isCloneable)
        {
            return $"this.{propertyName} != null ? new System.Collections.Generic.Stack<{elementType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}>(this.{propertyName}.Reverse().Select(x => x?.{DeepCloneMethodName}())) : null";
        }
        return $"this.{propertyName} != null ? new System.Collections.Generic.Stack<{elementType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}>(this.{propertyName}.Reverse()) : null";
    }

    private static string GenerateQueueClone(
        string propertyName,
        ITypeSymbol elementType,
        bool isCloneable
    )
    {
        if (isCloneable)
        {
            return $"this.{propertyName} != null ? new System.Collections.Generic.Queue<{elementType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}>(this.{propertyName}.Select(x => x?.{DeepCloneMethodName}())) : null";
        }
        return $"this.{propertyName} != null ? new System.Collections.Generic.Queue<{elementType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}>(this.{propertyName}) : null";
    }

    private static string GenerateHashSetClone(
        string propertyName,
        ITypeSymbol elementType,
        bool isCloneable
    )
    {
        if (isCloneable)
        {
            return $"this.{propertyName} != null ? new System.Collections.Generic.HashSet<{elementType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}>(this.{propertyName}.Select(x => x?.{DeepCloneMethodName}())) : null";
        }
        return $"this.{propertyName} != null ? new System.Collections.Generic.HashSet<{elementType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}>(this.{propertyName}) : null";
    }

    private static string GenerateSortedSetClone(
        string propertyName,
        ITypeSymbol elementType,
        bool isCloneable
    )
    {
        if (isCloneable)
        {
            return $"this.{propertyName} != null ? new System.Collections.Generic.SortedSet<{elementType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}>(this.{propertyName}.Select(x => x?.{DeepCloneMethodName}())) : null";
        }
        return $"this.{propertyName} != null ? new System.Collections.Generic.SortedSet<{elementType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}>(this.{propertyName}) : null";
    }

    private static string GenerateObservableCollectionClone(
        string propertyName,
        ITypeSymbol elementType,
        bool isCloneable
    )
    {
        if (isCloneable)
        {
            return $"this.{propertyName} != null ? new System.Collections.ObjectModel.ObservableCollection<{elementType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}>(this.{propertyName}.Select(x => x?.{DeepCloneMethodName}())) : null";
        }
        return $"this.{propertyName} != null ? new System.Collections.ObjectModel.ObservableCollection<{elementType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}>(this.{propertyName}) : null";
    }

    private static string GenerateReadOnlyCollectionClone(
        string propertyName,
        ITypeSymbol elementType,
        bool isCloneable
    )
    {
        if (isCloneable)
        {
            return $"this.{propertyName} != null ? new System.Collections.ObjectModel.ReadOnlyCollection<{elementType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}>(this.{propertyName}.Select(x => x?.{DeepCloneMethodName}()).ToList()) : null";
        }
        // ReadOnlyCollection wraps a mutable list, so we need to clone it even for value types
        return $"this.{propertyName} != null ? new System.Collections.ObjectModel.ReadOnlyCollection<{elementType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}>(this.{propertyName}.ToList()) : null";
    }

    private static string GenerateImmutableListClone(
        string propertyName,
        ITypeSymbol elementType,
        bool isCloneable
    )
    {
        if (isCloneable)
        {
            return $"this.{propertyName}?.Select(x => x?.{DeepCloneMethodName}()).ToImmutableList()";
        }
        // For value types and strings in immutable collections, we can safely reuse the same collection
        // since both the collection and elements are immutable
        if (elementType.IsValueType || elementType.SpecialType == SpecialType.System_String)
        {
            return $"this.{propertyName}";
        }
        return $"this.{propertyName}?.ToImmutableList()";
    }

    private static string GenerateImmutableArrayClone(
        string propertyName,
        ITypeSymbol elementType,
        bool isCloneable
    )
    {
        if (isCloneable)
        {
            return $"this.{propertyName}.IsDefault ? default : this.{propertyName}.Select(x => x?.{DeepCloneMethodName}()).ToImmutableArray()";
        }
        // For value types and strings in immutable arrays, we can safely reuse the same array
        // since both the array and elements are immutable
        if (elementType.IsValueType || elementType.SpecialType == SpecialType.System_String)
        {
            return $"this.{propertyName}";
        }
        return $"this.{propertyName}.IsDefault ? default : this.{propertyName}.ToImmutableArray()";
    }

    private static string GenerateImmutableHashSetClone(
        string propertyName,
        ITypeSymbol elementType,
        bool isCloneable
    )
    {
        if (isCloneable)
        {
            return $"this.{propertyName}?.Select(x => x?.{DeepCloneMethodName}()).ToImmutableHashSet()";
        }
        // For value types and strings in immutable collections, we can safely reuse the same collection
        // since both the collection and elements are immutable
        if (elementType.IsValueType || elementType.SpecialType == SpecialType.System_String)
        {
            return $"this.{propertyName}";
        }
        return $"this.{propertyName}?.ToImmutableHashSet()";
    }

    private static string GenerateImmutableQueueClone(
        string propertyName,
        ITypeSymbol elementType,
        bool isCloneable
    )
    {
        if (isCloneable)
        {
            return $"this.{propertyName} == null ? System.Collections.Immutable.ImmutableQueue<{elementType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}>.Empty : System.Collections.Immutable.ImmutableQueue.CreateRange(this.{propertyName}.Select(x => x?.{DeepCloneMethodName}()))";
        }
        // For value types and strings in immutable collections, we can safely reuse the same collection
        if (elementType.IsValueType || elementType.SpecialType == SpecialType.System_String)
        {
            return $"this.{propertyName}";
        }
        return $"this.{propertyName} == null ? System.Collections.Immutable.ImmutableQueue<{elementType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}>.Empty : System.Collections.Immutable.ImmutableQueue.CreateRange(this.{propertyName})";
    }

    private static string GenerateImmutableStackClone(
        string propertyName,
        ITypeSymbol elementType,
        bool isCloneable
    )
    {
        if (isCloneable)
        {
            return $"this.{propertyName} == null ? System.Collections.Immutable.ImmutableStack<{elementType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}>.Empty : System.Collections.Immutable.ImmutableStack.CreateRange(this.{propertyName}.Select(x => x?.{DeepCloneMethodName}()))";
        }
        // For value types and strings in immutable collections, we can safely reuse the same collection
        if (elementType.IsValueType || elementType.SpecialType == SpecialType.System_String)
        {
            return $"this.{propertyName}";
        }
        return $"this.{propertyName} == null ? System.Collections.Immutable.ImmutableStack<{elementType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}>.Empty : System.Collections.Immutable.ImmutableStack.CreateRange(this.{propertyName})";
    }

    private static string GenerateDefaultListClone(
        string propertyName,
        ITypeSymbol elementType,
        bool isCloneable
    )
    {
        if (isCloneable)
        {
            return $"this.{propertyName}?.Select(x => x?.{DeepCloneMethodName}()).ToList()";
        }

        // Check if element type is a collection that needs deep cloning
        if (elementType is INamedTypeSymbol namedElementType && IsCollectionType(namedElementType))
        {
            // For nested collections (e.g., List<List<int>>), we need to deep clone each element
            var elementCloneExpr = GenerateNestedCollectionCloneExpression(namedElementType, "x");
            return $"this.{propertyName}?.Select(x => {elementCloneExpr}).ToList()";
        }

        // Check if element type is a reference type with properties that need deep cloning
        if (
            elementType is INamedTypeSymbol namedElementTypeRef
            && !namedElementTypeRef.IsValueType
            && namedElementTypeRef.SpecialType != SpecialType.System_String
        )
        {
            var properties = GetCloneableProperties(namedElementTypeRef);
            if (properties.Count > 0)
            {
                // Generate object initializer for deep cloning each element
                var propertyAssignments = new List<string>();
                foreach (var prop in properties)
                {
                    var propCloneExpr = GeneratePropertyCloneExpressionForObject(prop, "x", 0);
                    propertyAssignments.Add($"{prop.Name} = {propCloneExpr}!");
                }
                var assignmentsStr = string.Join(", ", propertyAssignments);
                return $"this.{propertyName}?.Select(x => x != null ? new {namedElementTypeRef.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)} {{ {assignmentsStr} }} : null).ToList()";
            }
        }

        // For value types including primitives and strings, the List constructor is already optimized
        // and will use Array.Copy internally which is very efficient
        return $"this.{propertyName} != null ? new System.Collections.Generic.List<{elementType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}>(this.{propertyName}) : null";
    }

    private static string GenerateNestedCollectionCloneExpression(
        INamedTypeSymbol collectionType,
        string varName
    )
    {
        // If collection has no type arguments, return the variable as-is
        // This shouldn't normally happen for generic collections but handles edge cases
        if (collectionType.TypeArguments.Length == 0)
            return varName;

        var elementType = collectionType.TypeArguments[0];

        // Check if element is cloneable
        bool isCloneable = IsCloneableType(elementType);

        if (isCloneable)
        {
            return $"{varName}?.Select(item => item?.{DeepCloneMethodName}()).ToList()";
        }

        // Check if element is itself a collection
        if (
            elementType is INamedTypeSymbol nestedCollectionType
            && IsCollectionType(nestedCollectionType)
        )
        {
            var nestedCloneExpr = GenerateNestedCollectionCloneExpression(
                nestedCollectionType,
                "item"
            );
            return $"{varName}?.Select(item => {nestedCloneExpr}).ToList()";
        }

        // Check if element is a reference type with properties that need deep cloning
        if (
            elementType is INamedTypeSymbol namedElementType
            && !namedElementType.IsValueType
            && namedElementType.SpecialType != SpecialType.System_String
        )
        {
            var properties = GetCloneableProperties(namedElementType);
            if (properties.Count > 0)
            {
                // Generate object initializer for deep cloning each element
                var propertyAssignments = new List<string>();
                foreach (var prop in properties)
                {
                    var propCloneExpr = GeneratePropertyCloneExpressionForObject(prop, "item", 0);
                    propertyAssignments.Add($"{prop.Name} = {propCloneExpr}!");
                }
                var assignmentsStr = string.Join(", ", propertyAssignments);
                return $"{varName}?.Select(item => item != null ? new {namedElementType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)} {{ {assignmentsStr} }} : null).ToList()";
            }
        }

        // For value types, strings, and reference types, create a new list with copied elements
        return $"{varName} != null ? new System.Collections.Generic.List<{elementType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}>({varName}) : null";
    }

    private static List<IPropertySymbol> GetCloneableProperties(INamedTypeSymbol classSymbol)
    {
        var properties = new List<IPropertySymbol>();

        var currentType = classSymbol;
        while (currentType is not null)
        {
            foreach (var member in currentType.GetMembers())
            {
                if (
                    member is IPropertySymbol property
                    && !property.IsStatic
                    && property.GetMethod is not null
                    && !properties.Any(p => p.Name == property.Name)
                )
                {
                    bool hasPublicSetter =
                        property.SetMethod is not null
                        && property.SetMethod.DeclaredAccessibility == Accessibility.Public;

                    bool hasPublicInit =
                        property.SetMethod is not null
                        && property.SetMethod.IsInitOnly
                        && property.SetMethod.DeclaredAccessibility == Accessibility.Public;

                    if (hasPublicSetter || hasPublicInit)
                    {
                        properties.Add(property);
                    }
                }
            }
            currentType = currentType.BaseType;
        }

        return properties;
    }

    private record ClassInfo(
        string ClassName,
        string? Namespace,
        INamedTypeSymbol ClassSymbol,
        bool ShouldGenerateDeepClone,
        string TypeKeyword
    );
}
