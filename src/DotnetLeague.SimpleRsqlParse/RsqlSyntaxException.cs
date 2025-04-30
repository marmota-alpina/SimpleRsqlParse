namespace DotnetLeague.SimpleRsqlParse;

/// <summary>
/// Represents an exception that occurs during the syntax analysis (parsing) of an RSQL string.
/// </summary>
public class RsqlSyntaxException : Exception
{
    /// <summary>
    /// Gets the approximate position in the input string where the syntax error was detected.
    /// </summary>
    public int Position { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="RsqlSyntaxException"/> class with a specified error message and the position of the error.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    /// <param name="position">The position in the input string where the syntax error was detected.</param>
    public RsqlSyntaxException(string message, int position) : base($"{message} near position {position}")
    {
        Position = position;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="RsqlSyntaxException"/> class with a specified error message and the token near where the error occurred.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    /// <param name="nearToken">The token near where the syntax error was detected.</param>
    public RsqlSyntaxException(string message, Token nearToken) : base($"{message} near '{nearToken.Value}' at position {nearToken.Position}")
    {
        Position = nearToken.Position;
    }
}
