using System.Collections.Generic;
using Microsoft.CodeAnalysis;

namespace IDeepCloneable.Generator;

/// <summary>
/// Special type handler for List&lt;T&gt; collections.
/// </summary>
internal class ListTypeInfo : SpecialTypeInfo
{
    private const string ListFullName = "global::System.Collections.Generic.List";
    public override string TargetTypeStartWith => "global::System.Collections.Generic.List<";

    public override string GetMethodName(string typeFullName)
    {
        // Use the full type name for the method name, not just the inner type
        return "CloneList_" + CodeGenerationUtility.SanitizeTypeName(typeFullName);
    }

    public override IndentedStringBuilder GenerateCloneMethod(
        string typeFullName,
        string methodName,
        List<ClassInfo> allClassInfos,
        IndentedStringBuilder builder,
        CodeGenerator codeGenerator
    )
    {
        builder.AppendLine("");
        builder.AppendLine(CodeTemplateContents.EditorBrowsableAttribute);
        builder.AppendLine(
            $"private static {typeFullName} {methodName}(this {typeFullName} original)"
        );
        builder.AppendLine("{");
        builder.IncreaseIndent();
        GenerateCloneMethodLogicPart(typeFullName, allClassInfos, builder, codeGenerator);
        builder.AppendLine("return list;");
        builder.DecreaseIndent();
        builder.AppendLine("}");

        return builder;
    }

    public static IndentedStringBuilder GenerateCloneMethodLogicPart(
        string typeFullName,
        List<ClassInfo> allClassInfos,
        IndentedStringBuilder builder,
        CodeGenerator codeGenerator
    )
    {
        var innerType = CodeGenerationUtility.ExtractGenericType(typeFullName);
        var isImmutable = CodeGenerationUtility.IsTypeImmutable(innerType);
        builder.AppendLine("if (original == null) return null;");

        if (isImmutable)
        {
            builder.AppendLine($"var list = new {ListFullName}<{innerType}>(original);");
        }
        else
        {
            builder.AppendLine($"var list = new {ListFullName}<{innerType}>(original.Count);");
            builder.AppendLine("foreach (var item in original)");
            builder.AppendLine("{");
            builder.IncreaseIndent();
            var cloneCall = codeGenerator.GenerateTypeCloneCall(innerType, "item", allClassInfos);
            builder.AppendLine($"list.Add({cloneCall});");
            builder.DecreaseIndent();
            builder.AppendLine("}");
        }
        return builder;
    }
}
