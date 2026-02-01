using System;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace IDeepCloneable.Generator;

/// <summary>
/// Captures environment details for source generation.
/// </summary>
public readonly record struct GenerationEnvironment
{
    /// <summary>
    /// Is array to Span API supported?
    /// </summary>
    public bool SupportsArrayAsSpan { get; init; }

    /// <summary>
    /// Initializes a new instance of the <see cref="GenerationEnvironment"/> struct.
    /// </summary>
    public static GenerationEnvironment Create(Compilation compilation)
    {
        return new GenerationEnvironment
        {
            SupportsArrayAsSpan = SupportsArrayAsSpanApi(compilation),
        };
    }

    /// <summary>
    /// Determines whether the compilation supports Array to Span API.
    /// </summary>
    private static bool SupportsArrayAsSpanApi(Compilation compilation)
    {
        var spanType = compilation.GetTypesByMetadataName("System.Span`1");
        return spanType.Any();
    }
}
