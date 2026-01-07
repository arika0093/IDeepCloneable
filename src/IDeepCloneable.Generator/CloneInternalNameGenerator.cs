using Microsoft.CodeAnalysis;
using System.Collections.Generic;

namespace IDeepCloneable.Generator;

/// <summary>
/// Helper class to generate CloneInternal method names and track which types have them
/// </summary>
internal class CloneInternalNameGenerator
{
    private readonly Dictionary<string, string> _typeToSafeName = new Dictionary<string, string>();

    public void RegisterType(string fullTypeName)
    {
        if (!_typeToSafeName.ContainsKey(fullTypeName))
        {
            _typeToSafeName[fullTypeName] = GenerateSafeName(fullTypeName);
        }
    }

    public bool HasCloneInternal(string fullTypeName)
    {
        return _typeToSafeName.ContainsKey(fullTypeName);
    }

    public string GetCloneInternalName(string fullTypeName)
    {
        if (_typeToSafeName.TryGetValue(fullTypeName, out var safeName))
        {
            return $"IDeepCloneable.Extensions.DeepCloneExtensions.{safeName}CloneInternal";
        }
        return null;
    }

    public string GetSafeName(string fullTypeName)
    {
        if (_typeToSafeName.TryGetValue(fullTypeName, out var safeName))
        {
            return safeName;
        }
        return GenerateSafeName(fullTypeName);
    }

    private static string GenerateSafeName(string fullTypeName)
    {
        return fullTypeName
            .Replace(".", "_")
            .Replace("<", "_")
            .Replace(">", "_")
            .Replace(",", "_")
            .Replace(" ", "")
            .Replace("[", "_")
            .Replace("]", "_")
            .Replace("(", "_")
            .Replace(")", "_")
            .Trim('_');
    }
}
