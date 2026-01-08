using Microsoft.CodeAnalysis;
using System.Collections.Generic;
using System.Text;

namespace IDeepCloneable.Generator.TypeHandlers;

/// <summary>
/// Handler registry that manages all type handlers
/// </summary>
internal class TypeHandlerRegistry
{
    private readonly List<ITypeHandler> _handlers;

    public TypeHandlerRegistry()
    {
        _handlers = new List<ITypeHandler>
        {
            new ValueTypeHandler(),
            // More handlers will be added here
        };

        // Sort by priority (lower values first)
        _handlers.Sort((a, b) => a.Priority.CompareTo(b.Priority));
    }

    public ITypeHandler? GetHandler(ITypeSymbol typeSymbol, IPropertySymbol property)
    {
        foreach (var handler in _handlers)
        {
            if (handler.CanHandle(typeSymbol, property))
            {
                return handler;
            }
        }
        return null;
    }

    public void GenerateCloneStatements(
        StringBuilder sb,
        IPropertySymbol property,
        ITypeSymbol typeSymbol,
        string sourceVar,
        string targetVar,
        string indent)
    {
        var handler = GetHandler(typeSymbol, property);
        if (handler != null)
        {
            handler.GenerateCloneStatements(sb, property, typeSymbol, sourceVar, targetVar, indent);
        }
        else
        {
            // Fallback: simple assignment
            sb.AppendLine($"{indent}{targetVar}.{property.Name} = {sourceVar}.{property.Name};");
        }
    }
}
