# IDeepCloneable Source Generator - Implementation Guidelines

## Overview

This source generator automatically generates `DeepClone()` methods for types marked with the `[DeepCloneable]` attribute. It follows a pattern where:
1. An internal `_CloneInternal` extension method is generated for each type
2. A public `DeepClone()` method is generated that calls the internal method
3. Special collection types (List, Dictionary, Arrays) get optimized clone implementations

## Architecture

### Class Structure

The generator is organized into separate, complete classes (not partial classes):

- **CloneableGenerator**: Main orchestrator, implements `IIncrementalGenerator`
- **ClassInfo**: Record holding metadata about a class (name, namespace, properties, type characteristics)
- **PropertyInfo**: Record holding metadata about a property/field
- **TypeAnalyzer**: Analyzes types and recursively discovers all reachable types
- **CodeGenerator**: Generates source code using `IndentedStringBuilder`
- **SpecialTypeInfo**: Abstract base class for special collection type handlers
  - **ListTypeInfo**: Handler for `List<T>` collections
  - **DictionaryTypeInfo**: Handler for `Dictionary<TKey, TValue>` collections
- **CodeGenerationUtility**: Utility methods for type name manipulation and immutability checks

### Type Discovery Process (GetRelationalAllClassInfo)

Extract and return `ClassInfo` for all classes reachable from a `[DeepCloneable]` marked type:

**"Reachable" Definition:**
1. The class marked with `[DeepCloneable]` itself (A)
2. All classes that inherit from A
3. All classes referenced by properties/fields of A (recursively)

**ClassInfo Properties:**
- `string ClassName`: Simple class name
- `string FullClassName`: Fully qualified name starting with `global::`
- `string Namespace`: Namespace of the class
- `EquatableArray<PropertyInfo> Properties`: Child properties (immediate children only, not grandchildren)
- `bool IsNullable`: Whether the type is nullable
- `bool IsRecord`: Whether the type is a record
- `bool IsValueType`: Whether it's a value type or reference type
- `bool IsAllImmutable`: Whether all internal types are value types or immutable types (string, etc.)
- `bool IsCollection`: Whether it's an array or has collection initializer
- `bool NeedsDeepCloneMethod`: Whether to generate `DeepClone()` method (has `[DeepCloneable]` attribute or inherits from `[DeepCloneable]` class)
- `bool IsSealed`: Whether the type is sealed
- `bool BaseHasDeepClone`: Whether the base class has a `DeepClone()` method

### Code Generation Process (Execute)

Generate two types of files:

#### 1. DeepCloneExtensions.g.cs (Single File)

Contains all `_CloneInternal` extension methods:

```csharp
[EditorBrowsable(EditorBrowsableState.Never)]
internal static partial class DeepCloneExtensions
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    // internal if NeedsDeepCloneMethod, otherwise private
    internal static (Type) TypeName_CloneInternal(this (Type) original)
    {
        // Implementation depends on type:
        
        // For records/structs: Use with expression
        return original with {
            Prop1 = TypeName_CloneInternal(original.Prop1),
            Prop2 = TypeName_CloneInternal(original.Prop2),
            // ...
        };
        
        // For classes: Create new instance and copy properties
        var clone = new (Type)();
        clone.Prop1 = original.Prop1; // For value types or immutable types
        clone.Prop2 = TypeName_CloneInternal(original.Prop2); // For reference types
        // ...
        return clone;
    }
}
```

**Special Collection Type Handlers:**

Special types use the `SpecialTypeInfo` pattern:

```csharp
private static List<SpecialTypeInfo> SpecialTypeInfos = [ 
    new ListTypeInfo(), 
    new DictionaryTypeInfo(), 
    // ... 
];

// Usage:
foreach (var specialTypeInfo in SpecialTypeInfos)
{
    if (specialTypeInfo.IsMatch(classInfo.FullClassName))
    {
        // Generate using specialTypeInfo
        break;
    }
}
```

Example for List<T>:
```csharp
[MethodImpl(MethodImplOptions.AggressiveInlining)]
[EditorBrowsable(EditorBrowsableState.Never)]
private static List<ElementType> CloneList_ElementType(this List<ElementType> original)
{
    if (original == null) return null;
    var list = new List<ElementType>(original.Count);
    foreach (var item in original)
    {
        list.Add(DeepCloneExtensions.ElementType_CloneInternal(item));
    }
    return list;
}
```

#### 2. {FullClassName}.DeepClone.g.cs (Per Type)

For each type that needs a `DeepClone()` method:

