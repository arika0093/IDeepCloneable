using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace IDeepCloneable.Generator;

/// <summary>
/// An immutable array that implements value-based equality.
/// This is used in incremental generators to ensure proper caching behavior.
/// </summary>
internal readonly struct EquatableArray<T> : IEquatable<EquatableArray<T>>, IEnumerable<T>
    where T : IEquatable<T>
{
    private readonly T[] _array;

    public EquatableArray(T[] array)
    {
        _array = array ?? Array.Empty<T>();
    }

    public EquatableArray(IEnumerable<T> items)
    {
        _array = items?.ToArray() ?? Array.Empty<T>();
    }

    public int Count => _array.Length;

    public T this[int index] => _array[index];

    public bool Equals(EquatableArray<T> other)
    {
        if (_array.Length != other._array.Length)
            return false;

        for (int i = 0; i < _array.Length; i++)
        {
            var item1 = _array[i];
            var item2 = other._array[i];

            if (item1 is null && item2 is null)
                continue;

            if (item1 is null || item2 is null)
                return false;

            if (!item1.Equals(item2))
                return false;
        }

        return true;
    }

    public override bool Equals(object? obj)
    {
        return obj is EquatableArray<T> other && Equals(other);
    }

    public override int GetHashCode()
    {
        unchecked
        {
            int hash = 17;
            foreach (var item in _array)
            {
                hash = hash * 31 + (item?.GetHashCode() ?? 0);
            }
            return hash;
        }
    }

    public IEnumerator<T> GetEnumerator()
    {
        return ((IEnumerable<T>)_array).GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return _array.GetEnumerator();
    }

    public static bool operator ==(EquatableArray<T> left, EquatableArray<T> right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(EquatableArray<T> left, EquatableArray<T> right)
    {
        return !left.Equals(right);
    }
}

[Generator]
public class CloneableGenerator : IIncrementalGenerator
{
    private const string DeepCloneMethodName = "DeepClone";
    private const string DeepCloneableAttributeMetadataName = "DeepCloneableAttribute";
    private const string DeepCloneableAttributeFullName = "global::DeepCloneableAttribute";

    // Indentation constants for generated code
    // These represent the final indentation after raw string literal baseline removal (12 spaces)
    private const string PropertyIndent = "                "; // 16 spaces (4 levels: namespace/class/method/initializer)
    private const string StatementIndent = "            "; // 12 spaces (3 levels: namespace/class/method)

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var classDeclarations = context
            .SyntaxProvider.ForAttributeWithMetadataName(
                DeepCloneableAttributeMetadataName,
                predicate: static (node, _) => true,
                transform: static (ctx, _) => GetClassInfo(ctx)
            )
            .Where(static m => m is not null)
            .Select(static (m, _) => m!);

        context.RegisterSourceOutput(
            classDeclarations,
            static (spc, source) => Execute(source!, spc)
        );
    }

    private static ClassInfo? GetClassInfo(GeneratorAttributeSyntaxContext context)
    {
        if (context.TargetSymbol is not INamedTypeSymbol classSymbol)
            return null;

        // Ensure the declaration is partial
        bool isPartial =
            context.TargetNode is TypeDeclarationSyntax typeDecl
            && typeDecl.Modifiers.Any(SyntaxKind.PartialKeyword);

        if (!isPartial)
            return null;

        bool hasDeepClone = HasMethodImplementation(classSymbol, DeepCloneMethodName);

        string typeKeyword;
        if (classSymbol.IsRecord)
        {
            typeKeyword = classSymbol.IsValueType ? "record struct" : "record";
        }
        else
        {
            typeKeyword = classSymbol.IsValueType ? "struct" : "class";
        }

        var baseCloneableType = GetBaseCloneableType(classSymbol);
        
        // Get full name
        var fullName = classSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)
            .Replace("global::", "");

        // Analyze contained types
        var containedTypes = GetContainedTypes(classSymbol);
        
        // Check if all children are value types or immutable
        var allChildrenValueOrImmutable = AreAllChildrenValueOrImmutable(classSymbol);
        
        // Check if type has collection initializer support
        var hasCollectionInitializer = HasCollectionInitializer(classSymbol);
        
        // Determine if we should generate CloneInternal
        var shouldGenerateCloneInternal = !IsObviouslyImmutableType(classSymbol);
        
        // Determine if CloneInternal should be internal or private
        var isCloneInternalInternal = HasDeepCloneableAttribute(classSymbol) || baseCloneableType != null;

        return new ClassInfo(
            classSymbol.Name,
            GetNamespace(classSymbol),
            fullName,
            classSymbol,
            containedTypes,
            classSymbol.NullableAnnotation == NullableAnnotation.Annotated,
            classSymbol.IsRecord,
            classSymbol.IsValueType,
            allChildrenValueOrImmutable,
            hasCollectionInitializer,
            !hasDeepClone,
            classSymbol.IsAbstract,
            shouldGenerateCloneInternal,
            isCloneInternalInternal,
            typeKeyword,
            GetContainingTypes(classSymbol),
            baseCloneableType
        );
    }

    private static INamedTypeSymbol? GetBaseCloneableType(INamedTypeSymbol classSymbol)
    {
        if (
            classSymbol.BaseType is null
            || classSymbol.BaseType.SpecialType.Equals(SpecialType.System_Object)
        )
        {
            return null;
        }

        return HasDeepCloneableAttribute(classSymbol.BaseType) ? classSymbol.BaseType : null;
    }

    private static bool IsCloneableType(ITypeSymbol typeSymbol)
    {
        if (typeSymbol is not INamedTypeSymbol namedType)
            return false;

        if (HasDeepCloneableAttribute(namedType))
        {
            return true;
        }

        return namedType.AllInterfaces.Any(IsDeepCloneableInterface);
    }

    private static bool IsDeepCloneableInterface(INamedTypeSymbol interfaceSymbol)
    {
        return interfaceSymbol
            .OriginalDefinition.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)
            .StartsWith("global::IDeepCloneable<");
    }

    private static bool HasDeepCloneableAttribute(INamedTypeSymbol typeSymbol)
    {
        foreach (var attribute in typeSymbol.GetAttributes())
        {
            var attributeClass = attribute.AttributeClass;
            if (attributeClass is null)
            {
                continue;
            }

            var fullName = attributeClass.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

            if (fullName == DeepCloneableAttributeFullName)
            {
                return true;
            }

            if (
                attributeClass.Name == "DeepCloneableAttribute"
                && attributeClass.ContainingNamespace.IsGlobalNamespace
            )
            {
                return true;
            }
        }

        return false;
    }

    private static bool HasMethodImplementation(INamedTypeSymbol classSymbol, string methodName)
    {
        return classSymbol
            .GetMembers(methodName)
            .OfType<IMethodSymbol>()
            .Any(m => m.DeclaringSyntaxReferences.Any());
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

    private static EquatableArray<string> GetContainingTypes(ISymbol symbol)
    {
        var containingTypes = new List<string>();
        var containingType = symbol.ContainingType;
        while (containingType != null)
        {
            containingTypes.Insert(0, containingType.Name);
            containingType = containingType.ContainingType;
        }
        return new EquatableArray<string>(containingTypes);
    }

    private static EquatableArray<string> GetContainedTypes(INamedTypeSymbol typeSymbol)
    {
        var containedTypes = new HashSet<string>();
        
        // Get all properties
        var properties = GetCloneableProperties(typeSymbol);
        
        foreach (var property in properties)
        {
            CollectContainedTypes(property.Type, containedTypes);
        }
        
        return new EquatableArray<string>(containedTypes.ToArray());
    }
    
    private static void CollectContainedTypes(ITypeSymbol typeSymbol, HashSet<string> containedTypes)
    {
        // Add the full name of this type
        var fullName = typeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)
            .Replace("global::", "");
        containedTypes.Add(fullName);
        
        // If it's a generic type, collect type arguments
        if (typeSymbol is INamedTypeSymbol namedType && namedType.TypeArguments.Length > 0)
        {
            foreach (var typeArg in namedType.TypeArguments)
            {
                CollectContainedTypes(typeArg, containedTypes);
            }
        }
        
        // If it's an array, collect element type
        if (typeSymbol is IArrayTypeSymbol arrayType)
        {
            CollectContainedTypes(arrayType.ElementType, containedTypes);
        }
    }
    
    private static bool AreAllChildrenValueOrImmutable(INamedTypeSymbol typeSymbol)
    {
        var properties = GetCloneableProperties(typeSymbol);
        
        foreach (var property in properties)
        {
            if (!IsValueOrImmutableType(property.Type))
            {
                return false;
            }
        }
        
        return true;
    }
    
    private static bool IsValueOrImmutableType(ITypeSymbol typeSymbol)
    {
        // Value types are fine
        if (typeSymbol.IsValueType)
        {
            // But check if the value type contains reference types
            if (typeSymbol is INamedTypeSymbol namedValueType)
            {
                var properties = GetCloneableProperties(namedValueType);
                foreach (var prop in properties)
                {
                    if (!IsValueOrImmutableType(prop.Type))
                    {
                        return false;
                    }
                }
            }
            return true;
        }
        
        // String is immutable
        if (typeSymbol.SpecialType == SpecialType.System_String)
        {
            return true;
        }
        
        // Other immutable types
        var fullName = typeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        if (fullName is "global::System.DateTime" 
            or "global::System.DateTimeOffset" 
            or "global::System.TimeSpan"
            or "global::System.Guid")
        {
            return true;
        }
        
        return false;
    }
    
    private static bool HasCollectionInitializer(ITypeSymbol typeSymbol)
    {
        // Arrays support collection initializers
        if (typeSymbol is IArrayTypeSymbol)
        {
            return true;
        }
        
        // Check if it implements IEnumerable and has Add method
        if (typeSymbol is INamedTypeSymbol namedType)
        {
            var hasEnumerable = namedType.AllInterfaces.Any(i =>
                i.OriginalDefinition.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)
                == "global::System.Collections.Generic.IEnumerable<T>");
                
            if (!hasEnumerable)
            {
                return false;
            }
            
            // Check for Add method
            var hasAdd = namedType.GetMembers("Add").Any(m => m is IMethodSymbol);
            
            return hasAdd;
        }
        
        return false;
    }
    
    private static bool IsObviouslyImmutableType(ITypeSymbol typeSymbol)
    {
        // Primitives
        if (typeSymbol.SpecialType != SpecialType.None)
        {
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
                case SpecialType.System_Single:
                case SpecialType.System_Double:
                case SpecialType.System_Decimal:
                case SpecialType.System_IntPtr:
                case SpecialType.System_UIntPtr:
                case SpecialType.System_String:
                    return true;
            }
        }
        
        // Other common immutable types
        var fullName = typeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        if (fullName is "global::System.DateTime" 
            or "global::System.DateTimeOffset" 
            or "global::System.TimeSpan"
            or "global::System.Guid")
        {
            return true;
        }
        
        return false;
    }

    private static void Execute(ClassInfo classInfo, SourceProductionContext context)
    {
        // Generate the main class with DeepClone method
        var source = GenerateCloneMethod(classInfo);
        var hintNameParts = new List<string>();
        if (!string.IsNullOrEmpty(classInfo.Namespace))
        {
            hintNameParts.Add(classInfo.Namespace!);
        }

        hintNameParts.AddRange(classInfo.ContainingTypes);
        hintNameParts.Add(classInfo.ClassName);

        var hintName = string.Join(".", hintNameParts) + ".g.cs";
        context.AddSource(hintName, SourceText.From(source, Encoding.UTF8));
        
        // Note: CloneInternal extension generation commented out for now
        // This would require updating all helper methods to support a customizable object name
        // For now, we're using the optimizations directly in the DeepClone method
    }

    private static string GenerateCloneMethod(ClassInfo classInfo)
    {
        var deepCloneMethod = classInfo.ShouldGenerateDeepClone
            ? GenerateDeepCloneMethod(classInfo)
            : string.Empty;

        var fullClassName = string.Join(
            ".",
            classInfo.ContainingTypes.Concat(new[] { classInfo.ClassName })
        );

        // Build the nested class structure
        var classDeclaration = BuildNestedClassDeclaration(classInfo, deepCloneMethod);

        if (classInfo.Namespace is not null)
        {
            return $$"""
                {{OutputFileHeaderParts}}
                namespace {{classInfo.Namespace}}
                {
                {{classDeclaration}}
                }
                """;
        }
        else
        {
            return $$"""
                {{OutputFileHeaderParts}}
                {{classDeclaration}}
                """;
        }
    }

    private static string BuildNestedClassDeclaration(ClassInfo classInfo, string deepCloneMethod)
    {
        var indent = "    ";
        var currentIndent = indent;

        // Build interface list
        var interfaces = $"IDeepCloneable<{classInfo.ClassName}>";
        if (classInfo.BaseCloneableType is not null)
        {
            var baseTypeName = classInfo.BaseCloneableType.Name;
            interfaces += $", IDeepCloneable<{baseTypeName}>";
        }

        if (classInfo.ContainingTypes.Count == 0)
        {
            // No nesting
            return $$"""
                {{indent}}partial {{classInfo.TypeKeyword}} {{classInfo.ClassName}} : {{interfaces}}
                {{indent}}{
                {{deepCloneMethod}}
                {{indent}}}
                """;
        }

        var sb = new StringBuilder();

        // Open containing types
        foreach (var containingType in classInfo.ContainingTypes)
        {
            sb.AppendLine($"{currentIndent}partial class {containingType}");
            sb.AppendLine($"{currentIndent}{{");
            currentIndent += indent;
        }

        // Add the actual class with DeepClone implementation
        sb.AppendLine(
            $"{currentIndent}partial {classInfo.TypeKeyword} {classInfo.ClassName} : {interfaces}"
        );
        sb.AppendLine($"{currentIndent}{{");

        // Add the deep clone method with proper indentation
        var methodLines = deepCloneMethod.Split('\n');
        foreach (var line in methodLines)
        {
            if (!string.IsNullOrWhiteSpace(line))
            {
                sb.AppendLine($"{currentIndent}{line.TrimStart()}");
            }
        }

        sb.AppendLine($"{currentIndent}}}");

        // Close containing types
        for (int i = classInfo.ContainingTypes.Count - 1; i >= 0; i--)
        {
            currentIndent = currentIndent.Substring(0, currentIndent.Length - indent.Length);
            sb.AppendLine($"{currentIndent}}}");
        }

        return sb.ToString().TrimEnd();
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
        // For abstract classes, generate an abstract method declaration
        if (classInfo.IsAbstract)
        {
            var baseTypeName = classInfo.ClassName;
            return $$"""
                        /// <summary>
                        /// Creates a deep clone of this instance.
                        /// </summary>
                        public abstract {{baseTypeName}} {{DeepCloneMethodName}}();
                """;
        }

        var sb = new StringBuilder();

        // Determine if we need override keyword
        var methodModifier = classInfo.BaseCloneableType != null ? "public override" : "public";

        var properties = GetCloneableProperties(classInfo.ClassSymbol);

        bool hasInitOnlyProperties = properties.Any(p => p.SetMethod?.IsInitOnly == true);
        bool needsStatements = properties.Any(p =>
            p.Type is IArrayTypeSymbol arrayType && arrayType.Rank > 1
        );

        // If all children are value types or immutable, we can use simpler cloning
        if (classInfo.AllChildrenAreValueOrImmutable && classInfo.IsRecord)
        {
            // For records with only value/immutable types, use simple with syntax
            sb.AppendLine(
                $$"""
                        /// <inheritdoc />
                        {{methodModifier}} {{classInfo.ClassName}} {{DeepCloneMethodName}}()
                        {
                            return this with { };
                        }
                """
            );
            return sb.ToString().TrimEnd();
        }

        // Generate the main DeepClone method that returns the derived type
        if (needsStatements || hasInitOnlyProperties)
        {
            if (classInfo.IsRecord && hasInitOnlyProperties)
            {
                var assignments = new List<string>();
                foreach (var property in properties)
                {
                    var expression = GenerateDeepCloneExpression(property);
                    assignments.Add($"{PropertyIndent}{property.Name} = {expression}");
                }

                var withAssignments = string.Join(",\n", assignments);

                sb.AppendLine(
                    $$"""
                            /// <inheritdoc />
                            {{methodModifier}} {{classInfo.ClassName}} {{DeepCloneMethodName}}()
                            {
                                return this with
                                {
                    {{withAssignments}}
                                };
                            }
                    """
                );
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

                sb.AppendLine(
                    $$"""
                            /// <inheritdoc />
                            {{methodModifier}} {{classInfo.ClassName}} {{DeepCloneMethodName}}()
                            {
                    {{methodBody}}
                            }
                    """
                );
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

            sb.AppendLine(
                $$"""
                        /// <inheritdoc />
                        {{methodModifier}} {{classInfo.ClassName}} {{DeepCloneMethodName}}()
                        {
                            return new {{classInfo.ClassName}}
                            {
                {{propertyAssignments}}
                            };
                        }
                """
            );
        }

        return sb.ToString().TrimEnd();
    }

    // NOTE: This method is commented out for now but kept for future implementation
    // It would generate CloneInternal extension methods as described in the performance requirements
    // Currently, we're using inline optimizations instead
    /*
    private static string GenerateCloneInternalExtension(ClassInfo classInfo)
    {
        var accessibility = classInfo.IsCloneInternalInternal ? "internal" : "private";
        var fullTypeName = classInfo.ClassSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        var safeName = classInfo.FullName.Replace(".", "_").Replace("<", "_").Replace(">", "_").Replace(",", "_").Replace(" ", "");
        
        var properties = GetCloneableProperties(classInfo.ClassSymbol);
        
        var sb = new StringBuilder();
        
        // Header
        sb.AppendLine(OutputFileHeaderParts);
        sb.AppendLine("namespace IDeepCloneable.Extensions");
        sb.AppendLine("{");
        sb.AppendLine($"    {accessibility} static class DeepCloneExtensions_{safeName}");
        sb.AppendLine("    {");
        sb.AppendLine($"        {accessibility} static {fullTypeName} {safeName}CloneInternal(this {fullTypeName} value)");
        sb.AppendLine("        {");
        
        // Handle nullable reference types
        if (!classInfo.IsValueType && classInfo.IsNullable)
        {
            sb.AppendLine("            if (value is null)");
            sb.AppendLine("            {");
            sb.AppendLine("                return null;");
            sb.AppendLine("            }");
        }
        
        // If all children are value types or immutable
        if (classInfo.AllChildrenAreValueOrImmutable)
        {
            if (classInfo.IsRecord || classInfo.IsValueType)
            {
                sb.AppendLine("            return value with { };");
            }
            else if (classInfo.HasCollectionInitializer)
            {
                sb.AppendLine("            return [.. value];");
            }
            else
            {
                // Simple value copy
                sb.AppendLine("            return value;");
            }
        }
        else
        {
            // Complex cloning logic
            if (classInfo.IsRecord)
            {
                var assignments = new List<string>();
                foreach (var property in properties)
                {
                    if (!IsValueOrImmutableType(property.Type))
                    {
                        var expression = GenerateDeepCloneExpression(property, "value");
                        assignments.Add($"                {property.Name} = {expression}");
                    }
                }
                
                if (assignments.Count > 0)
                {
                    var withAssignments = string.Join(",\n", assignments);
                    sb.AppendLine("            return value with");
                    sb.AppendLine("            {");
                    sb.AppendLine(withAssignments);
                    sb.AppendLine("            };");
                }
                else
                {
                    sb.AppendLine("            return value with { };");
                }
            }
            else if (classInfo.HasCollectionInitializer && !classInfo.IsValueType)
            {
                // For collections, iterate and clone elements
                sb.AppendLine($"            var clone = new {fullTypeName}();");
                sb.AppendLine("            foreach (var item in value)");
                sb.AppendLine("            {");
                sb.AppendLine("                clone.Add(item);");
                sb.AppendLine("            }");
                sb.AppendLine("            return clone;");
            }
            else
            {
                // Regular class cloning
                sb.AppendLine($"            var clone = new {fullTypeName}();");
                
                foreach (var property in properties)
                {
                    var expression = GenerateDeepCloneExpression(property, "value");
                    sb.AppendLine($"            clone.{property.Name} = {expression};");
                }
                
                sb.AppendLine("            return clone;");
            }
        }
        
        sb.AppendLine("        }");
        sb.AppendLine("    }");
        sb.AppendLine("}");
        
        return sb.ToString();
    }
    */

    private static string GenerateDeepCloneExpression(IPropertySymbol property)
    {
        var typeSymbol = property.Type;

        if (typeSymbol is IArrayTypeSymbol arrayType)
        {
            return GenerateArrayDeepClone(property, arrayType);
        }

        if (typeSymbol is INamedTypeSymbol namedType)
        {
            if (IsCloneableType(namedType))
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

        if (IsCloneableType(elementType))
        {
            return $"this.{propertyName}?.Select(x => x?.{DeepCloneMethodName}()).ToArray()";
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

        bool valueIsCloneable = IsCloneableType(valueType);

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

        bool isCloneable = IsCloneableType(elementType);

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
        string FullName,
        INamedTypeSymbol ClassSymbol,
        EquatableArray<string> ContainedTypeFullNames,
        bool IsNullable,
        bool IsRecord,
        bool IsValueType,
        bool AllChildrenAreValueOrImmutable,
        bool HasCollectionInitializer,
        bool ShouldGenerateDeepClone,
        bool IsAbstract,
        bool ShouldGenerateCloneInternal,
        bool IsCloneInternalInternal,
        string TypeKeyword,
        EquatableArray<string> ContainingTypes,
        INamedTypeSymbol? BaseCloneableType
    );
}
