using Microsoft.CodeAnalysis;

namespace IDeepCloneable.Generator;

/// <summary>
/// Special type handler for List&lt;T&gt; collections.
/// </summary>
internal class ListTypeInfo : SpecialTypeInfo
{
    public override string TargetTypeStartWith => "global::System.Collections.Generic.List<";

    public override string GetMethodName(string typeFullName)
    {
        // Use the full type name for the method name, not just the inner type
        return "CloneList_" + CodeGenerationUtility.SanitizeTypeName(typeFullName);
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
        builder.AppendLine(CodeTemplateContents.AggressiveInliningAttribute);
        builder.AppendLine(CodeTemplateContents.EditorBrowsableAttribute);
        builder.AppendLine(
            $"private static {typeFullName} {methodName}(this {typeFullName} original)"
        );
        builder.AppendLine("{");
        builder.IncreaseIndent();
        builder.AppendLine("if (original == null) return null;");

        if (isImmutable)
        {
            builder.AppendLine($"return new {typeFullName}(original);");
        }
        else
        {
            builder.AppendLine($"var list = new {typeFullName}(original.Count);");
            builder.AppendLine("foreach (var item in original)");
            builder.AppendLine("{");
            builder.IncreaseIndent();
            var cloneCall = CodeGenerator.GenerateTypeCloneCall(innerType, "item", allClassInfos);
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
