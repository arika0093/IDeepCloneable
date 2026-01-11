using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis;

namespace IDeepCloneable.Generator;

/// <summary>
/// Fallback handler for IEnumerable types that don't match other specific type handlers.
/// This provides basic cloning support for any IEnumerable implementation.
/// Checks both for IEnumerable&lt;T&gt; itself and types implementing IEnumerable&lt;T&gt;.
/// </summary>
internal class IEnumerableTypeInfo : SpecialTypeInfo
{
    public override string TargetTypeStartWith => "global::System.Collections.Generic.IEnumerable<";

    /// <summary>
    /// Checks if the type is IEnumerable&lt;T&gt;.
    /// This is a fallback handler for IEnumerable types not covered by specific handlers.
    /// Only matches exact IEnumerable&lt;T&gt; type, not types that implement it.
    /// </summary>
    public override bool IsMatch(string typeFullName)
    {
        // Only match exact IEnumerable<T>, not implementing types
        // This prevents matching List<T>, Dictionary<T>, etc. which are handled by specific handlers
        return typeFullName.StartsWith(TargetTypeStartWith, StringComparison.Ordinal);
    }

    public override string GetMethodName(string typeFullName)
    {
        return "CloneIEnumerable_" + CodeGenerationUtility.SanitizeTypeName(typeFullName);
    }

    public override IndentedStringBuilder GenerateCloneMethod(
        string typeFullName,
        string methodName,
        List<ClassInfo> allClassInfos,
        IndentedStringBuilder builder,
        CodeGenerator codeGenerator
    )
    {
        // Extract inner type from IEnumerable<T>
        var innerType = CodeGenerationUtility.ExtractGenericType(typeFullName);
        var isImmutable = CodeGenerationUtility.IsTypeImmutable(innerType);

        builder.AppendLine("");
        builder.AppendLine(CodeTemplateContents.EditorBrowsableAttribute);
        builder.AppendLine(
            $"private static {typeFullName} {methodName}(this {typeFullName} original)"
        );
        builder.AppendLine("{");
        builder.IncreaseIndent();
        builder.AppendLine("if (original == null) return null;");

        // For IEnumerable, convert to List<T> as we can't instantiate IEnumerable directly
        if (isImmutable)
        {
            builder.AppendLine(
                $"return new global::System.Collections.Generic.List<{innerType}>(original);"
            );
        }
        else
        {
            builder.AppendLine(
                $"var list = new global::System.Collections.Generic.List<{innerType}>();"
            );
            builder.AppendLine("foreach (var item in original)");
            builder.AppendLine("{");
            builder.IncreaseIndent();
            var cloneCall = codeGenerator.GenerateTypeCloneCall(innerType, "item", allClassInfos);
            builder.AppendLine($"list.Add({cloneCall});");
            builder.DecreaseIndent();
            builder.AppendLine("}");
            builder.AppendLine("return list;");
        }

        builder.DecreaseIndent();
        builder.AppendLine("}");

        return builder;
    }
}
