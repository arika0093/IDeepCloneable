using System.Collections.Generic;
using System.Linq;
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
    /// Deep clones each element for mutable types, Array.Clone for immutable types.
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
            // Multi-dimensional arrays with mutable elements - deep clone each element
            // Extract dimensions from type (e.g., [,] is 2D, [,,] is 3D)
            var dimensionCount = typeFullName.Where(c => c == ',').Count() + 1;

            // Create new array with same dimensions
            builder.AppendLine($"var lengths = new int[{dimensionCount}];");
            for (int i = 0; i < dimensionCount; i++)
            {
                builder.AppendLine($"lengths[{i}] = original.GetLength({i});");
            }
            builder.AppendLine(
                $"var clone = global::System.Array.CreateInstance(typeof({elementType}), lengths) as {typeFullName};"
            );

            // Walk through all elements using indices
            builder.AppendLine($"var indices = new int[{dimensionCount}];");
            builder.AppendLine($"var totalElements = 1;");
            for (int i = 0; i < dimensionCount; i++)
            {
                builder.AppendLine($"totalElements *= lengths[{i}];");
            }
            builder.AppendLine("for (int i = 0; i < totalElements; i++)");
            builder.AppendLine("{");
            builder.IncreaseIndent();

            // Calculate multi-dimensional indices from flat index
            builder.AppendLine("var temp = i;");
            for (int dim = dimensionCount - 1; dim >= 0; dim--)
            {
                builder.AppendLine($"indices[{dim}] = temp % lengths[{dim}];");
                if (dim > 0)
                {
                    builder.AppendLine($"temp /= lengths[{dim}];");
                }
            }

            // Clone the element
            var cloneCall = codeGenerator.GenerateTypeCloneCall(
                elementType,
                "original.GetValue(indices)",
                allClassInfos
            );
            builder.AppendLine($"clone.SetValue({cloneCall}, indices);");

            builder.DecreaseIndent();
            builder.AppendLine("}");
            builder.AppendLine("return clone;");
        }

        builder.DecreaseIndent();
        builder.AppendLine("}");

        return builder;
    }
}
