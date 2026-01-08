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
        IndentedStringBuilder builder)
    {
        var innerType = CodeGenerationUtility.ExtractGenericType(typeFullName);
        var isImmutable = CodeGenerationUtility.IsTypeImmutable(innerType);

        builder.Append("");
        builder.Append($"        [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]");
        builder.Append($"        [global::System.ComponentModel.EditorBrowsable(global::System.ComponentModel.EditorBrowsableState.Never)]");
        builder.Append($"        private static {typeFullName} {methodName}(this {typeFullName} original)");
        builder.Append("        {");
        builder.Append("            if (original == null) return null;");
        
        if (isImmutable)
        {
            builder.Append($"            return new {typeFullName}(original);");
        }
        else
        {
            builder.Append($"            var list = new {typeFullName}(original.Count);");
            builder.Append("            foreach (var item in original)");
            builder.Append("            {");
            var cloneCall = CodeGenerator.GenerateTypeCloneCall(innerType, "item", allClassInfos);
            builder.Append($"                list.Add({cloneCall});");
            builder.Append("            }");
            builder.Append("            return list;");
        }
        
        builder.Append("        }");
        
        return builder;
    }
}
