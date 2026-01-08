using Microsoft.CodeAnalysis;

namespace IDeepCloneable.Generator;

/// <summary>
/// Special type handler for ImmutableArray&lt;T&gt; collections.
/// </summary>
internal class ImmutableArrayTypeInfo : SpecialTypeInfo
{
    public override string TargetTypeStartWith => "global::System.Collections.Immutable.ImmutableArray<";

    public override string GetMethodName(string typeFullName)
    {
        var innerType = CodeGenerationUtility.ExtractGenericType(typeFullName);
        return "CloneImmutableArray_" + CodeGenerationUtility.SanitizeTypeName(innerType);
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
        builder.AppendLine("            if (original.IsDefault) return original;");

        if (isImmutable)
        {
            // ImmutableArray is a struct, and if elements are immutable, we can return the same instance
            builder.AppendLine("            return original;");
        }
        else
        {
            builder.AppendLine($"            var builder = global::System.Collections.Immutable.ImmutableArray.CreateBuilder<{innerType}>(original.Length);");
            builder.AppendLine("            for (int i = 0; i < original.Length; i++)");
            builder.AppendLine("            {");
            var cloneCall = CodeGenerator.GenerateTypeCloneCall(innerType, "original[i]", allClassInfos);
            builder.AppendLine($"                builder.Add({cloneCall});");
            builder.AppendLine("            }");
            builder.AppendLine("            return builder.ToImmutable();");
        }

        builder.AppendLine("        }");

        return builder;
    }
}
