using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace IDeepCloneable.Generator;

/// <summary>
/// Special type handler for IEnumerable&lt;T&gt; collections.
/// Clones IEnumerable&lt;T&gt; by materializing to List&lt;T&gt; with deep cloned elements.
/// </summary>
internal class IEnumerableTypeInfo : SpecialTypeInfo
{
    public override string TargetTypeStartWith => "global::System.Collections.Generic.IEnumerable<";

    public override bool IsMatch(string typeFullName)
    {
        // TODO:
        // Modify IsMatch to accept not only the fullname but also a list of implemented interfaces,
        // and check whether the type implements IEnumerable<T>.
        return base.IsMatch(typeFullName);
    }

    public override string GetMethodName(string typeFullName)
    {
        // Use the full type name for the method name, not just the inner type
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
        builder.AppendLine("");
        builder.AppendLine(CodeTemplateContents.EditorBrowsableAttribute);
        builder.AppendLine(
            $"private static {typeFullName} {methodName}(this {typeFullName} original)"
        );
        builder.AppendLine("{");
        builder.IncreaseIndent();
        ListTypeInfo.GenerateCloneMethodLogicPart(
            typeFullName,
            allClassInfos,
            builder,
            codeGenerator
        );
        builder.AppendLine("return list;");
        builder.DecreaseIndent();
        builder.AppendLine("}");

        return builder;
    }
}
