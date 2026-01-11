using System.Collections.Generic;
using Microsoft.CodeAnalysis;

namespace IDeepCloneable.Generator;

/// <summary>
/// Special type handler for arrays (both single and multi-dimensional).
/// </summary>
internal class ArrayTypeInfo : SpecialTypeInfo
{
    public override string TargetTypeStartWith => ""; // Arrays don't have a fixed prefix

    public override bool IsMatch(string typeFullName)
    {
        // Match if the type contains array brackets
        return typeFullName.Contains("[") && typeFullName.Contains("]");
    }

    public override string GetMethodName(string typeFullName)
    {
        return "CloneArray_" + CodeGenerationUtility.SanitizeTypeName(typeFullName);
    }

    public override IndentedStringBuilder GenerateCloneMethod(
        string typeFullName,
        string methodName,
        List<ClassInfo> allClassInfos,
        IndentedStringBuilder builder,
        CodeGenerator codeGenerator
    )
    {
        // Check if this is a multi-dimensional array
        if (typeFullName.Contains(","))
        {
            return GenerateMultiDimensionalArrayCloneMethod(
                typeFullName,
                methodName,
                allClassInfos,
                builder,
                codeGenerator
            );
        }
        else
        {
            return GenerateSingleDimensionalArrayCloneMethod(
                typeFullName,
                methodName,
                allClassInfos,
                builder,
                codeGenerator
            );
        }
    }

    /// <summary>
    /// Generates clone method for single-dimensional arrays.
    /// Uses AsSpan().ToArray() for immutable elements, deep clones mutable elements.
    /// </summary>
    private IndentedStringBuilder GenerateSingleDimensionalArrayCloneMethod(
        string typeFullName,
        string methodName,
        List<ClassInfo> allClassInfos,
        IndentedStringBuilder builder,
        CodeGenerator codeGenerator
    )
    {
        // Extract element type (everything before the first '[')
        var bracketIndex = typeFullName.IndexOf('[');
        var elementType = typeFullName.Substring(0, bracketIndex);
        var isImmutable = CodeGenerationUtility.IsTypeImmutable(elementType);

        builder.AppendLine("");
        builder.AppendLine($"{CodeTemplateContents.EditorBrowsableAttribute}");
        builder.AppendLine(
            $"private static {typeFullName} {methodName}(this {typeFullName} original)"
        );
        builder.AppendLine("{");
        builder.IncreaseIndent();
        builder.AppendLine("if (original == null) return null;");

        if (isImmutable)
        {
            // Use AsSpan().ToArray() for fast cloning of immutable/primitive elements
            builder.AppendLine($"return original.AsSpan().ToArray();");
        }
        else
        {
            // Deep clone each mutable element
            builder.AppendLine($"var array = new {elementType}[original.Length];");
            builder.AppendLine("for (int i = 0; i < original.Length; i++)");
            builder.AppendLine("{");
            builder.IncreaseIndent();
            var cloneCall = codeGenerator.GenerateTypeCloneCall(
                elementType,
                "original[i]",
                allClassInfos
            );
            builder.AppendLine($"array[i] = {cloneCall};");
            builder.DecreaseIndent();
            builder.AppendLine("}");
            builder.AppendLine("return array;");
        }

        builder.DecreaseIndent();
        builder.AppendLine("}");

        return builder;
    }

    /// <summary>
    /// Generates clone method for multi-dimensional arrays.
    /// Uses Array.Clone() for all cases as deep cloning multi-dimensional mutable arrays is complex.
    /// </summary>
    private IndentedStringBuilder GenerateMultiDimensionalArrayCloneMethod(
        string typeFullName,
        string methodName,
        List<ClassInfo> allClassInfos,
        IndentedStringBuilder builder,
        CodeGenerator codeGenerator
    )
    {
        // Extract element type (everything before the first '[')
        var bracketIndex = typeFullName.IndexOf('[');
        var elementType = typeFullName.Substring(0, bracketIndex);
        var isImmutable = CodeGenerationUtility.IsTypeImmutable(elementType);

        builder.AppendLine("");
        builder.AppendLine($"{CodeTemplateContents.EditorBrowsableAttribute}");
        builder.AppendLine(
            $"private static {typeFullName} {methodName}(this {typeFullName} original)"
        );
        builder.AppendLine("{");
        builder.IncreaseIndent();
        builder.AppendLine("if (original == null) return null;");

        if (isImmutable)
        {
            // For immutable elements, Array.Clone() is sufficient
            builder.AppendLine($"return (original.Clone() as {typeFullName});");
        }
        else
        {
            // Multi-dimensional arrays with mutable elements
            // TODO: Implement proper deep cloning for multi-dimensional arrays with mutable elements
            // For now, shallow copy with warning
            builder.AppendLine(
                $"// WARNING: Multi-dimensional arrays with mutable elements are shallow copied"
            );
            builder.AppendLine($"return (original.Clone() as {typeFullName});");
        }

        builder.DecreaseIndent();
        builder.AppendLine("}");

        return builder;
    }
}
