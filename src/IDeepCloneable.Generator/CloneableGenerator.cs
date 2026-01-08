using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace IDeepCloneable.Generator;

/// <summary>
/// Incremental source generator for automatic DeepClone implementation.
/// Generates DeepClone methods for types marked with [DeepCloneable] attribute.
/// </summary>
[Generator]
public class CloneableGenerator : IIncrementalGenerator
{
    private const string DeepCloneableAttributeMetadataName = "DeepCloneableAttribute";

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var classDeclarations = context
            .SyntaxProvider.ForAttributeWithMetadataName(
                DeepCloneableAttributeMetadataName,
                predicate: static (node, _) => true,
                transform: static (ctx, _) => TypeAnalyzer.GetRelationalAllClassInfo(ctx)
            )
            .Where(static m => m.HasValue)
            .Select(static (m, _) => m!.Value)
            .Collect();

        context.RegisterSourceOutput(
            classDeclarations,
            static (spc, sources) => ExecuteForAll(sources, spc)
        );
    }
    
    private static void ExecuteForAll(ImmutableArray<EquatableArray<ClassInfo>> allClassInfoArrays, SourceProductionContext context)
    {
        var allClassInfos = new List<ClassInfo>();
        var seenTypes = new HashSet<string>();
        
        foreach (var classInfoArray in allClassInfoArrays)
        {
            foreach (var classInfo in classInfoArray)
            {
                if (!seenTypes.Contains(classInfo.FullClassName))
                {
                    seenTypes.Add(classInfo.FullClassName);
                    allClassInfos.Add(classInfo);
                }
            }
        }
        
        if (allClassInfos.Count == 0)
            return;
        
        var classInfosArray = new EquatableArray<ClassInfo>(allClassInfos);
        CodeGenerator.Execute(classInfosArray, context);
    }
}
