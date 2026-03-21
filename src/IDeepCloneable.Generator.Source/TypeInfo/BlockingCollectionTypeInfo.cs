using System.Collections.Generic;
using Microsoft.CodeAnalysis;

namespace IDeepCloneable.Generator;

/// <summary>
/// Special type handler for BlockingCollection&lt;T&gt; collections.
/// </summary>
internal class BlockingCollectionTypeInfo : SpecialTypeInfo
{
    public override string TargetTypeStartWith =>
        "global::System.Collections.Concurrent.BlockingCollection<";

    public override string GetMethodName(string typeFullName)
    {
        return "CloneBlockingCollection_" + CodeGenerationUtility.SanitizeTypeName(typeFullName);
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
        builder.AppendLine("if (original == null) return null;");

        if (isImmutable)
        {
            builder.AppendLine("var items = original.ToArray();");
            builder.AppendLine(
                $"var queue = new global::System.Collections.Concurrent.ConcurrentQueue<{innerType}>(items);"
            );
        }
        else
        {
            builder.AppendLine(
                $$"""
                var items = original.ToArray();
                var queue = new global::System.Collections.Concurrent.ConcurrentQueue<{{innerType}}>();
                foreach (var item in items)
                {
                """
            );
            builder.IncreaseIndent();
            var cloneCall = codeGenerator.GenerateTypeCloneCall(innerType, "item", allClassInfos);
            builder.AppendLine($"queue.Enqueue({cloneCall});");
            builder.DecreaseIndent();
            builder.AppendLine("}");
        }

        builder.AppendLine(
            $$"""
            var boundedCapacity = original.BoundedCapacity;
            var clone = boundedCapacity > 0 ? new {{typeFullName}}(queue, boundedCapacity) : new {{typeFullName}}(queue);
            if (original.IsAddingCompleted) clone.CompleteAdding();
            return clone;
            """
        );
        builder.DecreaseIndent();
        builder.AppendLine("}");

        return builder;
    }
}
