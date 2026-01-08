using Microsoft.CodeAnalysis;

namespace IDeepCloneable.Generator;

/// <summary>
/// Special type handler for ReadOnlyCollection&lt;T&gt; collections.
/// </summary>
internal class ReadOnlyCollectionTypeInfo : SpecialTypeInfo
{
    public override string TargetTypeStartWith => "global::System.Collections.ObjectModel.ReadOnlyCollection<";

    public override string GetMethodName(string typeFullName)
    {
        return "CloneReadOnlyCollection_" + CodeGenerationUtility.SanitizeTypeName(typeFullName);
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
            $"[global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]"
        );
        builder.AppendLine(
            $"[global::System.ComponentModel.EditorBrowsable(global::System.ComponentModel.EditorBrowsableState.Never)]"
        );
        builder.AppendLine(
            $"private static {typeFullName} {methodName}(this {typeFullName} original)"
        );
        builder.AppendLine("{");
        builder = builder.IncreaseIndent();
        builder.AppendLine("if (original == null) return null;");

        if (isImmutable)
        {
            // ReadOnlyCollection wraps a List, but we can just create a new one with the same items
            builder.AppendLine($"return new {typeFullName}(new global::System.Collections.Generic.List<{innerType}>(original));");
        }
        else
        {
            builder.AppendLine($"var list = new global::System.Collections.Generic.List<{innerType}>(original.Count);");
            builder.AppendLine("foreach (var item in original)");
            builder.AppendLine("{");
            builder = builder.IncreaseIndent();
            var cloneCall = CodeGenerator.GenerateTypeCloneCall(innerType, "item", allClassInfos);
            builder.AppendLine($"list.Add({cloneCall});");
            builder = builder.DecreaseIndent();
            builder.AppendLine("}");
            builder.AppendLine($"return new {typeFullName}(list);");
        }

        builder = builder.DecreaseIndent();
        builder.AppendLine("}");

        return builder;
    }
}
