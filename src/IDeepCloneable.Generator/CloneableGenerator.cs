using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace IDeepCloneable.Generator;

/// <summary>
/// Incremental source generator for automatic DeepClone implementation.
/// Generates DeepClone methods for types marked with [DeepCloneable] attribute.
/// </summary>
[Generator]
public partial class CloneableGenerator : IIncrementalGenerator
{
    private const string DeepCloneMethodName = "DeepClone";
    private const string DeepCloneableAttributeMetadataName = "DeepCloneableAttribute";
    private const string DeepCloneableAttributeFullName = "global::DeepCloneableAttribute";

    /// <summary>
    /// Initializes the incremental generator.
    /// Sets up the pipeline to process types with [DeepCloneable] attribute.
    /// </summary>
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var classDeclarations = context
            .SyntaxProvider.ForAttributeWithMetadataName(
                DeepCloneableAttributeMetadataName,
                predicate: static (node, _) => true,
                transform: static (ctx, _) => GetRelationalAllClassInfo(ctx)
            )
            .Where(static m => m.HasValue)
            .Select(static (m, _) => m!.Value);

        context.RegisterSourceOutput(
            classDeclarations,
            static (spc, source) => Execute(source, spc)
        );
    }

    // Implementation is in CloneableGenerator.Impl.cs and CloneableGenerator.Execute.cs
}
