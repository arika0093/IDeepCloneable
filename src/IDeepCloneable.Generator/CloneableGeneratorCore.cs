using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace IDeepCloneable.Generator;

/// <summary>
/// Base class for incremental source generators for automatic DeepClone implementation.
/// </summary>
public abstract class CloneableGeneratorCore<TOptions>() : IIncrementalGenerator
    where TOptions : ICloneableGeneratorOptions, new()
{
    private static readonly TOptions options = new();
    private static readonly TypeAnalyzer _typeAnalyzer = new(options);
    private static readonly CodeGenerator _codeGenerator = new(options);

    public virtual void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var classDeclarations = context
            .SyntaxProvider.ForAttributeWithMetadataName(
                options.AttributeMetadataName,
                predicate: static (node, _) => true,
                transform: (ctx, _) => _typeAnalyzer.GetRelationalAllClassInfo(ctx)
            )
            .Where(static m => m.HasValue)
            .SelectMany(static (m, _) => m!.Value)
            .Collect();

        context.RegisterSourceOutput(
            classDeclarations,
            (spc, sources) => ExecuteForAll(sources, spc)
        );
    }

    private void ExecuteForAll(
        ImmutableArray<ClassInfo> allClassInfoArrays,
        SourceProductionContext context
    )
    {
        var allClassInfos = new List<ClassInfo>();
        var seenTypes = new HashSet<string>();

        foreach (var classInfo in allClassInfoArrays)
        {
            if (!seenTypes.Contains(classInfo.FullClassName))
            {
                seenTypes.Add(classInfo.FullClassName);
                allClassInfos.Add(classInfo);
            }
        }

        _codeGenerator.Execute(allClassInfos, context);
    }
}
