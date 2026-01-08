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
        var innerType = CodeGenerationUtility.ExtractGenericType(typeFullName);
        return "CloneList_" + CodeGenerationUtility.SanitizeTypeName(innerType);
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
            $"        [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]"
        );
        builder.AppendLine(
            $"        [global::System.ComponentModel.EditorBrowsable(global::System.ComponentModel.EditorBrowsableState.Never)]"
        );
        builder.AppendLine(
            $"        private static {typeFullName} {methodName}(this {typeFullName} original)"
        );
        builder.AppendLine("        {");
        builder.AppendLine("            if (original == null) return null;");

        if (isImmutable)
        {
            builder.AppendLine($"            return new {typeFullName}(original);");
        }
        else
        {
            builder.AppendLine($"            var list = new {typeFullName}(original.Count);");
            builder.AppendLine("            foreach (var item in original)");
            builder.AppendLine("            {");
            var cloneCall = CodeGenerator.GenerateTypeCloneCall(innerType, "item", allClassInfos);
            builder.AppendLine($"                list.Add({cloneCall});");
            builder.AppendLine("            }");
            builder.AppendLine("            return list;");
        }

        builder.AppendLine("        }");

        return builder;
    }
}
