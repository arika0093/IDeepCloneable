using Microsoft.CodeAnalysis;

namespace IDeepCloneable.Generator;

/// <summary>
/// Special type handler for ObservableCollection&lt;T&gt; collections.
/// </summary>
internal class ObservableCollectionTypeInfo : SpecialTypeInfo
{
    public override string TargetTypeStartWith =>
        "global::System.Collections.ObjectModel.ObservableCollection<";

    public override string GetMethodName(string typeFullName)
    {
        return "CloneObservableCollection_" + CodeGenerationUtility.SanitizeTypeName(typeFullName);
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

        if (isImmutable)
        {
            builder.AppendLine($"return new {typeFullName}(original);");
        }
        else
        {
            builder.AppendLine($"var collection = new {typeFullName}();");
            builder.AppendLine("foreach (var item in original)");
            builder.AppendLine("{");
            builder.IncreaseIndent();
            var cloneCall = CodeGenerator.GenerateTypeCloneCall(innerType, "item", allClassInfos);
            builder.AppendLine($"collection.Add({cloneCall});");
            builder.DecreaseIndent();
            builder.AppendLine("}");
            builder.AppendLine("return collection;");
        }

        builder.DecreaseIndent();
        builder.AppendLine("}");

        return builder;
    }
}
