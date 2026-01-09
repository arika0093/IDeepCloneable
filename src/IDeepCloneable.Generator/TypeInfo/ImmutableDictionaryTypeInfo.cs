using Microsoft.CodeAnalysis;

namespace IDeepCloneable.Generator;

/// <summary>
/// Special type handler for ImmutableDictionary&lt;TKey, TValue&gt; collections.
/// </summary>
internal class ImmutableDictionaryTypeInfo : SpecialTypeInfo
{
    public override string TargetTypeStartWith =>
        "global::System.Collections.Immutable.ImmutableDictionary<";

    public override string GetMethodName(string typeFullName)
    {
        return "CloneImmutableDictionary_" + CodeGenerationUtility.SanitizeTypeName(typeFullName);
    }

    public override IndentedStringBuilder GenerateCloneMethod(
        string typeFullName,
        string methodName,
        EquatableArray<ClassInfo> allClassInfos,
        IndentedStringBuilder builder
    )
    {
        var genericArgs = CodeGenerationUtility.ExtractGenericType(typeFullName);
        var parts = CodeGenerationUtility.SplitGenericArgs(genericArgs);

        if (parts.Count != 2)
            return builder;

        var keyType = parts[0];
        var valueType = parts[1];

        var keyIsImmutable = CodeGenerationUtility.IsTypeImmutable(keyType);
        var valueIsImmutable = CodeGenerationUtility.IsTypeImmutable(valueType);

        builder.AppendLine("");
        builder.AppendLine(CodeTemplateContents.AggressiveInliningAttribute);
        builder.AppendLine(CodeTemplateContents.EditorBrowsableAttribute);
        builder.AppendLine(
            $"private static {typeFullName} {methodName}(this {typeFullName} original)"
        );
        builder.AppendLine("{");
        builder.IncreaseIndent();

        if (keyIsImmutable && valueIsImmutable)
        {
            // ImmutableDictionary is immutable, and if keys and values are immutable too, we can return the same instance
            builder.AppendLine("return original;");
        }
        else
        {
            builder.AppendLine(
                $"var builder = global::System.Collections.Immutable.ImmutableDictionary.CreateBuilder<{keyType}, {valueType}>();"
            );
            builder.AppendLine("foreach (var kvp in original)");
            builder.AppendLine("{");
            builder.IncreaseIndent();

            var keyClone = keyIsImmutable
                ? "kvp.Key"
                : CodeGenerator.GenerateTypeCloneCall(keyType, "kvp.Key", allClassInfos);
            var valueClone = valueIsImmutable
                ? "kvp.Value"
                : CodeGenerator.GenerateTypeCloneCall(valueType, "kvp.Value", allClassInfos);

            builder.AppendLine($"builder.Add({keyClone}, {valueClone});");
            builder.DecreaseIndent();
            builder.AppendLine("}");
            builder.AppendLine("return builder.ToImmutable();");
        }

        builder.DecreaseIndent();
        builder.AppendLine("}");

        return builder;
    }
}
