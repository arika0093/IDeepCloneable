using System;
using Microsoft.CodeAnalysis;

namespace IDeepCloneable.Generator;

/// <summary>
/// Incremental source generator for automatic DeepClone implementation.
/// Generates DeepClone methods for types marked with [DeepCloneable] attribute.
/// </summary>
[Generator]
internal class CloneableGenerator : CloneableGeneratorCore<CloneableGeneratorOptions>;
