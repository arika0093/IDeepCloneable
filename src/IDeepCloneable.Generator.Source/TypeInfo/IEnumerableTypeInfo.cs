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
        var innerType = CodeGenerationUtility.ExtractGenericType(typeFullName);
        
        // Extract type parameters from the inner type (e.g., "T" from "MyClass<T>")
        var typeParams = ExtractTypeParameters(innerType);
        var genericParams = typeParams.Count > 0 ? $"<{string.Join(", ", typeParams)}>" : string.Empty;
        
        builder.AppendLine("");
        builder.AppendLine(CodeTemplateContents.EditorBrowsableAttribute);
        builder.AppendLine(
            $"private static {typeFullName} {methodName}{genericParams}(this {typeFullName} original)"
        );
        builder.AppendLine("{");
        builder.IncreaseIndent();
        GenerateCloneMethodLogicPart(typeFullName, allClassInfos, builder, codeGenerator);
        builder.DecreaseIndent();
        builder.AppendLine("}");

        return builder;
    }
    
    /// <summary>
    /// Extracts type parameter names from a type (e.g., "T" from "MyClass<T>" or "TKey, TValue" from "MyClass<TKey, TValue>")
    /// </summary>
    private List<string> ExtractTypeParameters(string typeName)
    {
        var result = new List<string>();
        var startIndex = typeName.IndexOf('<');
        if (startIndex < 0)
            return result;
            
        var endIndex = typeName.LastIndexOf('>');
        if (endIndex <= startIndex)
            return result;
            
        var typeArgsString = typeName.Substring(startIndex + 1, endIndex - startIndex - 1);
        var typeArgs = CodeGenerationUtility.SplitGenericArgs(typeArgsString);
        
        foreach (var typeArg in typeArgs)
        {
            // Check if this is a simple type parameter (single identifier, not a qualified type)
            // Type parameters don't contain :: or . or spaces
            var trimmed = typeArg.Trim().Replace("global::", "");
            if (!trimmed.Contains("::") && !trimmed.Contains(".") && !trimmed.Contains(" ") && !trimmed.Contains("<"))
            {
                result.Add(typeArg.Trim());
            }
            else
            {
                // This is a complex type, recursively extract type parameters from it
                result.AddRange(ExtractTypeParameters(typeArg));
            }
        }
        
        return result;
    }

    public static IndentedStringBuilder GenerateCloneMethodLogicPart(
        string typeFullName,
        List<ClassInfo> allClassInfos,
        IndentedStringBuilder builder,
        CodeGenerator codeGenerator
    )
    {
        var innerType = CodeGenerationUtility.ExtractGenericType(typeFullName);
        var isImmutable = CodeGenerationUtility.IsTypeImmutable(innerType);
        builder.AppendLine("if (original == null) return null;");

        // For IEnumerable<T>, we materialize to List<T> to create a new collection
        // This is the most common scenario and provides a concrete implementation
        var listType = $"global::System.Collections.Generic.List<{innerType}>";

        if (isImmutable)
        {
            // For immutable element types, we can just create a new List from the original
            builder.AppendLine($"return new {listType}(original);");
        }
        else
        {
            // For mutable element types, we need to deep clone each element
            builder.AppendLine($"var list = new {listType}();");
            builder.AppendLine("foreach (var item in original)");
            builder.AppendLine("{");
            builder.IncreaseIndent();
            var cloneCall = codeGenerator.GenerateTypeCloneCall(innerType, "item", allClassInfos);
            builder.AppendLine($"list.Add({cloneCall});");
            builder.DecreaseIndent();
            builder.AppendLine("}");
            builder.AppendLine("return list;");
        }
        return builder;
    }
}
