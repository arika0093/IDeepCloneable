using Microsoft.CodeAnalysis;

namespace IDeepCloneable.Generator;

/// <summary>
/// Special type handler for Stack&lt;T&gt; collections.
/// </summary>
internal class StackTypeInfo : SpecialTypeInfo
{
    public override string TargetTypeStartWith => "global::System.Collections.Generic.Stack<";

    public override string GetMethodName(string typeFullName)
    {
        var innerType = CodeGenerationUtility.ExtractGenericType(typeFullName);
        return "CloneStack_" + CodeGenerationUtility.SanitizeTypeName(innerType);
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
            // Stack needs to preserve order, so we need to reverse or use an intermediate array
            builder.AppendLine($"            var array = original.ToArray();");
            builder.AppendLine($"            var stack = new {typeFullName}(array.Length);");
            builder.AppendLine("            for (int i = 0; i < array.Length; i++)");
            builder.AppendLine("            {");
            var cloneCall = CodeGenerator.GenerateTypeCloneCall(innerType, "array[i]", allClassInfos);
            builder.AppendLine($"                stack.Push({cloneCall});");
            builder.AppendLine("            }");
            builder.AppendLine("            return stack;");
        }

        builder.AppendLine("        }");

        return builder;
    }
}
