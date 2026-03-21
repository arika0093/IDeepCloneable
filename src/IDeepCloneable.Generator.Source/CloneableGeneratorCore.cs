using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;

namespace IDeepCloneable.Generator;

/// <summary>
/// Base class for incremental source generators for automatic DeepClone implementation.
/// </summary>
internal abstract class CloneableGeneratorCore<TOptions>() : IIncrementalGenerator
    where TOptions : CloneableGeneratorOptionsCore, new()
{
    /// <summary>
    /// Generator options instance.
    /// </summary>
    private static readonly TOptions options = new();

    /// <summary>
    /// Initializes the incremental generator.
    /// </summary>
    public virtual void Initialize(IncrementalGeneratorInitializationContext context)
    {
        GenerateEmbbedAttributes(context);
        var deepCloneableDeclarations = context
            .SyntaxProvider.ForAttributeWithMetadataName(
                options.AttributeMetadataName,
                predicate: static (node, _) => true,
                transform: TransformFunc
            )
            .Where(static m => m.HasValue)
            .SelectMany(static (m, _) => m!.Value);

        var generateCloneableDeclarations = context
            .SyntaxProvider.ForAttributeWithMetadataName(
                options.GenerateDeepCloneableAttributeName,
                predicate: static (node, _) => true,
                transform: TransformGenerateDeepCloneableFunc
            )
            .Where(static m => m.HasValue)
            .SelectMany(static (m, _) => m!.Value);

        var classDeclarations = deepCloneableDeclarations
            .Collect()
            .Combine(generateCloneableDeclarations.Collect())
            .Select(
                static (pair, _) => new EquatableArray<ClassInfo>(pair.Left.AddRange(pair.Right))
            );

        var distinctClassDeclarations = classDeclarations.Select(
            static (classInfos, cancellationToken) => DropDuplicates(classInfos, cancellationToken)
        );

        var environment = context.CompilationProvider.Select(
            static (compilation, _) => GenerationEnvironment.Create(compilation)
        );

        context.RegisterSourceOutput(
            distinctClassDeclarations.Combine(environment),
            static (spc, pair) => Execute(pair.Left, pair.Right, spc)
        );
    }

    /// <summary>
    /// Transforms the syntax context into class information.
    /// </summary>
    private static EquatableArray<ClassInfo>? TransformFunc(
        GeneratorAttributeSyntaxContext ctx,
        CancellationToken cancellationToken
    ) => TypeAnalyzer.GetRelationalAllClassInfo(ctx, cancellationToken, options);

    /// <summary>
    /// Transforms the syntax context for [GenerateDeepCloneable] into class information.
    /// </summary>
    private static EquatableArray<ClassInfo>? TransformGenerateDeepCloneableFunc(
        GeneratorAttributeSyntaxContext ctx,
        CancellationToken cancellationToken
    ) =>
        TypeAnalyzer.GetRelationalAllClassInfoFromGenerateAttribute(
            ctx,
            cancellationToken,
            options
        );

    /// <summary>
    /// Executes the code generation process.
    /// </summary>
    private static void Execute(
        EquatableArray<ClassInfo> allClassInfos,
        GenerationEnvironment environment,
        SourceProductionContext context
    )
    {
        var codeGenerator = new CodeGenerator(options, environment);
        codeGenerator.Execute(allClassInfos.ToList(), context);
    }

    /// <summary>
    /// Drops duplicate ClassInfo entries based on FullClassName.
    /// </summary>
    private static EquatableArray<ClassInfo> DropDuplicates(
        EquatableArray<ClassInfo> classInfos,
        CancellationToken cancellationToken
    )
    {
        var map = new Dictionary<string, ClassInfo>();
        foreach (var classInfo in classInfos)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!map.TryGetValue(classInfo.FullClassName, out var existing))
            {
                map[classInfo.FullClassName] = classInfo;
                continue;
            }

            // Prefer entries that require generating DeepClone methods.
            if (!existing.NeedsDeepCloneMethod && classInfo.NeedsDeepCloneMethod)
            {
                map[classInfo.FullClassName] = classInfo;
                continue;
            }

            // Merge minimal flags to avoid losing metadata across sources.
            map[classInfo.FullClassName] = existing with
            {
                NeedsDeepCloneMethod =
                    existing.NeedsDeepCloneMethod || classInfo.NeedsDeepCloneMethod,
                BaseHasDeepClone = existing.BaseHasDeepClone || classInfo.BaseHasDeepClone,
            };
        }

        return new EquatableArray<ClassInfo>(map.Values);
    }

    /// <summary>
    /// Generates embedded attribute classes if configured to do so.
    /// </summary>
    protected virtual void GenerateEmbbedAttributes(
        IncrementalGeneratorInitializationContext context
    )
    {
        if (!options.GenerateAttributes)
        {
            return;
        }

        var builder = new IndentedStringBuilder();
        // add file header comment
        builder.AppendLine(
            $"""
            // <auto-generated>
            // {options.GeneratedHeaderComment}
            // </auto-generated>

            """
        );

        // first, add EmbeddedAttribute
        builder.AppendLine(
            """
            namespace Microsoft.CodeAnalysis
            {
                internal sealed partial class EmbeddedAttribute : global::System.Attribute { }
            }

            """
        );

        if (options.ExportAttributeNamespace != null)
        {
            builder.AppendLine($"namespace {options.ExportAttributeNamespace}");
            builder.AppendLine("{");
            builder.IncreaseIndent();
        }

        // next, add [ShallowCloneAttribute] and [CloneIgnoreAttribute]
        if (!string.IsNullOrEmpty(options.ShallowCloneAttributeName))
        {
            builder.AppendLine(
                $$"""
                /// <summary>
                /// Marks a field or property to be shallow-copied during DeepClone operations.
                /// Properties or fields marked with this attribute will have their references copied directly
                /// without creating a deep clone of the object.
                /// </summary>
                [global::Microsoft.CodeAnalysis.Embedded]
                [global::System.AttributeUsage(
                    global::System.AttributeTargets.Property | global::System.AttributeTargets.Field,
                    Inherited = false,
                    AllowMultiple = false
                )]
                internal sealed class {{options.ShallowCloneAttributeName}} : global::System.Attribute { }

                """
            );
        }
        if (!string.IsNullOrEmpty(options.CloneIgnoreAttributeName))
        {
            builder.AppendLine(
                $$"""
                /// <summary>
                /// Marks a field or property to be ignored during DeepClone operations.
                /// Properties or fields marked with this attribute will not be cloned and will retain their default values.
                /// </summary>
                [global::Microsoft.CodeAnalysis.Embedded]
                [global::System.AttributeUsage(
                    global::System.AttributeTargets.Property | global::System.AttributeTargets.Field,
                    Inherited = false,
                    AllowMultiple = false
                )]
                internal sealed class {{options.CloneIgnoreAttributeName}} : global::System.Attribute { }

                """
            );
        }
        if (!string.IsNullOrEmpty(options.GenerateDeepCloneableAttributeName))
        {
            // finally, add [GenerateDeepCloneableAttribute]
            builder.AppendLine(
                $$"""
                /// <summary>
                /// Registers a target type for DeepClone generation without requiring the target to be partial.
                /// Use this to generate clone logic for types you can't modify.
                /// </summary>
                [global::Microsoft.CodeAnalysis.Embedded]
                [global::System.AttributeUsage(
                    global::System.AttributeTargets.Class | global::System.AttributeTargets.Struct,
                    Inherited = false,
                    AllowMultiple = true
                )]
                internal sealed class {{options.GenerateDeepCloneableAttributeName}} : global::System.Attribute
                {
                    /// <summary>
                    /// The target type to generate deep clone logic for.
                    /// </summary>
                    public global::System.Type TargetType { get; }

                    /// <summary>
                    /// Registers the target type to be included in clone generation.
                    /// </summary>
                    /// <param name="targetType">The type to generate clone logic for.</param>
                    public {{options.GenerateDeepCloneableAttributeName}}(global::System.Type targetType)
                    {
                        TargetType = targetType;
                    }
                }

                """
            );
        }

        if (options.ExportAttributeNamespace != null)
        {
            builder.DecreaseIndent();
            builder.AppendLine("}");
        }

        var source = builder.ToString();
        context.RegisterPostInitializationOutput(i =>
        {
            i.AddSource($"{options.AttributeExportFileName}.g.cs", source);
        });
    }
}
