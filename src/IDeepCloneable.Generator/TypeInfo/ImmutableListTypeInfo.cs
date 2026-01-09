using Microsoft.CodeAnalysis;

namespace IDeepCloneable.Generator;

/// <summary>
/// Special type handler for ImmutableList&lt;T&gt; collections.
/// </summary>
internal class ImmutableListTypeInfo : SpecialTypeInfo
{
    public override string TargetTypeStartWith => "global::System.Collections.Immutable.ImmutableList<";

    public override string GetMethodName(string typeFullName)
    {
        return "CloneImmutableList_" + CodeGenerationUtility.SanitizeTypeName(typeFullName);
    }

    public override IndentedStringBuilder GenerateCloneMethod(
        string typeFullName,
        string methodName,
        EquatableArray<ClassInfo> allClassInfos,
        IndentedStringBuilder builder
    )
    {
        var innerType = CodeGenerationUtility.ExtractGenericType(typeFullName);
        var isImmutable = CodeGenerationUtility.IsTypeImmutable(innerType);

        builder.AppendLine("");
        builder.AppendLine(
            CodeTemplateContents.AggressiveInliningAttribute
        );
        builder.AppendLine(
            CodeTemplateContents.EditorBrowsableAttribute
        );
        builder.AppendLine(
            $"private static {typeFullName} {methodName}(this {typeFullName} original)"
        );
        builder.AppendLine("{");
        builder.IncreaseIndent();
        builder.AppendLine("if (original == null) return null;");

        if (isImmutable)
        {
            // ImmutableList is immutable, and if elements are immutable too, we can return the same instance
            builder.AppendLine("return original;");
        }
        else
        {
            builder.AppendLine($"var builder = global::System.Collections.Immutable.ImmutableList.CreateBuilder<{innerType}>();");
            builder.AppendLine("foreach (var item in original)");
            builder.AppendLine("{");
            builder.IncreaseIndent();
            var cloneCall = CodeGenerator.GenerateTypeCloneCall(innerType, "item", allClassInfos);
            builder.AppendLine($"builder.Add({cloneCall});");
            builder.DecreaseIndent();
            builder.AppendLine("}");
            builder.AppendLine("return builder.ToImmutable();");
        }

        builder.DecreaseIndent();
        builder.AppendLine("}");

        return builder;
    }
}
