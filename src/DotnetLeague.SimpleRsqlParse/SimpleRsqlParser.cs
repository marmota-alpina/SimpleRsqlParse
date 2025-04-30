using System.Text;

namespace DotnetLeague.SimpleRsqlParse;

/// <summary>
/// Parses an RSQL (RESTful Service Query Language) string into a SQL WHERE clause
/// and a dictionary of parameters.
/// </summary>
public class SimpleRsqlParser
{
    private readonly Tokenizer _tokenizer;
    // We no longer need the _whereParts list; we will build the string directly.
    private readonly Dictionary<string, object> _parameters = new();
    private int _paramCounter = 0;

    /// <summary>
    /// Initializes a new instance of the <see cref="SimpleRsqlParser"/> class.
    /// </summary>
    /// <param name="tokenizer">The tokenizer to be used for tokenizing the RSQL input.</param>
    /// <exception cref="ArgumentNullException">Thrown if the provided tokenizer is null.</exception>
    public SimpleRsqlParser(Tokenizer tokenizer)
    {
        _tokenizer = tokenizer ?? throw new ArgumentNullException(nameof(tokenizer));
    }

    /// <summary>
    /// Parses the RSQL input provided to the tokenizer and generates a SQL WHERE clause
    /// and a dictionary of corresponding parameters.
    /// </summary>
    /// <returns>A tuple containing the generated SQL WHERE clause string and a dictionary of parameters.</returns>
    /// <exception cref="RsqlSyntaxException">Thrown if a syntax error is found in the RSQL input.</exception>
    public (string whereClause, Dictionary<string, object> parameters) Parse()
    {
        // If the input is empty (or just whitespace), we return "WHERE 1=1" directly
        if (_tokenizer.Peek().Type == TokenType.Eof)
        {
            return ("WHERE 1=1", _parameters);
        }

        string expression = ParseExpression();

        // Check if there are any unexpected tokens left after parsing the main expression
        Token finalToken = _tokenizer.Peek();
        if (finalToken.Type != TokenType.Eof)
        {
            throw new RsqlSyntaxException($"Unexpected token found after the end of the main expression: {finalToken.Type}", finalToken);
        }

        return ($"WHERE {expression}", _parameters);
    }

    /// <summary>
    /// Parses an RSQL expression, handling the OR operator (comma).
    /// This method implements the lowest precedence level of the grammar.
    /// </summary>
    /// <returns>A string representing the parsed expression segment.</returns>
    /// <exception cref="RsqlSyntaxException">Thrown if a syntax error is found during parsing.</exception>
    private string ParseExpression()
    {
        string left = ParseAndTerm(); // Starts with terms linked by AND

        while (_tokenizer.Peek().Type == TokenType.Or)
        {
            Token opToken = _tokenizer.Consume(); // Consumes the comma (OR)

            // Check for abrupt termination after OR
            if (_tokenizer.Peek().Type == TokenType.Eof)
            {
                throw new RsqlSyntaxException("Expression expected after OR operator (',')", opToken);
            }
            // Check for consecutive logical operators
             if (_tokenizer.Peek().Type == TokenType.And || _tokenizer.Peek().Type == TokenType.Or)
             {
                 throw new RsqlSyntaxException($"Unexpected logical operator '{_tokenizer.Peek().Value}' after '{opToken.Value}'", _tokenizer.Peek());
             }

            string right = ParseAndTerm(); // Get the next term (linked by AND)
            left = $"({left} OR {right})"; // Combine with OR (keeping parentheses for current clarity/grouping)
        }

        return left;
    }

    /// <summary>
    /// Parses an RSQL term, handling the AND operator (semicolon).
    /// This method implements the intermediate precedence level of the grammar.
    /// </summary>
    /// <returns>A string representing the parsed term segment.</returns>
    /// <exception cref="RsqlSyntaxException">Thrown if a syntax error is found during parsing.</exception>
    private string ParseAndTerm()
    {
        string left = ParseTerm(); // Starts with a simple term or parentheses

        while (_tokenizer.Peek().Type == TokenType.And)
        {
            Token opToken = _tokenizer.Consume(); // Consumes the semicolon (AND)

             // Check for abrupt termination after AND
            if (_tokenizer.Peek().Type == TokenType.Eof)
            {
                throw new RsqlSyntaxException("Expression expected after AND operator (';')", opToken);
            }
            // Check for consecutive logical operators
             if (_tokenizer.Peek().Type == TokenType.And || _tokenizer.Peek().Type == TokenType.Or)
             {
                 throw new RsqlSyntaxException($"Unexpected logical operator '{_tokenizer.Peek().Value}' after '{opToken.Value}'", _tokenizer.Peek());
             }

            string right = ParseTerm(); // Get the next term
            left = $"({left} AND {right})"; // Combine with AND
        }

        return left;
    }

