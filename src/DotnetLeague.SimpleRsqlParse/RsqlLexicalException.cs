namespace DotnetLeague.SimpleRsqlParse;

/// <summary>
/// Represents an exception that occurs during the lexical analysis (tokenization) of an RSQL string.
/// </summary>
/// <param name="message">The message that describes the error.</param>
/// <param name="position">The position in the input string where the lexical error occurred.</param>
public class RsqlLexicalException(string message, int position) : System.Exception($"{message} at position {position}")
{
    /// <summary>
    /// Gets the position in the input string where the lexical error occurred.
    /// </summary>
    public int Position { get; } = position;
}
