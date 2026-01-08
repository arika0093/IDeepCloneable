/// <summary>
/// Utility class for efficiently building indented strings.
/// </summary>
/// <param name="IndentLevel">Initial indent level (0 or greater).</param>
public class IndentedStringBuilder(int IndentLevel = 0)
{
    private readonly System.Text.StringBuilder _builder = new();

    /// <summary>Number of spaces per indent level.</summary>
    public const int IndentSize = 4;

    /// <summary>Returns a string of spaces representing the current indent.</summary>
    public string Indent => new string(' ', IndentLevel * IndentSize);

    /// <summary>Returns a new builder with the indent level increased by one.</summary>
    public IndentedStringBuilder IncreaseIndent() =>
        new IndentedStringBuilder(IndentLevel + 1);

    /// <summary>Returns a new builder with the indent level decreased by one (not below zero).</summary>
    public IndentedStringBuilder DecreaseIndent() => 
        new IndentedStringBuilder(Math.Max(0, IndentLevel - 1));

    /// <summary>
    /// Appends the specified text with the current indent. Newlines in the text are handled per line.
    /// </summary>
    /// <param name="text">The text to append (may contain multiple lines).</param>
    public void Append(string text)
    {
        var lines = text.Split(new[] { "\r\n", "\r", "\n" });
        foreach (var line in lines)
        {
            _builder.AppendLine(Indent + line);
        }
    }

    /// <summary>Appends the specified text and adds a trailing newline.</summary>
    /// <param name="text">The text to append.</param>
    public void AppendLine(string text) => Append(text + "\n");

    /// <summary>Appends the contents of another IndentedStringBuilder to this builder.</summary>
    /// <param name="builder">The builder whose contents will be appended.</param>
    public void Append(IndentedStringBuilder builder) => Append(builder.ToString());

    /// <summary>Appends the contents of another IndentedStringBuilder and adds a trailing newline.</summary>
    /// <param name="builder">The builder whose contents will be appended.</param>
    public void AppendLine(IndentedStringBuilder builder) => Append(builder.ToString() + "\n");

    /// <summary>Returns the current contents of the builder as a string.</summary>
    public override string ToString() => _builder.ToString();
}