    /// <summary>
    /// Parses a single RSQL term, which can be either a grouped expression within parentheses
    /// or a constraint (field, operator, value). This method handles the highest precedence level.
    /// </summary>
    /// <returns>A string representing the parsed term.</returns>
    /// <exception cref="RsqlSyntaxException">Thrown if a syntax error is found during parsing.</exception>
    private string ParseTerm()
    {
        Token nextToken = _tokenizer.Peek();

        switch (nextToken.Type)
        {
            case TokenType.LParen:
            {
                _tokenizer.Consume(); // Consume '('

                // Check for empty parentheses
                if (_tokenizer.Peek().Type == TokenType.RParen)
                {
                    throw new RsqlSyntaxException("Empty parentheses '()' are not allowed", nextToken);
                }

                string expr = ParseExpression(); // Parse the expression inside the parentheses

                // Expect and consume the corresponding ')'
                _tokenizer.Expect(TokenType.RParen); // Expect already throws an error if ')' is not found

                // No need to add extra parentheses here, as ParseExpression/ParseAndTerm already add them if needed
                // to group AND/OR. If the internal expression is simple, it's already formatted.
                // Returning expr directly avoids unnecessary double parentheses in cases like `(a==1)`
                return expr; // Return the internal expression directly
                // return $"({expr})"; // -> Generated extra parentheses: WHERE (((a = @p0)))
            }
            // A constraint starts with a field (UnreservedStr)
            case TokenType.UnreservedStr:
                return ParseConstraint();
            default:
                // If it's neither '(' nor a field name, it's a syntax error
                // Cases like starting with an operator `==value` or `( ; a=1)`
                throw new RsqlSyntaxException($"Expected beginning of expression (field name or '(') but found {nextToken.Type}", nextToken);
        }
    }

    /// <summary>
    /// Parses a single RSQL constraint, which follows the format: field OPERATOR value(s).
    /// This is the most granular parsing method for conditions.
    /// </summary>
    /// <returns>A string representing the parsed SQL condition fragment (e.g., "field = @p0").</returns>
    /// <exception cref="RsqlSyntaxException">Thrown if a syntax error is found during parsing the constraint.</exception>
    private string ParseConstraint()
    {
        // Expect a field name (which must be UnreservedStr)
        Token fieldToken = _tokenizer.Expect(TokenType.UnreservedStr);
        string field = fieldToken.Value;

        // Expect a comparison operator
        Token opToken = _tokenizer.Expect(TokenType.Operator);
        string rsqlOperator = opToken.Value;

        // Translate the RSQL operator to SQL
        string sqlOperator = rsqlOperator.ToLowerInvariant() switch // Using ToLowerInvariant for safety
        {
            "==" => "=",
            "!=" => "<>",
            "=gt=" => ">",
            "=lt=" => "<",
            "=ge=" => ">=",
            "=le=" => "<=",
            "=in=" => "IN",
            "=out=" => "NOT IN",
            "=like=" => "LIKE",
            "like" => "LIKE", // Added lowercase "like" for flexibility
            "=ilike=" => "ILIKE",   // Note: ILIKE is not standard in all RDBMS like SQL Server
            "ilike" => "ILIKE", // Added lowercase "ilike" for flexibility
            // No need for a default, as the Tokenizer already ensures it's a valid operator
            // However, an extra check can be useful:
            _ => throw new RsqlSyntaxException($"Unknown or unexpected RSQL operator '{rsqlOperator}' found by the parser", opToken)
        };

        // --- Value(s) Processing ---
        Token valuePeek = _tokenizer.Peek();

        if (sqlOperator == "IN" || sqlOperator == "NOT IN")
        {
            // For IN/OUT, we expect a list in parentheses: field=in=(value1, value2, 'value 3')
            _tokenizer.Expect(TokenType.LParen); // Expect '('

            var paramNames = new List<string>();
            bool firstValue = true;
            while (_tokenizer.Peek().Type != TokenType.RParen)
            {
                // If not the first value, expect a comma separator
                if (!firstValue)
                {
                    _tokenizer.Expect(TokenType.Or); // Use ',' as separator in IN/OUT list
                }

                Token valueToken = _tokenizer.Peek();
                switch (valueToken.Type)
                {
                    case TokenType.UnreservedStr:
                    case TokenType.Literal:
                    {
                        _tokenizer.Consume(); // Consume the value
                        string paramName = $"@p{_paramCounter++}";
                        _parameters.Add(paramName, valueToken.Value);
                        paramNames.Add(paramName);
                        break;
                    }
                    // Check for unexpected end within the list
                    case TokenType.Eof:
                        throw new RsqlSyntaxException($"Unexpected end of input within {sqlOperator} list", valueToken);
                    // If it's not UnreservedStr or Literal, it's an error in the list
                    default:
                        throw new RsqlSyntaxException($"Invalid value '{valueToken.Value}' (type {valueToken.Type}) found in {sqlOperator} list", valueToken);
                }
                firstValue = false;
            }

            // Expect the closing ')' of the list
            _tokenizer.Expect(TokenType.RParen);

            // Check if the list was empty (just '(' and ')')
            if (paramNames.Count == 0)
            {
                return "1=1";
            }

            return $"{field} {sqlOperator} ({string.Join(",", paramNames)})";
        }

        // Simple comparison operators (==, !=, >, <, etc.)
        // Expect a single value (Literal or UnreservedStr)
        if (valuePeek.Type == TokenType.UnreservedStr || valuePeek.Type == TokenType.Literal)
        {
            Token valueToken = _tokenizer.Consume(); // Consume the value
            string paramName = $"@p{_paramCounter++}";
            _parameters.Add(paramName, valueToken.Value);
            return $"{field} {sqlOperator} {paramName}"; // Parentheses around the field removed (e.g., field = @p0)
        }

        // If it's not a valid value after the operator
        throw new RsqlSyntaxException($"Expected a value (Literal or UnreservedStr) after operator '{rsqlOperator}' but found {valuePeek.Type}", valuePeek);
    }
}
