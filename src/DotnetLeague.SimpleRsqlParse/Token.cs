namespace DotnetLeague.SimpleRsqlParse;

/// <summary>
/// Represents a single token parsed from the RSQL input string.
/// </summary>
public class Token
{
    /// <summary>
    /// Gets the type of the token.
    /// </summary>
    public TokenType Type { get; }

    /// <summary>
    /// Gets the string value of the token.
    /// </summary>
    public string Value { get; }

    /// <summary>
    /// Gets the starting position of the token in the input string.
    /// </summary>
    public int Position { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="Token"/> class.
    /// </summary>
    /// <param name="type">The type of the token.</param>
    /// <param name="value">The string value of the token.</param>
    /// <param name="position">The starting position of the token in the input string.</param>
    public Token(TokenType type, string value, int position)
    {
        Type = type;
        Value = value;
        Position = position;
    }

    /// <summary>
    /// Returns a string representation of the current token.
    /// </summary>
    /// <returns>A string that represents the current token.</returns>
    public override string ToString() => $"Token({Type}, '{Value}', Pos={Position})";
}