```csharp
namespace (Namespace)
{
    partial (record/class/struct) (ClassName) : IDeepCloneable<(ClassName)>
    {
        /// <inheritdoc />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        // Add 'override' if base has DeepClone, 'virtual' if not sealed and no base, nothing if sealed
        public virtual (ClassName) DeepClone() => 
            DeepCloneExtensions.(ClassName)_CloneInternal(this);
    }
}
```

## Code Generation Rules (NOTICE)

**CRITICAL: These rules must be strictly followed:**

1. **Fully Qualified Names**: All generated and referenced type names MUST use fully qualified names starting with `global::` to avoid name collisions
   - Example: `global::System.Collections.Generic.List<T>` not `List<T>`
   - Example: `global::System.Runtime.CompilerServices.MethodImpl` not `MethodImpl`

2. **IndentedStringBuilder**: Code generation MUST use `IndentedStringBuilder` (not regular `StringBuilder`) for proper indentation
   - All code generation methods should accept `IndentedStringBuilder` as a parameter
   - Methods should operate on the builder and return it for efficiency

3. **File Header Format**: All generated files must use this header:
   ```csharp
   // <auto-generated>
   // This file was generated by the IDeepCloneable source generator.
   // </auto-generated>
   #nullable disable
   #pragma warning disable

   using System;
   using System.Collections.Generic;
   using System.ComponentModel;
   using System.Runtime.CompilerServices;
   ```

4. **Raw String Literals**: Use `$"""` format for multi-line string generation

5. **Method Attributes**: All generated methods must use fully qualified attribute names:
   - `[global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]`
   - `[global::System.ComponentModel.EditorBrowsable(global::System.ComponentModel.EditorBrowsableState.Never)]`

6. **Extension Methods**: Extension methods in `DeepCloneExtensions` class MUST be marked as `static`

## Special Type Handling

### SpecialTypeInfo Pattern

Each special collection type should inherit from the `SpecialTypeInfo` base class:

```csharp
internal abstract class SpecialTypeInfo
{
    public abstract string TargetTypeStartWith { get; }
    public virtual bool IsMatch(string typeFullName) => 
        typeFullName.StartsWith(TargetTypeStartWith, StringComparison.Ordinal);
    public abstract string GetMethodName(string typeFullName);
    public abstract IndentedStringBuilder GenerateCloneMethod(
        string typeFullName, 
        string methodName, 
        EquatableArray<ClassInfo> allClassInfos, 
        IndentedStringBuilder builder);
}
```

Each specialized handler (ListTypeInfo, DictionaryTypeInfo, etc.) implements:
- `TargetTypeStartWith`: Prefix to match (e.g., `"global::System.Collections.Generic.List<"`)
- `GetMethodName`: Returns the clone method name
- `GenerateCloneMethod`: Generates the complete clone method implementation

### Immutable Types

Types that don't need cloning (return value as-is):
- Primitives: `int`, `long`, `short`, `uint`, `ulong`, `ushort`, `byte`, `sbyte`, `bool`, `double`, `float`, `decimal`, `char`
- `string`
- `System.DateTime`, `System.DateTimeOffset`, `System.TimeSpan`, `System.Guid`
- Enums
- `System.Object` (cannot be cloned)

## Common Patterns

### Type Name Sanitization

Convert type names to valid identifiers:
```csharp
typeName
    .Replace("global::", "")
    .Replace("::", "_")
    .Replace(".", "_")
    .Replace("<", "_")
    .Replace(">", "_")
    .Replace(",", "_")
    .Replace(" ", "")
    .Replace("[]", "_Array")
    .Replace("?", "");
```

### Generic Type Extraction

Extract inner types from generic types while handling nested generics correctly:
- Track depth with `<` and `>` characters
- Split on `,` only at depth 0
- For `Dictionary<TKey, TValue>`, extract both key and value types

## Testing Requirements

1. **Build Verification**: Solution must build without errors
2. **Test Execution**: All tests must pass
3. **Generated Code Quality**: Generated code should be clean, efficient, and follow C# conventions
4. **Edge Cases**: Handle nullable types, record structs, sealed classes, abstract classes, inheritance hierarchies

## Performance Optimizations

1. Use `StringComparison.Ordinal` for string comparisons (culture-invariant)
2. Reuse utility methods to avoid code duplication
3. Use method chaining with `IndentedStringBuilder` for efficiency
4. Pre-allocate collections with known capacity where possible

## Extensibility

Adding new special collection types:
1. Create a new class inheriting from `SpecialTypeInfo`
2. Implement the required abstract members
3. Add instance to `SpecialTypeInfos` list in `CodeGenerator`
