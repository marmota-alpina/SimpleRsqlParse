namespace DotnetLeague.SimpleRsqlParse;

/// <summary>
/// Represents the types of tokens recognized by the RSQL parser.
/// </summary>
public enum TokenType
{
    /// <summary>
    /// Represents the logical AND operator, typically represented by a semicolon (;).
    /// </summary>
    And,          // ;

    /// <summary>
    /// Represents the logical OR operator, typically represented by a comma (,).
    /// </summary>
    Or,           // ,

    /// <summary>
    /// Represents a left parenthesis ((). Used for grouping expressions.
    /// </summary>
    LParen,       // (

    /// <summary>
    /// Represents a right parenthesis ()). Used for grouping expressions.
    /// </summary>
    RParen,       // )

    /// <summary>
    /// Represents a comparison or operation operator (e.g., ==, !=, =gt=, =in=).
    /// </summary>
    Operator,     // ==, !=, =gt=, etc.

    /// <summary>
    /// Represents an unreserved string, typically used for field names or unquoted values.
    /// </summary>
    UnreservedStr,

    /// <summary>
    /// Represents a literal value, typically enclosed in single or double quotes.
    /// </summary>
    Literal,

    /// <summary>
    /// Represents the end of the input string.
    /// </summary>
    Eof
}
