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

    // Thread-local context for generation
    [ThreadStatic]
    private static CloneInternalNameGenerator? s_currentNameGenerator;
    
    /// <summary>
    /// Gets the clone expression for a given type. Uses CloneInternal if available, otherwise returns null indicating direct assignment.
    /// </summary>
    private static string? GetCloneExpression(ITypeSymbol typeSymbol, string valueExpression, bool isNullable)
    {
        var fullTypeName = typeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat).Replace("global::", "");
        var nameGenerator = s_currentNameGenerator;
        
        if (nameGenerator != null && nameGenerator.HasCloneInternal(fullTypeName))
        {
            var cloneMethodName = nameGenerator.GetCloneInternalName(fullTypeName);
            if (isNullable)
            {
                return $"{valueExpression} != null ? {cloneMethodName}({valueExpression}) : null";
            }
            else
            {
                return $"{cloneMethodName}({valueExpression})";
            }
        }
        
        // No CloneInternal available, return null to indicate direct assignment
        return null;
    }
    
    /// <summary>
    /// Generate clone statement for an item. Uses CloneInternal if available, otherwise assigns directly.
    /// </summary>
    private static string GetItemCloneStatement(ITypeSymbol elementType, string itemExpression)
    {
        // For reference types, we should always handle null unless explicitly marked as non-nullable
        var elementIsNullable = !elementType.IsValueType || elementType.NullableAnnotation == NullableAnnotation.Annotated;
        var cloneExpr = GetCloneExpression(elementType, itemExpression, elementIsNullable);
        return cloneExpr ?? itemExpression;
    }


    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // Collect all types with [DeepCloneable] attribute
        var classDeclarations = context
            .SyntaxProvider.ForAttributeWithMetadataName(
                DeepCloneableAttributeMetadataName,
                predicate: static (node, _) => true,
                transform: static (ctx, _) => ctx.TargetSymbol as INamedTypeSymbol
            )
            .Where(static m => m is not null)
            .Select(static (m, _) => m!);

        // Collect all types and discover dependencies
        var allTypes = classDeclarations.Collect();

        context.RegisterSourceOutput(
            allTypes,
            static (spc, types) => ExecuteAll(types, spc)
        );
    }

    private static void ExecuteAll(System.Collections.Immutable.ImmutableArray<INamedTypeSymbol> rootTypes, SourceProductionContext context)
    {
        var registry = new TypeRegistry();
        var discovery = new TypeDiscovery(registry);
        var nameGenerator = new CloneInternalNameGenerator();

        // Discover all reachable types from root types
        foreach (var rootType in rootTypes)
        {
            discovery.DiscoverTypes(rootType, true);
        }

        // Register all discovered types with the name generator
        foreach (var typeInfo in registry.GetAllTypes())
        {
            nameGenerator.RegisterType(typeInfo.FullName);
        }

        // Set the thread-local context
        s_currentNameGenerator = nameGenerator;

        try
        {
            // Build single file for all CloneInternal extensions
            var extensionsSb = new StringBuilder();
            extensionsSb.AppendLine(OutputFileHeaderParts);
            extensionsSb.AppendLine("using System.Runtime.InteropServices;");
            extensionsSb.AppendLine();
            extensionsSb.AppendLine("namespace IDeepCloneable.Extensions");
            extensionsSb.AppendLine("{");
            extensionsSb.AppendLine("    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]");
            extensionsSb.AppendLine("    internal static partial class DeepCloneExtensions");
            extensionsSb.AppendLine("    {");
            
            // Generate code for each discovered type
            foreach (var typeInfo in registry.GetAllTypes())
            {
                var classInfo = GetClassInfoFromSymbol(typeInfo.Symbol, typeInfo.HasDeepCloneableAttribute);
                if (classInfo != null)
                {
                    // Only generate main class file for types with [DeepCloneable] attribute
                    if (typeInfo.HasDeepCloneableAttribute)
                    {
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
                    }
                    
                    // Generate CloneInternal extension method for all types (if not abstract)
                    if (!classInfo.IsAbstract)
                    {
                        var extensionMethod = GenerateCloneInternalExtensionMethod(classInfo, typeInfo.Symbol, nameGenerator, typeInfo.HasDeepCloneableAttribute);
                        extensionsSb.AppendLine(extensionMethod);
                    }
                }
            }
            
            // Close the class and namespace
            extensionsSb.AppendLine("    }");
            extensionsSb.AppendLine("}");
            
            // Add single file with all extensions
            context.AddSource("DeepCloneExtensions.g.cs", SourceText.From(extensionsSb.ToString(), Encoding.UTF8));
        }
        finally
        {
            // Clear the thread-local context
            s_currentNameGenerator = null;
        }
    }

    private static ClassInfo? GetClassInfoFromSymbol(INamedTypeSymbol classSymbol, bool hasDeepCloneableAttribute)
    {
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
        var baseCloneableTypeFullName = baseCloneableType?.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat).Replace("global::", "");
        
        // Get full name for the type
        var fullName = classSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat).Replace("global::", "");
        
        // Analyze all properties to collect contained types
        var properties = GetCloneableProperties(classSymbol);
        var containedTypes = new HashSet<string>();
        foreach (var prop in properties)
        {
            CollectContainedTypes(prop.Type, containedTypes);
        }
        
        // Determine if all children are value types or immutable
        bool allChildrenValueOrImmutable = properties.All(p => IsValueOrImmutableType(p.Type));
        
        // Check if type has collection initializer
        bool hasCollectionInitializer = classSymbol.AllInterfaces.Any(i =>
            i.OriginalDefinition.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)
            == "global::System.Collections.Generic.IEnumerable<T>")
            && classSymbol.GetMembers("Add").Any(m => m is IMethodSymbol);

        return new ClassInfo(
            classSymbol.Name,
            GetNamespace(classSymbol),
            fullName,
            new EquatableArray<string>(containedTypes.ToArray()),
            classSymbol.NullableAnnotation == NullableAnnotation.Annotated,
            classSymbol.IsRecord,
            classSymbol.IsValueType,
            allChildrenValueOrImmutable,
            hasCollectionInitializer,
            hasDeepCloneableAttribute && !hasDeepClone, // Only generate if attribute present and no existing implementation
            classSymbol.IsAbstract,
            typeKeyword,
            GetContainingTypes(classSymbol),
            baseCloneableTypeFullName
        );
    }

    
    private static void CollectContainedTypes(ITypeSymbol typeSymbol, HashSet<string> containedTypes)
    {
        var fullName = typeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat).Replace("global::", "");
        containedTypes.Add(fullName);
        
        // Collect generic type arguments
        if (typeSymbol is INamedTypeSymbol namedType && namedType.TypeArguments.Length > 0)
        {
            foreach (var typeArg in namedType.TypeArguments)
            {
                CollectContainedTypes(typeArg, containedTypes);
            }
        }
        
        // Collect array element types
        if (typeSymbol is IArrayTypeSymbol arrayType)
        {
            CollectContainedTypes(arrayType.ElementType, containedTypes);
        }
    }
    
    private static bool IsValueOrImmutableType(ITypeSymbol typeSymbol)
    {
        // Primitives and value types
        if (typeSymbol.IsValueType)
        {
            // Check if value type contains reference types
            if (typeSymbol is INamedTypeSymbol namedValueType)
            {
                var props = GetCloneableProperties(namedValueType);
                return props.All(p => IsValueOrImmutableType(p.Type));
            }
            return true;
        }
        
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
        if (classInfo.BaseCloneableTypeFullName is not null)
        {
            // Extract just the class name from the full name
            var baseTypeName = classInfo.BaseCloneableTypeFullName.Split('.').Last().Split('<').First();
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

        // Determine if we need override keyword
        var methodModifier = classInfo.BaseCloneableTypeFullName != null ? "public override" : "public";
        
        // Generate safe name for the extension method
        var safeName = GenerateSafeName(classInfo.FullName);
        
        // Simply call the CloneInternal extension method
        return $$"""
                    /// <inheritdoc />
                    {{methodModifier}} {{classInfo.ClassName}} {{DeepCloneMethodName}}()
                    {
                        return IDeepCloneable.Extensions.DeepCloneExtensions.{{safeName}}CloneInternal(this);
                    }
            """;
    }
    
    private static string GenerateSafeName(string fullName)
    {
        return fullName
            .Replace(".", "_")
            .Replace("<", "_")
            .Replace(">", "_")
            .Replace(",", "_")
            .Replace(" ", "")
            .Replace(":", "_");
    }

    private static string GenerateCloneInternalExtensionMethod(ClassInfo classInfo, INamedTypeSymbol classSymbol, CloneInternalNameGenerator nameGenerator, bool hasDeepCloneableAttribute)
    {
        var fullTypeName = classSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        var safeName = GenerateSafeName(classInfo.FullName);
        
        var sb = new StringBuilder();
        
        // Add attributes
        sb.AppendLine("        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]");
        sb.AppendLine("        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]");
        
        // Internal if called from DeepClone, otherwise private
        var accessibility = hasDeepCloneableAttribute ? "internal" : "private";
        sb.AppendLine($"        {accessibility} static {fullTypeName} {safeName}CloneInternal(this {fullTypeName} value)");
        sb.AppendLine("        {");
        
        // Generate cloning logic based on type characteristics
        var properties = GetCloneableProperties(classSymbol);
        
        // If all children are value types or immutable and it's a record/struct
        if (classInfo.AllChildrenAreValueOrImmutable && (classInfo.IsRecord || classInfo.IsValueType))
        {
            sb.AppendLine("            return value with { };");
        }
        // If it has collection initializer and all children are value types
        else if (classInfo.HasCollectionInitializer && classInfo.AllChildrenAreValueOrImmutable)
        {
            sb.AppendLine("            return [.. value];");
        }
        // Record with reference type properties
        else if (classInfo.IsRecord)
        {
            var assignments = new List<string>();
            foreach (var property in properties)
            {
                if (!IsValueOrImmutableType(property.Type))
                {
                    var expression = GenerateCloneExpression(property, "value");
                    assignments.Add($"                {property.Name} = {expression}");
                }
            }
            
            if (assignments.Count > 0)
            {
                sb.AppendLine("            return value with");
                sb.AppendLine("            {");
                sb.AppendLine(string.Join(",\n", assignments));
                sb.AppendLine("            };");
            }
            else
            {
                sb.AppendLine("            return value with { };");
            }
        }
        // Regular class or struct with properties
        else
        {
            sb.AppendLine($"            var clone = new {fullTypeName}();");
            
            foreach (var property in properties)
            {
                GeneratePropertyCloneStatements(sb, property, "value", "clone", "            ");
            }
            
            sb.AppendLine("            return clone;");
        }
        
        sb.AppendLine("        }");
        sb.AppendLine();
        
        return sb.ToString();
    }
    
    private static void GeneratePropertyCloneStatements(StringBuilder sb, IPropertySymbol property, string sourceVar, string targetVar, string indent)
    {
        var typeSymbol = property.Type;
        var propertyName = property.Name;
        
        // Value types and immutable types - simple assignment
        if (IsValueOrImmutableType(typeSymbol))
        {
            sb.AppendLine($"{indent}{targetVar}.{propertyName} = {sourceVar}.{propertyName};");
            return;
        }
        
        // Arrays
        if (typeSymbol is IArrayTypeSymbol arrayType)
        {
            GenerateArrayCloneStatements(sb, property, arrayType, sourceVar, targetVar, indent);
            return;
        }
        
        // Named types
        if (typeSymbol is INamedTypeSymbol namedType)
        {
            // Cloneable types
            if (IsCloneableType(namedType))
            {
                var isNullable = property.NullableAnnotation == NullableAnnotation.Annotated;
                var cloneExpr = GetCloneExpression(namedType, $"{sourceVar}.{propertyName}", isNullable);
                if (cloneExpr != null)
                {
                    sb.AppendLine($"{indent}{targetVar}.{propertyName} = {cloneExpr};");
                }
                else
                {
                    // No CloneInternal available, assign directly
                    sb.AppendLine($"{indent}{targetVar}.{propertyName} = {sourceVar}.{propertyName};");
                }
                return;
            }
            
            // Dictionaries
            if (IsDictionaryType(namedType))
            {
                GenerateDictionaryCloneStatements(sb, property, namedType, sourceVar, targetVar, indent);
                return;
            }
            
            // Collections
            if (IsCollectionType(namedType))
            {
                GenerateCollectionCloneStatements(sb, property, namedType, sourceVar, targetVar, indent);
                return;
            }
            
            // Reference types with properties
            if (!namedType.IsValueType && namedType.SpecialType != SpecialType.System_String)
            {
                var props = GetCloneableProperties(namedType);
                if (props.Count > 0)
                {
                    // Check if this type has a CloneInternal method registered
                    var fullName = namedType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat).Replace("global::", "");
                    var hasCloneInternal = s_currentNameGenerator?.HasCloneInternal(fullName) ?? false;
                    
                    if (hasCloneInternal)
                    {
                        var cloneInternalName = s_currentNameGenerator!.GetCloneInternalName(fullName);
                        var isNullable = property.NullableAnnotation == NullableAnnotation.Annotated;
                        
                        if (isNullable)
                        {
                            sb.AppendLine($"{indent}{targetVar}.{propertyName} = {sourceVar}.{propertyName} != null ? {cloneInternalName}({sourceVar}.{propertyName}) : null;");
                        }
                        else
                        {
                            sb.AppendLine($"{indent}{targetVar}.{propertyName} = {cloneInternalName}({sourceVar}.{propertyName});");
                        }
                    }
                    else
                    {
                        // Generate inline object initializer for types without CloneInternal
                        var fullTypeName = namedType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                        var isNullable = property.NullableAnnotation == NullableAnnotation.Annotated;
                        
                        if (isNullable)
                        {
                            sb.AppendLine($"{indent}if ({sourceVar}.{propertyName} != null)");
                            sb.AppendLine($"{indent}{{");
                            sb.AppendLine($"{indent}    {targetVar}.{propertyName} = new {fullTypeName}();");
                            foreach (var prop in props)
                            {
                                var nestedExpr = GenerateCloneExpression(prop, $"{sourceVar}.{propertyName}");
                                sb.AppendLine($"{indent}    {targetVar}.{propertyName}.{prop.Name} = {nestedExpr};");
                            }
                            sb.AppendLine($"{indent}}}");
                            sb.AppendLine($"{indent}else");
                            sb.AppendLine($"{indent}{{");
                            sb.AppendLine($"{indent}    {targetVar}.{propertyName} = null;");
                            sb.AppendLine($"{indent}}}");
                        }
                        else
                        {
                            sb.AppendLine($"{indent}{targetVar}.{propertyName} = new {fullTypeName}();");
                            foreach (var prop in props)
                            {
                                var nestedExpr = GenerateCloneExpression(prop, $"{sourceVar}.{propertyName}");
                                sb.AppendLine($"{indent}{targetVar}.{propertyName}.{prop.Name} = {nestedExpr};");
                            }
                        }
                    }
                    return;
                }
            }
        }
        
        // Default: simple assignment
        sb.AppendLine($"{indent}{targetVar}.{propertyName} = {sourceVar}.{propertyName};");
    }
    
    private static void GenerateArrayCloneStatements(StringBuilder sb, IPropertySymbol property, IArrayTypeSymbol arrayType, string sourceVar, string targetVar, string indent)
    {
        var elementType = arrayType.ElementType;
        var propertyName = property.Name;
        var elementTypeName = elementType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        var isNullable = property.NullableAnnotation == NullableAnnotation.Annotated;
        
        // Multi-dimensional arrays - use Clone()
        if (arrayType.Rank > 1)
        {
            var rankCommas = new string(',', arrayType.Rank - 1);
            var arrayTypeName = $"{elementTypeName}[{rankCommas}]";
            if (isNullable)
            {
                sb.AppendLine($"{indent}{targetVar}.{propertyName} = {sourceVar}.{propertyName} != null ? ({arrayTypeName}){sourceVar}.{propertyName}.Clone() : null;");
            }
            else
            {
                sb.AppendLine($"{indent}{targetVar}.{propertyName} = ({arrayTypeName}){sourceVar}.{propertyName}.Clone();");
            }
            return;
        }
        
        // Cloneable elements - use foreach
        if (IsCloneableType(elementType))
        {
            // For reference types, treat as nullable by default
            var elementIsNullable = !elementType.IsValueType || elementType.NullableAnnotation == NullableAnnotation.Annotated;
            var itemExpr = $"{sourceVar}.{propertyName}[i]";
            var cloneExpr = GetCloneExpression(elementType, itemExpr, elementIsNullable);
            
            if (isNullable)
            {
                sb.AppendLine($"{indent}if ({sourceVar}.{propertyName} != null)");
                sb.AppendLine($"{indent}{{");
                sb.AppendLine($"{indent}    var array = new {elementTypeName}[{sourceVar}.{propertyName}.Length];");
                sb.AppendLine($"{indent}    for (int i = 0; i < {sourceVar}.{propertyName}.Length; i++)");
                sb.AppendLine($"{indent}    {{");
                if (cloneExpr != null)
                {
                    sb.AppendLine($"{indent}        array[i] = {cloneExpr};");
                }
                else
                {
                    sb.AppendLine($"{indent}        array[i] = {itemExpr};");
                }
                sb.AppendLine($"{indent}    }}");
                sb.AppendLine($"{indent}    {targetVar}.{propertyName} = array;");
                sb.AppendLine($"{indent}}}");
                sb.AppendLine($"{indent}else");
                sb.AppendLine($"{indent}{{");
                sb.AppendLine($"{indent}    {targetVar}.{propertyName} = null;");
                sb.AppendLine($"{indent}}}");
            }
            else
            {
                sb.AppendLine($"{indent}var array_{propertyName} = new {elementTypeName}[{sourceVar}.{propertyName}.Length];");
                sb.AppendLine($"{indent}for (int i = 0; i < {sourceVar}.{propertyName}.Length; i++)");
                sb.AppendLine($"{indent}{{");
                if (cloneExpr != null)
                {
                    sb.AppendLine($"{indent}    array_{propertyName}[i] = {cloneExpr};");
                }
                else
                {
                    sb.AppendLine($"{indent}    array_{propertyName}[i] = {itemExpr};");
                }
                sb.AppendLine($"{indent}}}");
                sb.AppendLine($"{indent}{targetVar}.{propertyName} = array_{propertyName};");
            }
            return;
        }
        
        // Value types or immutable types - use AsSpan().ToArray()
        if (IsValueOrImmutableType(elementType))
        {
            if (isNullable)
            {
                sb.AppendLine($"{indent}{targetVar}.{propertyName} = {sourceVar}.{propertyName} != null ? {sourceVar}.{propertyName}.AsSpan().ToArray() : null;");
            }
            else
            {
                sb.AppendLine($"{indent}{targetVar}.{propertyName} = {sourceVar}.{propertyName}.AsSpan().ToArray();");
            }
            return;
        }
        
        // Reference types - use Clone()
        var arrayTypeFullName = elementTypeName + "[]";
        if (isNullable)
        {
            sb.AppendLine($"{indent}{targetVar}.{propertyName} = {sourceVar}.{propertyName} != null ? ({arrayTypeFullName}){sourceVar}.{propertyName}.Clone() : null;");
        }
        else
        {
            sb.AppendLine($"{indent}{targetVar}.{propertyName} = ({arrayTypeFullName}){sourceVar}.{propertyName}.Clone();");
        }
    }
    
    private static void GenerateCollectionCloneStatements(StringBuilder sb, IPropertySymbol property, INamedTypeSymbol collectionType, string sourceVar, string targetVar, string indent)
    {
        if (collectionType.TypeArguments.Length == 0)
        {
            sb.AppendLine($"{indent}{targetVar}.{property.Name} = {sourceVar}.{property.Name};");
            return;
        }
        
        var elementType = collectionType.TypeArguments[0];
        var propertyName = property.Name;
        var typeName = collectionType.OriginalDefinition.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        var elementTypeName = elementType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        var isNullable = property.NullableAnnotation == NullableAnnotation.Annotated;
        var isCloneable = IsCloneableType(elementType);
        
        // Helper to generate null check wrapper
        void GenerateWithNullCheck(Action generateBody)
        {
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
        
        string tempIndent = isNullable ? indent + "    " : indent;
        
        // Stack
        if (typeName == "global::System.Collections.Generic.Stack<T>")
        {
            GenerateWithNullCheck(() =>
            {
                if (isCloneable)
                {
                    sb.AppendLine($"{tempIndent}var temp = new System.Collections.Generic.List<{elementTypeName}>();");
                    sb.AppendLine($"{tempIndent}foreach (var item in {sourceVar}.{propertyName})");
                    sb.AppendLine($"{tempIndent}{{");
                    var cloneStmt = GetItemCloneStatement(elementType, "item");
                    sb.AppendLine($"{tempIndent}    temp.Add({cloneStmt});");
                    sb.AppendLine($"{tempIndent}}}");
                    sb.AppendLine($"{tempIndent}temp.Reverse();");
                    sb.AppendLine($"{tempIndent}{targetVar}.{propertyName} = new System.Collections.Generic.Stack<{elementTypeName}>(temp);");
                }
                else
                {
                    sb.AppendLine($"{tempIndent}var temp = new System.Collections.Generic.List<{elementTypeName}>({sourceVar}.{propertyName});");
                    sb.AppendLine($"{tempIndent}temp.Reverse();");
                    sb.AppendLine($"{tempIndent}{targetVar}.{propertyName} = new System.Collections.Generic.Stack<{elementTypeName}>(temp);");
                }
            });
            return;
        }
        
        // Queue
        if (typeName == "global::System.Collections.Generic.Queue<T>")
        {
            GenerateWithNullCheck(() =>
            {
                sb.AppendLine($"{tempIndent}{targetVar}.{propertyName} = new System.Collections.Generic.Queue<{elementTypeName}>();");
                if (isCloneable)
                {
                    sb.AppendLine($"{tempIndent}foreach (var item in {sourceVar}.{propertyName})");
                    sb.AppendLine($"{tempIndent}{{");
                    var cloneStmt = GetItemCloneStatement(elementType, "item");
                    sb.AppendLine($"{tempIndent}    {targetVar}.{propertyName}.Enqueue({cloneStmt});");
                    sb.AppendLine($"{tempIndent}}}");
                }
                else
                {
                    sb.AppendLine($"{tempIndent}foreach (var item in {sourceVar}.{propertyName})");
                    sb.AppendLine($"{tempIndent}{{");
                    sb.AppendLine($"{tempIndent}    {targetVar}.{propertyName}.Enqueue(item);");
                    sb.AppendLine($"{tempIndent}}}");
                }
            });
            return;
        }
        
        // HashSet
        if (typeName == "global::System.Collections.Generic.HashSet<T>")
        {
            GenerateWithNullCheck(() =>
            {
                sb.AppendLine($"{tempIndent}{targetVar}.{propertyName} = new System.Collections.Generic.HashSet<{elementTypeName}>();");
                if (isCloneable)
                {
                    sb.AppendLine($"{tempIndent}foreach (var item in {sourceVar}.{propertyName})");
                    sb.AppendLine($"{tempIndent}{{");
                    var cloneStmt = GetItemCloneStatement(elementType, "item");
                    sb.AppendLine($"{tempIndent}    {targetVar}.{propertyName}.Add({cloneStmt});");
                    sb.AppendLine($"{tempIndent}}}");
                }
                else
                {
                    sb.AppendLine($"{tempIndent}foreach (var item in {sourceVar}.{propertyName})");
                    sb.AppendLine($"{tempIndent}{{");
                    sb.AppendLine($"{tempIndent}    {targetVar}.{propertyName}.Add(item);");
                    sb.AppendLine($"{tempIndent}}}");
                }
            });
            return;
        }
        
        // ReadOnlyCollection - needs special handling
        if (typeName == "global::System.Collections.ObjectModel.ReadOnlyCollection<T>")
        {
            GenerateWithNullCheck(() =>
            {
                sb.AppendLine($"{tempIndent}var tempList = new System.Collections.Generic.List<{elementTypeName}>();");
                if (isCloneable)
                {
                    sb.AppendLine($"{tempIndent}foreach (var item in {sourceVar}.{propertyName})");
                    sb.AppendLine($"{tempIndent}{{");
                    var cloneStmt = GetItemCloneStatement(elementType, "item");
                    sb.AppendLine($"{tempIndent}    tempList.Add({cloneStmt});");
                    sb.AppendLine($"{tempIndent}}}");
                }
                else
                {
                    sb.AppendLine($"{tempIndent}foreach (var item in {sourceVar}.{propertyName})");
                    sb.AppendLine($"{tempIndent}{{");
                    sb.AppendLine($"{tempIndent}    tempList.Add(item);");
                    sb.AppendLine($"{tempIndent}}}");
                }
                sb.AppendLine($"{tempIndent}{targetVar}.{propertyName} = new System.Collections.ObjectModel.ReadOnlyCollection<{elementTypeName}>(tempList);");
            });
            return;
        }
        
        // SortedSet, ObservableCollection, List, and others - similar pattern
        string collectionTypeName = null;
        bool isList = false;
        if (typeName == "global::System.Collections.Generic.SortedSet<T>")
            collectionTypeName = $"System.Collections.Generic.SortedSet<{elementTypeName}>";
        else if (typeName == "global::System.Collections.ObjectModel.ObservableCollection<T>")
            collectionTypeName = $"System.Collections.ObjectModel.ObservableCollection<{elementTypeName}>";
        else if (typeName.StartsWith("global::System.Collections.Generic.List<"))
        {
            collectionTypeName = $"System.Collections.Generic.List<{elementTypeName}>";
            isList = true;
        }
        
        if (collectionTypeName != null)
        {
            GenerateWithNullCheck(() =>
            {
                sb.AppendLine($"{tempIndent}{targetVar}.{propertyName} = new {collectionTypeName}();");
                if (isCloneable)
                {
                    // For List, use CollectionsMarshal.SetCount for performance
                    if (isList)
                    {
                        sb.AppendLine($"{tempIndent}System.Runtime.InteropServices.CollectionsMarshal.SetCount({targetVar}.{propertyName}, {sourceVar}.{propertyName}.Count);");
                        sb.AppendLine($"{tempIndent}for (int i = 0; i < {sourceVar}.{propertyName}.Count; i++)");
                        sb.AppendLine($"{tempIndent}{{");
                        var cloneStmt = GetItemCloneStatement(elementType, $"{sourceVar}.{propertyName}[i]");
                        sb.AppendLine($"{tempIndent}    {targetVar}.{propertyName}[i] = {cloneStmt};");
                        sb.AppendLine($"{tempIndent}}}");
                    }
                    else
                    {
                        sb.AppendLine($"{tempIndent}foreach (var item in {sourceVar}.{propertyName})");
                        sb.AppendLine($"{tempIndent}{{");
                        var cloneStmt = GetItemCloneStatement(elementType, "item");
                        sb.AppendLine($"{tempIndent}    {targetVar}.{propertyName}.Add({cloneStmt});");
                        sb.AppendLine($"{tempIndent}}}");
                    }
                }
                else if (elementType is INamedTypeSymbol elementNamedType && IsCollectionType(elementNamedType))
                {
                    // Nested collection - need to recursively clone
                    if (elementNamedType.TypeArguments.Length > 0)
                    {
                        var nestedElementType = elementNamedType.TypeArguments[0];
                        var nestedElementTypeName = nestedElementType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                        var nestedIsCloneable = IsCloneableType(nestedElementType);
                        
                        sb.AppendLine($"{tempIndent}foreach (var item in {sourceVar}.{propertyName})");
                        sb.AppendLine($"{tempIndent}{{");
                        sb.AppendLine($"{tempIndent}    if (item != null)");
                        sb.AppendLine($"{tempIndent}    {{");
                        sb.AppendLine($"{tempIndent}        var nestedClone = new {elementTypeName}();");
                        
                        if (nestedIsCloneable)
                        {
                            sb.AppendLine($"{tempIndent}        foreach (var nestedItem in item)");
                            sb.AppendLine($"{tempIndent}        {{");
                            var nestedCloneStmt = GetItemCloneStatement(nestedElementType, "nestedItem");
                            sb.AppendLine($"{tempIndent}            nestedClone.Add({nestedCloneStmt});");
                            sb.AppendLine($"{tempIndent}        }}");
                        }
                        else if (nestedElementType is INamedTypeSymbol nestedNamedType && IsCollectionType(nestedNamedType))
                        {
                            // Triple nested collection
                            if (nestedNamedType.TypeArguments.Length > 0)
                            {
                                var tripleNestedElementType = nestedNamedType.TypeArguments[0];
                                var tripleNestedElementTypeName = tripleNestedElementType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                                
                                sb.AppendLine($"{tempIndent}        foreach (var nestedItem in item)");
                                sb.AppendLine($"{tempIndent}        {{");
                                sb.AppendLine($"{tempIndent}            if (nestedItem != null)");
                                sb.AppendLine($"{tempIndent}            {{");
                                sb.AppendLine($"{tempIndent}                var tripleNestedClone = new {nestedElementTypeName}();");
                                sb.AppendLine($"{tempIndent}                foreach (var tripleNestedItem in nestedItem)");
                                sb.AppendLine($"{tempIndent}                {{");
                                sb.AppendLine($"{tempIndent}                    tripleNestedClone.Add(tripleNestedItem);");
                                sb.AppendLine($"{tempIndent}                }}");
                                sb.AppendLine($"{tempIndent}                nestedClone.Add(tripleNestedClone);");
                                sb.AppendLine($"{tempIndent}            }}");
                                sb.AppendLine($"{tempIndent}            else");
                                sb.AppendLine($"{tempIndent}            {{");
                                sb.AppendLine($"{tempIndent}                nestedClone.Add(null);");
                                sb.AppendLine($"{tempIndent}            }}");
                                sb.AppendLine($"{tempIndent}        }}");
                            }
                            else
                            {
                                sb.AppendLine($"{tempIndent}        foreach (var nestedItem in item)");
                                sb.AppendLine($"{tempIndent}        {{");
                                sb.AppendLine($"{tempIndent}            nestedClone.Add(nestedItem);");
                                sb.AppendLine($"{tempIndent}        }}");
                            }
                        }
                        else
                        {
                            sb.AppendLine($"{tempIndent}        foreach (var nestedItem in item)");
                            sb.AppendLine($"{tempIndent}        {{");
                            sb.AppendLine($"{tempIndent}            nestedClone.Add(nestedItem);");
                            sb.AppendLine($"{tempIndent}        }}");
                        }
                        
                        sb.AppendLine($"{tempIndent}        {targetVar}.{propertyName}.Add(nestedClone);");
                        sb.AppendLine($"{tempIndent}    }}");
                        sb.AppendLine($"{tempIndent}    else");
                        sb.AppendLine($"{tempIndent}    {{");
                        sb.AppendLine($"{tempIndent}        {targetVar}.{propertyName}.Add(null);");
                        sb.AppendLine($"{tempIndent}    }}");
                        sb.AppendLine($"{tempIndent}}}");
                    }
                    else
                    {
                        sb.AppendLine($"{tempIndent}foreach (var item in {sourceVar}.{propertyName})");
                        sb.AppendLine($"{tempIndent}{{");
                        sb.AppendLine($"{tempIndent}    {targetVar}.{propertyName}.Add(item);");
                        sb.AppendLine($"{tempIndent}}}");
                    }
                }
                else if (elementType is INamedTypeSymbol elementRefType && !elementRefType.IsValueType && elementRefType.SpecialType != SpecialType.System_String)
                {
                    var props = GetCloneableProperties(elementRefType);
                    if (props.Count > 0)
                    {
                        var fullName = elementRefType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat).Replace("global::", "");
                        var hasCloneInternal = s_currentNameGenerator?.HasCloneInternal(fullName) ?? false;
                        
                        if (hasCloneInternal)
                        {
                            var cloneInternalName = s_currentNameGenerator!.GetCloneInternalName(fullName);
                            sb.AppendLine($"{tempIndent}foreach (var item in {sourceVar}.{propertyName})");
                            sb.AppendLine($"{tempIndent}{{");
                            sb.AppendLine($"{tempIndent}    {targetVar}.{propertyName}.Add(item != null ? {cloneInternalName}(item) : null);");
                            sb.AppendLine($"{tempIndent}}}");
                        }
                        else
                        {
                            // Generate inline cloning for types without CloneInternal
                            var elementFullTypeName = elementRefType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                            sb.AppendLine($"{tempIndent}foreach (var item in {sourceVar}.{propertyName})");
                            sb.AppendLine($"{tempIndent}{{");
                            sb.AppendLine($"{tempIndent}    if (item != null)");
                            sb.AppendLine($"{tempIndent}    {{");
                            sb.AppendLine($"{tempIndent}        var clonedItem = new {elementFullTypeName}();");
                            foreach (var prop in props)
                            {
                                var nestedExpr = GenerateCloneExpression(prop, "item");
                                sb.AppendLine($"{tempIndent}        clonedItem.{prop.Name} = {nestedExpr};");
                            }
                            sb.AppendLine($"{tempIndent}        {targetVar}.{propertyName}.Add(clonedItem);");
                            sb.AppendLine($"{tempIndent}    }}");
                            sb.AppendLine($"{tempIndent}    else");
                            sb.AppendLine($"{tempIndent}    {{");
                            sb.AppendLine($"{tempIndent}        {targetVar}.{propertyName}.Add(null);");
                            sb.AppendLine($"{tempIndent}    }}");
                            sb.AppendLine($"{tempIndent}}}");
                        }
                    }
                    else
                    {
                        sb.AppendLine($"{tempIndent}foreach (var item in {sourceVar}.{propertyName})");
                        sb.AppendLine($"{tempIndent}{{");
                        sb.AppendLine($"{tempIndent}    {targetVar}.{propertyName}.Add(item);");
                        sb.AppendLine($"{tempIndent}}}");
                    }
                }
                else
                {
                    sb.AppendLine($"{tempIndent}foreach (var item in {sourceVar}.{propertyName})");
                    sb.AppendLine($"{tempIndent}{{");
                    sb.AppendLine($"{tempIndent}    {targetVar}.{propertyName}.Add(item);");
                    sb.AppendLine($"{tempIndent}}}");
                }
            });
            return;
        }
        
        // Fallback to simple assignment
        sb.AppendLine($"{indent}{targetVar}.{propertyName} = {sourceVar}.{propertyName};");
    }
    
    private static void GenerateDictionaryCloneStatements(StringBuilder sb, IPropertySymbol property, INamedTypeSymbol dictionaryType, string sourceVar, string targetVar, string indent)
    {
        if (dictionaryType.TypeArguments.Length < 2)
        {
            sb.AppendLine($"{indent}{targetVar}.{property.Name} = {sourceVar}.{property.Name};");
            return;
        }
        
        var keyType = dictionaryType.TypeArguments[0];
        var valueType = dictionaryType.TypeArguments[1];
        var propertyName = property.Name;
        var keyTypeName = keyType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        var valueTypeName = valueType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        var isNullable = property.NullableAnnotation == NullableAnnotation.Annotated;
        var valueIsCloneable = IsCloneableType(valueType);
        var typeName = dictionaryType.OriginalDefinition.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        
        // ImmutableDictionary - just assign (immutable)
        if (typeName.StartsWith("global::System.Collections.Immutable.ImmutableDictionary<"))
        {
            if (isNullable || !IsValueOrImmutableType(valueType))
            {
                sb.AppendLine($"{indent}{targetVar}.{propertyName} = {sourceVar}.{propertyName};");
            }
            else
            {
                sb.AppendLine($"{indent}{targetVar}.{propertyName} = {sourceVar}.{propertyName};");
            }
            return;
        }
        
        string tempIndent = isNullable ? indent + "    " : indent;
        
        void GenerateWithNullCheck(Action generateBody)
        {
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
        
        GenerateWithNullCheck(() =>
        {
            sb.AppendLine($"{tempIndent}{targetVar}.{propertyName} = new System.Collections.Generic.Dictionary<{keyTypeName}, {valueTypeName}>();");
            
            if (valueIsCloneable)
            {
                sb.AppendLine($"{tempIndent}foreach (var kvp in {sourceVar}.{propertyName})");
                sb.AppendLine($"{tempIndent}{{");
                var cloneStmt = GetItemCloneStatement(valueType, "kvp.Value");
                sb.AppendLine($"{tempIndent}    {targetVar}.{propertyName}[kvp.Key] = {cloneStmt};");
                sb.AppendLine($"{tempIndent}}}");
            }
            else if (valueType is INamedTypeSymbol valueRefType && !valueRefType.IsValueType && valueRefType.SpecialType != SpecialType.System_String)
            {
                var props = GetCloneableProperties(valueRefType);
                if (props.Count > 0)
                {
                    var fullName = valueRefType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat).Replace("global::", "");
                    var hasCloneInternal = s_currentNameGenerator?.HasCloneInternal(fullName) ?? false;
                    
                    if (hasCloneInternal)
                    {
                        var cloneInternalName = s_currentNameGenerator!.GetCloneInternalName(fullName);
                        sb.AppendLine($"{tempIndent}foreach (var kvp in {sourceVar}.{propertyName})");
                        sb.AppendLine($"{tempIndent}{{");
                        sb.AppendLine($"{tempIndent}    {targetVar}.{propertyName}[kvp.Key] = kvp.Value != null ? {cloneInternalName}(kvp.Value) : null;");
                        sb.AppendLine($"{tempIndent}}}");
                    }
                    else
                    {
                        // Generate inline cloning for types without CloneInternal
                        var valueFullTypeName = valueRefType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                        sb.AppendLine($"{tempIndent}foreach (var kvp in {sourceVar}.{propertyName})");
                        sb.AppendLine($"{tempIndent}{{");
                        sb.AppendLine($"{tempIndent}    if (kvp.Value != null)");
                        sb.AppendLine($"{tempIndent}    {{");
                        sb.AppendLine($"{tempIndent}        var clonedValue = new {valueFullTypeName}();");
                        foreach (var prop in props)
                        {
                            var nestedExpr = GenerateCloneExpression(prop, "kvp.Value");
                            sb.AppendLine($"{tempIndent}        clonedValue.{prop.Name} = {nestedExpr};");
                        }
                        sb.AppendLine($"{tempIndent}        {targetVar}.{propertyName}[kvp.Key] = clonedValue;");
                        sb.AppendLine($"{tempIndent}    }}");
                        sb.AppendLine($"{tempIndent}    else");
                        sb.AppendLine($"{tempIndent}    {{");
                        sb.AppendLine($"{tempIndent}        {targetVar}.{propertyName}[kvp.Key] = null;");
                        sb.AppendLine($"{tempIndent}    }}");
                        sb.AppendLine($"{tempIndent}}}");
                    }
                }
                else
                {
                    sb.AppendLine($"{tempIndent}foreach (var kvp in {sourceVar}.{propertyName})");
                    sb.AppendLine($"{tempIndent}{{");
                    sb.AppendLine($"{tempIndent}    {targetVar}.{propertyName}[kvp.Key] = kvp.Value;");
                    sb.AppendLine($"{tempIndent}}}");
                }
            }
            else
            {
                sb.AppendLine($"{tempIndent}foreach (var kvp in {sourceVar}.{propertyName})");
                sb.AppendLine($"{tempIndent}{{");
                sb.AppendLine($"{tempIndent}    {targetVar}.{propertyName}[kvp.Key] = kvp.Value;");
                sb.AppendLine($"{tempIndent}}}");
            }
        });
    }
    
    private static string GenerateCloneExpression(IPropertySymbol property, string objectName)
    {
        var typeSymbol = property.Type;
        
        // Value types and immutable types - simple assignment
        if (IsValueOrImmutableType(typeSymbol))
        {
            return $"{objectName}.{property.Name}";
        }
        
        // Arrays
        if (typeSymbol is IArrayTypeSymbol arrayType)
        {
            return GenerateArrayClone(property, arrayType, objectName);
        }
        
        // Named types (classes, interfaces, etc.)
        if (typeSymbol is INamedTypeSymbol namedType)
        {
            // Check if it's a cloneable type
            if (IsCloneableType(namedType))
            {
                var isNullable = property.NullableAnnotation == NullableAnnotation.Annotated;
                var cloneExpr = GetCloneExpression(namedType, $"{objectName}.{property.Name}", isNullable);
                return cloneExpr ?? $"{objectName}.{property.Name}";
            }
            
            // Dictionaries
            if (IsDictionaryType(namedType))
            {
                return GenerateDictionaryClone(property, namedType, objectName);
            }
            
            // Collections
            if (IsCollectionType(namedType))
            {
                return GenerateCollectionClone(property, namedType, objectName);
            }
            
            // Reference types with properties
            if (!namedType.IsValueType && namedType.SpecialType != SpecialType.System_String)
            {
                var props = GetCloneableProperties(namedType);
                if (props.Count > 0)
                {
                    var fullName = namedType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat).Replace("global::", "");
                    var hasCloneInternal = s_currentNameGenerator?.HasCloneInternal(fullName) ?? false;
                    var fullTypeName = namedType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                    var isNullable = property.NullableAnnotation == NullableAnnotation.Annotated;
                    
                    if (hasCloneInternal)
                    {
                        // Call CloneInternal for types that have it registered
                        var cloneInternalName = s_currentNameGenerator!.GetCloneInternalName(fullName);
                        
                        if (isNullable)
                        {
                            return $"{objectName}.{property.Name} != null ? {cloneInternalName}({objectName}.{property.Name}) : null";
                        }
                        else
                        {
                            return $"{cloneInternalName}({objectName}.{property.Name})";
                        }
                    }
                    else
                    {
                        // Generate inline object initializer for types without CloneInternal
                        var assignments = props.Select(p => 
                            $"{p.Name} = {GenerateCloneExpression(p, $"{objectName}.{property.Name}")}"
                        );
                        
                        if (isNullable)
                        {
                            return $"{objectName}.{property.Name} != null ? new {fullTypeName} {{ {string.Join(", ", assignments)} }} : null";
                        }
                        else
                        {
                            return $"new {fullTypeName} {{ {string.Join(", ", assignments)} }}";
                        }
                    }
                }
            }
        }
        
        // Default: simple assignment
        return $"{objectName}.{property.Name}";
    }
    
    private static string GenerateArrayClone(IPropertySymbol property, IArrayTypeSymbol arrayType, string objectName)
    {
        var elementType = arrayType.ElementType;
        var propertyName = property.Name;
        
        // Multi-dimensional arrays
        if (arrayType.Rank > 1)
        {
            var elementTypeName = elementType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
            var rankCommas = new string(',', arrayType.Rank - 1);
            var arrayTypeName = $"{elementTypeName}[{rankCommas}]";
            return $"{objectName}.{propertyName} != null ? ({arrayTypeName}){objectName}.{propertyName}.Clone() : null";
        }
        
        // Cloneable elements
        if (IsCloneableType(elementType))
        {
            // Check if we have CloneInternal for this type
            var fullTypeName = elementType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat).Replace("global::", "");
            var nameGenerator = s_currentNameGenerator;
            
            if (nameGenerator != null && nameGenerator.HasCloneInternal(fullTypeName))
            {
                var cloneInternalName = nameGenerator.GetCloneInternalName(fullTypeName);
                var elementIsNullable = elementType.NullableAnnotation == NullableAnnotation.Annotated;
                if (elementIsNullable)
                {
                    return $"{objectName}.{propertyName}?.Select(x => x != null ? {cloneInternalName}(x) : null).ToArray()";
                }
                else
                {
                    return $"{objectName}.{propertyName}?.Select(x => {cloneInternalName}(x)).ToArray()";
                }
            }
            else
            {
                // Fallback to direct assignment
                return $"{objectName}.{propertyName}?.ToArray()";
            }
        }
        
        // Value types or immutable types
        if (IsValueOrImmutableType(elementType))
        {
            return $"{objectName}.{propertyName} != null ? {objectName}.{propertyName}.AsSpan().ToArray() : null";
        }
        
        // Reference types - use Clone()
        var arrayTypeFullName = elementType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) + "[]";
        return $"{objectName}.{propertyName} != null ? ({arrayTypeFullName}){objectName}.{propertyName}.Clone() : null";
    }
    
    private static string GenerateDictionaryClone(IPropertySymbol property, INamedTypeSymbol dictionaryType, string objectName)
    {
        if (dictionaryType.TypeArguments.Length < 2)
            return $"{objectName}.{property.Name}";
        
        var keyType = dictionaryType.TypeArguments[0];
        var valueType = dictionaryType.TypeArguments[1];
        var propertyName = property.Name;
        
        bool valueIsCloneable = IsCloneableType(valueType);
        
        var typeName = dictionaryType.OriginalDefinition.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        
        // Immutable dictionaries
        if (typeName.StartsWith("global::System.Collections.Immutable.ImmutableDictionary<"))
        {
            if (valueIsCloneable)
            {
                var cloneStmt = GetItemCloneStatement(valueType, "kvp.Value");
                return $"{objectName}.{propertyName}?.ToImmutableDictionary(kvp => kvp.Key, kvp => {cloneStmt})";
            }
            return $"{objectName}.{propertyName}";
        }
        
        // Regular dictionaries
        if (valueIsCloneable)
        {
            var cloneStmt = GetItemCloneStatement(valueType, "kvp.Value");
            return $"{objectName}.{propertyName}?.ToDictionary(kvp => kvp.Key, kvp => {cloneStmt})";
        }
        
        // Check if value is a reference type with properties
        if (valueType is INamedTypeSymbol valueRefType && !valueRefType.IsValueType && valueRefType.SpecialType != SpecialType.System_String)
        {
            var props = GetCloneableProperties(valueRefType);
            if (props.Count > 0)
            {
                // Generate object initializer for each value
                var assignments = props.Select(p => 
                    $"{p.Name} = {GenerateCloneExpression(p, "kvp.Value")}"
                );
                var valueTypeFullName = valueRefType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                var keyTypeName = keyType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                return $"{objectName}.{propertyName}?.ToDictionary(kvp => kvp.Key, kvp => kvp.Value != null ? new {valueTypeFullName} {{ {string.Join(", ", assignments)} }} : null)";
            }
        }
        
        var keyTypeNameDefault = keyType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        var valueTypeNameDefault = valueType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        return $"{objectName}.{propertyName} != null ? new System.Collections.Generic.Dictionary<{keyTypeNameDefault}, {valueTypeNameDefault}>({objectName}.{propertyName}) : null";
    }
    
    private static string GenerateCollectionClone(IPropertySymbol property, INamedTypeSymbol collectionType, string objectName)
    {
        if (collectionType.TypeArguments.Length == 0)
            return $"{objectName}.{property.Name}";
        
        var elementType = collectionType.TypeArguments[0];
        var propertyName = property.Name;
        var typeName = collectionType.OriginalDefinition.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        
        bool isCloneable = IsCloneableType(elementType);
        var elementTypeName = elementType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        
        // Helper to get clone expression for element
        string GetElementClone()
        {
            if (!isCloneable) return "x";
            return GetItemCloneStatement(elementType, "x");
        }
        
        string elementClone = GetElementClone();
        
        // Stack
        if (typeName == "global::System.Collections.Generic.Stack<T>")
        {
            if (isCloneable)
            {
                return $"{objectName}.{propertyName} != null ? new System.Collections.Generic.Stack<{elementTypeName}>({objectName}.{propertyName}.Reverse().Select(x => {elementClone})) : null";
            }
            return $"{objectName}.{propertyName} != null ? new System.Collections.Generic.Stack<{elementTypeName}>({objectName}.{propertyName}.Reverse()) : null";
        }
        
        // Queue
        if (typeName == "global::System.Collections.Generic.Queue<T>")
        {
            if (isCloneable)
            {
                return $"{objectName}.{propertyName} != null ? new System.Collections.Generic.Queue<{elementTypeName}>({objectName}.{propertyName}.Select(x => {elementClone})) : null";
            }
            return $"{objectName}.{propertyName} != null ? new System.Collections.Generic.Queue<{elementTypeName}>({objectName}.{propertyName}) : null";
        }
        
        // HashSet
        if (typeName == "global::System.Collections.Generic.HashSet<T>")
        {
            if (isCloneable)
            {
                return $"{objectName}.{propertyName} != null ? new System.Collections.Generic.HashSet<{elementTypeName}>({objectName}.{propertyName}.Select(x => {elementClone})) : null";
            }
            return $"{objectName}.{propertyName} != null ? new System.Collections.Generic.HashSet<{elementTypeName}>({objectName}.{propertyName}) : null";
        }
        
        // SortedSet
        if (typeName == "global::System.Collections.Generic.SortedSet<T>")
        {
            if (isCloneable)
            {
                return $"{objectName}.{propertyName} != null ? new System.Collections.Generic.SortedSet<{elementTypeName}>({objectName}.{propertyName}.Select(x => {elementClone})) : null";
            }
            return $"{objectName}.{propertyName} != null ? new System.Collections.Generic.SortedSet<{elementTypeName}>({objectName}.{propertyName}) : null";
        }
        
        // ObservableCollection
        if (typeName == "global::System.Collections.ObjectModel.ObservableCollection<T>")
        {
            if (isCloneable)
            {
                return $"{objectName}.{propertyName} != null ? new System.Collections.ObjectModel.ObservableCollection<{elementTypeName}>({objectName}.{propertyName}.Select(x => {elementClone})) : null";
            }
            return $"{objectName}.{propertyName} != null ? new System.Collections.ObjectModel.ObservableCollection<{elementTypeName}>({objectName}.{propertyName}) : null";
        }
        
        // ReadOnlyCollection
        if (typeName == "global::System.Collections.ObjectModel.ReadOnlyCollection<T>")
        {
            if (isCloneable)
            {
                return $"{objectName}.{propertyName} != null ? new System.Collections.ObjectModel.ReadOnlyCollection<{elementTypeName}>({objectName}.{propertyName}.Select(x => {elementClone}).ToList()) : null";
            }
            return $"{objectName}.{propertyName} != null ? new System.Collections.ObjectModel.ReadOnlyCollection<{elementTypeName}>({objectName}.{propertyName}.ToList()) : null";
        }
        
        // ImmutableList
        if (typeName.StartsWith("global::System.Collections.Immutable.ImmutableList<"))
        {
            if (isCloneable)
            {
                return $"{objectName}.{propertyName}?.Select(x => {elementClone}).ToImmutableList()";
            }
            if (IsValueOrImmutableType(elementType))
            {
                return $"{objectName}.{propertyName}";
            }
            return $"{objectName}.{propertyName}?.ToImmutableList()";
        }
        
        // ImmutableArray
        if (typeName.StartsWith("global::System.Collections.Immutable.ImmutableArray<"))
        {
            if (isCloneable)
            {
                return $"{objectName}.{propertyName}.IsDefault ? default : {objectName}.{propertyName}.Select(x => {elementClone}).ToImmutableArray()";
            }
            if (IsValueOrImmutableType(elementType))
            {
                return $"{objectName}.{propertyName}";
            }
            return $"{objectName}.{propertyName}.IsDefault ? default : {objectName}.{propertyName}.ToImmutableArray()";
        }
        
        // ImmutableHashSet
        if (typeName.StartsWith("global::System.Collections.Immutable.ImmutableHashSet<"))
        {
            if (isCloneable)
            {
                return $"{objectName}.{propertyName}?.Select(x => {elementClone}).ToImmutableHashSet()";
            }
            if (IsValueOrImmutableType(elementType))
            {
                return $"{objectName}.{propertyName}";
            }
            return $"{objectName}.{propertyName}?.ToImmutableHashSet()";
        }
        
        // ImmutableQueue
        if (typeName.StartsWith("global::System.Collections.Immutable.ImmutableQueue<"))
        {
            if (isCloneable)
            {
                return $"{objectName}.{propertyName} == null ? System.Collections.Immutable.ImmutableQueue<{elementTypeName}>.Empty : System.Collections.Immutable.ImmutableQueue.CreateRange({objectName}.{propertyName}.Select(x => {elementClone}))";
            }
            if (IsValueOrImmutableType(elementType))
            {
                return $"{objectName}.{propertyName}";
            }
            return $"{objectName}.{propertyName} == null ? System.Collections.Immutable.ImmutableQueue<{elementTypeName}>.Empty : System.Collections.Immutable.ImmutableQueue.CreateRange({objectName}.{propertyName})";
        }
        
        // ImmutableStack
        if (typeName.StartsWith("global::System.Collections.Immutable.ImmutableStack<"))
        {
            if (isCloneable)
            {
                return $"{objectName}.{propertyName} == null ? System.Collections.Immutable.ImmutableStack<{elementTypeName}>.Empty : System.Collections.Immutable.ImmutableStack.CreateRange({objectName}.{propertyName}.Select(x => {elementClone}))";
            }
            if (IsValueOrImmutableType(elementType))
            {
                return $"{objectName}.{propertyName}";
            }
            return $"{objectName}.{propertyName} == null ? System.Collections.Immutable.ImmutableStack<{elementTypeName}>.Empty : System.Collections.Immutable.ImmutableStack.CreateRange({objectName}.{propertyName})";
        }
        
        // Regular collections (List, etc.) - default to List
        if (isCloneable)
        {
            return $"{objectName}.{propertyName}?.Select(x => {elementClone}).ToList()";
        }
        
        // Check if element is a collection (nested collection scenario)
        if (elementType is INamedTypeSymbol elementNamedType && IsCollectionType(elementNamedType))
        {
            // Generate nested collection clone
            var nestedClone = GenerateNestedCollectionClone(elementNamedType, "x");
            return $"{objectName}.{propertyName}?.Select(x => {nestedClone}).ToList()";
        }
        
        // Check if element is a reference type with properties
        if (elementType is INamedTypeSymbol elementRefType && !elementRefType.IsValueType && elementRefType.SpecialType != SpecialType.System_String)
        {
            var props = GetCloneableProperties(elementRefType);
            if (props.Count > 0)
            {
                // Generate object initializer for each element
                var assignments = props.Select(p => 
                    $"{p.Name} = {GenerateCloneExpression(p, "x")}"
                );
                var fullTypeName = elementRefType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                return $"{objectName}.{propertyName}?.Select(x => x != null ? new {fullTypeName} {{ {string.Join(", ", assignments)} }} : null).ToList()";
            }
        }
        
        return $"{objectName}.{propertyName} != null ? new System.Collections.Generic.List<{elementTypeName}>({objectName}.{propertyName}) : null";
    }
    
    private static string GenerateNestedCollectionClone(INamedTypeSymbol collectionType, string varName)
    {
        if (collectionType.TypeArguments.Length == 0)
            return varName;
        
        var elementType = collectionType.TypeArguments[0];
        var typeName = collectionType.OriginalDefinition.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        
        // Check if element is cloneable
        bool isCloneable = IsCloneableType(elementType);
        
        if (isCloneable)
        {
            var cloneStmt = GetItemCloneStatement(elementType, "item");
            return $"{varName}?.Select(item => {cloneStmt}).ToList()";
        }
        
        // Check if element is itself a collection
        if (elementType is INamedTypeSymbol nestedCollectionType && IsCollectionType(nestedCollectionType))
        {
            var nestedClone = GenerateNestedCollectionClone(nestedCollectionType, "item");
            return $"{varName}?.Select(item => {nestedClone}).ToList()";
        }
        
        // Check if element is a reference type with properties
        if (elementType is INamedTypeSymbol elementRefType && !elementRefType.IsValueType && elementRefType.SpecialType != SpecialType.System_String)
        {
            var props = GetCloneableProperties(elementRefType);
            if (props.Count > 0)
            {
                // Generate object initializer for each element
                var assignments = props.Select(p => 
                    $"{p.Name} = {GenerateCloneExpression(p, "item")}"
                );
                var fullTypeName = elementRefType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                return $"{varName}?.Select(item => item != null ? new {fullTypeName} {{ {string.Join(", ", assignments)} }} : null).ToList()";
            }
        }
        
        // For value types and immutable types
        if (IsValueOrImmutableType(elementType))
        {
            return $"{varName} != null ? new System.Collections.Generic.List<{elementType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}>({varName}) : null";
        }
        
        // Default
        return $"{varName} != null ? new System.Collections.Generic.List<{elementType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}>({varName}) : null";
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
        EquatableArray<string> ContainedTypeFullNames,
        bool IsNullable,
        bool IsRecord,
        bool IsValueType,
        bool AllChildrenAreValueOrImmutable,
        bool HasCollectionInitializer,
        bool ShouldGenerateDeepClone,
        bool IsAbstract,
        string TypeKeyword,
        EquatableArray<string> ContainingTypes,
        string? BaseCloneableTypeFullName
    );
}
