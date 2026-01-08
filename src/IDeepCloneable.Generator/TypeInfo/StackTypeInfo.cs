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
        return "CloneStack_" + CodeGenerationUtility.SanitizeTypeName(typeFullName);
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
        builder = builder.IncreaseIndent();
        builder.AppendLine("if (original == null) return null;");

        // Stack needs to preserve order, so we use ToArray() then pass to constructor
        // ToArray() returns items in the order they would be popped (LIFO)
        // The Stack constructor pushes items in the order they appear in the enumerable
        // So we need to reverse to maintain the original order
        builder.AppendLine($"var array = original.ToArray();");
        builder.AppendLine($"global::System.Array.Reverse(array);");
        
        if (isImmutable)
        {
            builder.AppendLine($"return new {typeFullName}(array);");
        }
        else
        {
            builder.AppendLine($"var stack = new {typeFullName}(array.Length);");
            builder.AppendLine("for (int i = 0; i < array.Length; i++)");
            builder.AppendLine("{");
            builder = builder.IncreaseIndent();
            var cloneCall = CodeGenerator.GenerateTypeCloneCall(innerType, "array[i]", allClassInfos);
            builder.AppendLine($"stack.Push({cloneCall});");
            builder = builder.DecreaseIndent();
            builder.AppendLine("}");
            builder.AppendLine("return stack;");
        }

        builder = builder.DecreaseIndent();
        builder.AppendLine("}");

        return builder;
    }
}
