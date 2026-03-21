using System.Collections.Generic;
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
        List<ClassInfo> allClassInfos,
        IndentedStringBuilder builder,
        CodeGenerator codeGenerator
    )
    {
        var genericParams = CodeGenerationUtility.BuildGenericTypeParameterList(typeFullName);
        var innerType = CodeGenerationUtility.ExtractGenericType(typeFullName);
        var isImmutable = CodeGenerationUtility.IsTypeImmutable(innerType);

        builder.AppendLine("");
        AppendCloneMethodStart(builder, typeFullName, methodName, genericParams);
        builder.AppendLine(
            """
            if (original == null) return null;
            var array = original.ToArray();
            global::System.Array.Reverse(array);
            """
        );

        if (isImmutable)
        {
            builder.AppendLine($"return new {typeFullName}(array);");
        }
        else
        {
            builder.AppendLine(
                $$"""
                var stack = new {{typeFullName}}(array.Length);
                for (int i = 0; i < array.Length; i++)
                {
                """
            );
            builder.IncreaseIndent();
            var cloneCall = codeGenerator.GenerateTypeCloneCall(
                innerType,
                "array[i]",
                allClassInfos
            );
            builder.AppendLine($"stack.Push({cloneCall});");
            builder.DecreaseIndent();
            builder.AppendLine("}");
            builder.AppendLine("return stack;");
        }

        builder.DecreaseIndent();
        builder.AppendLine("}");

        return builder;
    }
}
