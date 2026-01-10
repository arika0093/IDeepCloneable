using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;

namespace IDeepCloneable.Generator;

/// <summary>
/// Base class for incremental source generators for automatic DeepClone implementation.
/// </summary>
public abstract class CloneableGeneratorCore<TOptions>() : IIncrementalGenerator
    where TOptions : CloneableGeneratorOptionsCore, new()
{
    /// <summary>
    /// Generator options instance.
    /// </summary>
    private static readonly TOptions options = new();

    /// <summary>
    /// Type analyzer instance.
    /// </summary>
    private static readonly TypeAnalyzer _typeAnalyzer = new(options);

    /// <summary>
    /// Code generator instance.
    /// </summary>
    private static readonly CodeGenerator _codeGenerator = new(options);

    /// <summary>
    /// Initializes the incremental generator.
    /// </summary>
    public virtual void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var classDeclarations = context
            .SyntaxProvider.ForAttributeWithMetadataName(
                options.AttributeMetadataName,
                predicate: static (node, _) => true,
                transform: TransformFunc
            )
            .Where(static m => m.HasValue)
            .SelectMany(static (m, _) => m!.Value)
            .Collect();

        context.RegisterSourceOutput(
            classDeclarations,
            (spc, sources) => Execute(DropDuplicates(sources), spc)
        );
    }

    /// <summary>
    /// Transforms the syntax context into class information.
    /// </summary>
    protected virtual EquatableArray<ClassInfo>? TransformFunc(
        GeneratorAttributeSyntaxContext ctx,
        CancellationToken cancellationToken
    ) => _typeAnalyzer.GetRelationalAllClassInfo(ctx);

    /// <summary>
    /// Executes the code generation process.
    /// </summary>
    protected virtual void Execute(
        List<ClassInfo> allClassInfos,
        SourceProductionContext context
    ) => _codeGenerator.Execute(allClassInfos, context);

    /// <summary>
    /// Drops duplicate ClassInfo entries based on FullClassName.
    /// </summary>
    protected List<ClassInfo> DropDuplicates(ImmutableArray<ClassInfo> classInfos)
    {
        var result = new List<ClassInfo>();
        var seenTypes = new HashSet<string>();
        foreach (var classInfo in classInfos)
        {
            if (!seenTypes.Contains(classInfo.FullClassName))
            {
                seenTypes.Add(classInfo.FullClassName);
                result.Add(classInfo);
            }
        }
        return result;
    }
}
