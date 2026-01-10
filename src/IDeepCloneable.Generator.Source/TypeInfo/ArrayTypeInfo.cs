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

        // For immutable element types or value types, use optimized AsSpan().ToArray() for better performance
        // Note: AsSpan() only works for single-dimensional arrays
        if (isImmutable)
        {
            if (typeFullName.Contains(","))
            {
                // Multi-dimensional arrays - use Array.Clone()
                builder.AppendLine($"return (original.Clone() as {typeFullName});");
            }
            else
            {
                // Single-dimensional arrays - use AsSpan().ToArray() for fast cloning
                builder.AppendLine($"return original.AsSpan().ToArray();");
            }
        }
        else
        {
            // For mutable element types, we need to deep clone each element
            if (typeFullName.Contains(","))
            {
                // Multi-dimensional array with mutable elements
                // TODO: Implement proper deep cloning for multi-dimensional arrays with mutable elements
                // For now, this limitation is documented - multi-dimensional arrays with mutable elements
                // will be shallow copied. This is an edge case that can be improved in the future.
                builder.AppendLine(
                    $"// WARNING: Multi-dimensional arrays with mutable elements are shallow copied"
                );
                builder.AppendLine($"return (original.Clone() as {typeFullName});");
            }
            else
            {
                // Single-dimensional array
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
        }

        builder.DecreaseIndent();
        builder.AppendLine("}");

        return builder;
    }
}
