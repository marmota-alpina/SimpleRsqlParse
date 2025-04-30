using System.Text;

namespace DotnetLeague.SimpleRsqlParse;

/// <summary>
/// Breaks an RSQL input string into a sequence of tokens.
/// </summary>
public class Tokenizer
{
    private readonly string _input;
    private int _pos;
    private static readonly HashSet<string> Operators = new(StringComparer.OrdinalIgnoreCase)
    {
        "==", "!=", "=gt=", "=lt=", "=ge=", "=le=", "=in=", "=out=", "=like=", "=ilike="
    };

    // Ordenar operadores do mais longo para o mais curto para evitar correspondências parciais (ex: pegar '==' antes de '=')
    // Embora o HashSet não garanta ordem, podemos fazer a verificação explícita ou usar uma Lista ordenada.
    // Para este conjunto específico, a ordem de verificação atual pode funcionar, mas ser explícito é mais seguro.
    private static readonly List<string> OrderedOperators = Operators.OrderByDescending(op => op.Length).ToList();

    /// <summary>
    /// Initializes a new instance of the <see cref="Tokenizer"/> class with the RSQL input string.
    /// </summary>
    /// <param name="input">The RSQL string to tokenize.</param>
    public Tokenizer(string input)
    {
        _input = input ?? string.Empty; // Garantir que não seja nulo
        _pos = 0;
    }

    /// <summary>
    /// Peeks at the next token in the input stream without consuming it.
    /// </summary>
    /// <returns>The next token, or an Eof token if the end of the input is reached.</returns>
    public Token Peek()
    {
        int savedPos = _pos;
        Token token = Next();
        _pos = savedPos;
        return token;
    }

    /// <summary>
    /// Consumes and returns the next token from the input stream.
    /// </summary>
    /// <returns>The next token, or an Eof token if the end of the input is reached.</returns>
    public Token Consume() => Next();

    /// <summary>
    /// Consumes the next token and verifies if its type matches the expected type.
    /// </summary>
    /// <param name="expectedType">The expected type of the next token.</param>
    /// <returns>The consumed token if its type matches the expected type.</returns>
    /// <exception cref="RsqlSyntaxException">Thrown if the next token's type does not match the expected type.</exception>
    public Token Expect(TokenType expectedType)
    {
        Token token = Consume();
        if (token.Type != expectedType)
        {
            // Usa a exceção customizada com posição
            throw new RsqlSyntaxException($"Esperado token do tipo {expectedType} mas encontrado {token.Type}", token);
        }
        return token;
    }

     /// <summary>
     /// Consumes the next token and verifies if its type and value match the expected type and value.
     /// </summary>
     /// <param name="expectedType">The expected type of the next token.</param>
     /// <param name="expectedValue">The expected string value of the next token.</param>
     /// <returns>The consumed token if its type and value match.</returns>
     /// <exception cref="RsqlSyntaxException">Thrown if the next token's type or value does not match.</exception>
     public Token Expect(TokenType expectedType, string expectedValue)
    {
        Token token = Expect(expectedType); // Primeiro verifica o tipo
        if (!string.Equals(token.Value, expectedValue, StringComparison.Ordinal)) // Comparação segura
        {
             throw new RsqlSyntaxException($"Esperado token '{expectedValue}' mas encontrado '{token.Value}'", token);
        }
        return token;
    }

    /// <summary>
    /// Gets the next token from the input string. This is the core tokenization logic.
    /// </summary>
    /// <returns>The next token found in the input stream.</returns>
    /// <exception cref="RsqlLexicalException">Thrown if an unexpected character or unterminated literal is found.</exception>
    private Token Next()
    {
        SkipWhitespace();

        int startPos = _pos; // Guarda a posição inicial do token

        if (_pos >= _input.Length)
        {
            return new Token(TokenType.Eof, string.Empty, startPos);
        }

        char currentChar = _input[_pos];

        // --- Tokens simples de um caractere ---
        if (currentChar == ';') { _pos++; return new Token(TokenType.And, ";", startPos); }
        if (currentChar == ',') { _pos++; return new Token(TokenType.Or, ",", startPos); }
        if (currentChar == '(') { _pos++; return new Token(TokenType.LParen, "(", startPos); }
        if (currentChar == ')') { _pos++; return new Token(TokenType.RParen, ")", startPos); }

        // --- Literais entre aspas ---
        if (currentChar == '\'' || currentChar == '"')
        {
            char quoteType = currentChar;
            _pos++; // Consome a aspa inicial
            var sb = new StringBuilder();
            while (_pos < _input.Length)
            {
                char c = _input[_pos];
                if (c == quoteType)
                {
                    _pos++; // Consome a aspa final
                    // Retorna o conteúdo *dentro* das aspas
                    return new Token(TokenType.Literal, sb.ToString(), startPos);
                }
                // Tratamento simples de escape (opcional, RSQL não especifica, mas útil)
                // if (c == '\\' && _pos + 1 < _input.Length)
                // {
                //     _pos++; // Consome a barra
                //     sb.Append(_input[_pos]); // Adiciona o caractere escapado
                //     _pos++;
                // }
                else
                {
                    sb.Append(c);
                    _pos++;
                }
            }
            // Se chegou aqui, a string não foi terminada
            throw new RsqlLexicalException($"Literal não terminado iniciado com {quoteType}", startPos);
        }

        // --- Operadores ---
        // Verifica do mais longo para o mais curto para garantir a correspondência correta (ex: =ge= antes de ==)
        foreach (string op in OrderedOperators)
        {
             // Verifica se o operador cabe e se a substring corresponde (case-insensitive se desejado)
             if (_pos + op.Length <= _input.Length &&
                 _input.Substring(_pos, op.Length).Equals(op, StringComparison.OrdinalIgnoreCase))
             {
                 _pos += op.Length;
                 return new Token(TokenType.Operator, op, startPos);
             }
        }

        // --- Unreserved String (Campos ou valores sem aspas) ---
        // Consome caracteres até encontrar um caractere especial ou espaço em branco
        // Caracteres especiais agora incluem aspas, pois são tratadas separadamente
        int valueStart = _pos;
        while (_pos < _input.Length && !IsSpecialCharOrQuote(_input[_pos]))
        {
            _pos++;
        }

        // Se não avançou, pode ser um caractere inválido ou o início de outro token
        if (_pos == valueStart)
        {
             // Se não for nenhum dos tokens conhecidos, é um erro léxico
             throw new RsqlLexicalException($"Caractere inesperado '{_input[_pos]}'", _pos);
        }

        string value = _input[valueStart.._pos];
        return new Token(TokenType.UnreservedStr, value, startPos);
    }

    /// <summary>
    /// Checks if a character is a special RSQL delimiter or a quote character.
    /// </summary>
    /// <param name="c">The character to check.</param>
    /// <returns>True if the character is a special character or a quote, false otherwise.</returns>
    private bool IsSpecialCharOrQuote(char c)
    {
        return c == ';' || c == ',' || c == '(' || c == ')' || c == '\'' || c == '"' || char.IsWhiteSpace(c) || c == '=' || c == '!' || c == '=';
    }

    /// <summary>
    /// Skips whitespace characters in the input string starting from the current position.
    /// </summary>
    private void SkipWhitespace()
    {
        while (_pos < _input.Length && char.IsWhiteSpace(_input[_pos]))
        {
            _pos++;
        }
    }
}
