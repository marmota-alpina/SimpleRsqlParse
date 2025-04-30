namespace DotnetLeague.SimpleRsqlParse.Tests;

/// <summary>
/// Contains unit tests for the <see cref="SimpleRsqlParser"/> class.
/// </summary>
public class SimpleRsqlParserTests
{
    /// <summary>
    /// Helper method to parse an RSQL string and return the generated WHERE clause and parameters.
    /// </summary>
    /// <param name="rsql">The RSQL string to parse.</param>
    /// <returns>A tuple containing the generated SQL WHERE clause string and a dictionary of parameters.</returns>
    private (string where, Dictionary<string, object> parameters) Parse(string rsql)
    {
        var tokenizer = new Tokenizer(rsql);
        var parser = new SimpleRsqlParser(tokenizer);
        return parser.Parse();
    }

    /// <summary>
    /// Tests parsing a simple RSQL equality constraint (==).
    /// </summary>
    [Fact]
    public void Parse_SimpleEquals()
    {
        var (where, parameters) = Parse("name==Phone");
        Assert.Equal("WHERE name = @p0", where);
        Assert.Single(parameters);
        Assert.Equal("Phone", parameters["@p0"]);
    }

    /// <summary>
    /// Tests parsing a simple RSQL not-equals constraint (!=).
    /// </summary>
    [Fact]
    public void Parse_SimpleNotEquals()
    {
        var (where, parameters) = Parse("status!=inactive");
        Assert.Equal("WHERE status <> @p0", where);
        Assert.Single(parameters);
        Assert.Equal("inactive", parameters["@p0"]);
    }

    /// <summary>
    /// Tests parsing a simple RSQL greater-than constraint (=gt=).
    /// </summary>
    [Fact]
    public void Parse_SimpleGreaterThan()
    {
        var (where, parameters) = Parse("price=gt=1000");
        Assert.Equal("WHERE price > @p0", where);
        Assert.Single(parameters);
        // Ideally, the parser/tokenizer could convert to a number,
        // but for now we keep it as a string
        Assert.Equal("1000", parameters["@p0"]);
    }

     /// <summary>
     /// Tests parsing a simple RSQL less-than constraint (=lt=).
     /// </summary>
    [Fact]
    public void Parse_SimpleLessThan()
    {
        var (where, parameters) = Parse("price=lt=500");
        Assert.Equal("WHERE price < @p0", where);
        Assert.Single(parameters);
        Assert.Equal("500", parameters["@p0"]);
    }

    /// <summary>
    /// Tests parsing a simple RSQL constraint with the AND operator (;).
    /// </summary>
    [Fact]
    public void Parse_AndOperator()
    {
        var (where, parameters) = Parse("name==Phone;status==active");
        Assert.Equal("WHERE (name = @p0 AND status = @p1)", where);
        Assert.Equal(2, parameters.Count);
        Assert.Equal("Phone", parameters["@p0"]);
        Assert.Equal("active", parameters["@p1"]);
    }

    /// <summary>
    /// Tests parsing a simple RSQL constraint with the OR operator (,).
    /// </summary>
    [Fact]
    public void Parse_OrOperator()
    {
        var (where, parameters) = Parse("name==Phone,status==Tablet");
        Assert.Equal("WHERE (name = @p0 OR status = @p1)", where);
        Assert.Equal(2, parameters.Count);
        Assert.Equal("Phone", parameters["@p0"]);
        Assert.Equal("Tablet", parameters["@p1"]);
    }

    /// <summary>
    /// Tests parsing an RSQL expression with a grouped constraint and the AND operator.
    /// </summary>
    [Fact]
    public void Parse_GroupedExpression()
    {
        var (where, parameters) = Parse("(name==Phone,status==Tablet);status==active");
        Assert.Equal("WHERE ((name = @p0 OR status = @p1) AND status = @p2)", where);
        Assert.Equal(3, parameters.Count);
        Assert.Equal("Phone", parameters["@p0"]);
        Assert.Equal("Tablet", parameters["@p1"]);
        Assert.Equal("active", parameters["@p2"]);
    }

    /// <summary>
    /// Tests parsing an RSQL constraint using the IN operator (=in=) with multiple values.
    /// </summary>
    [Fact]
    public void Parse_InOperator()
    {
        var (where, parameters) = Parse("category=in=(Electronics,Furniture)");
        Assert.Equal("WHERE category IN (@p0,@p1)", where);
        Assert.Equal(2, parameters.Count);
        Assert.Equal("Electronics", parameters["@p0"]);
        Assert.Equal("Furniture", parameters["@p1"]);
    }

    /// <summary>
    /// Tests parsing an RSQL constraint using the OUT operator (=out=) with multiple values.
    /// </summary>
    [Fact]
    public void Parse_OutOperator()
    {
        var (where, parameters) = Parse("category=out=(Food,Drinks)");
        Assert.Equal("WHERE category NOT IN (@p0,@p1)", where);
        Assert.Equal(2, parameters.Count);
        Assert.Equal("Food", parameters["@p0"]);
        Assert.Equal("Drinks", parameters["@p1"]);
    }

    /// <summary>
    /// Tests parsing a complex RSQL expression with nested parentheses.
    /// </summary>
    [Fact]
    public void Parse_NestedParentheses()
    {
        var (where, parameters) = Parse("((name==Phone,status==Tablet);(price=gt=500,price=lt=1000))");
        Assert.Equal("WHERE ((name = @p0 OR status = @p1) AND (price > @p2 OR price < @p3))", where);
        Assert.Equal(4, parameters.Count);
        Assert.Equal("Phone", parameters["@p0"]);
        Assert.Equal("Tablet", parameters["@p1"]);
        Assert.Equal("500", parameters["@p2"]);
        Assert.Equal("1000", parameters["@p3"]);
    }

    /// <summary>
    /// Tests parsing an empty RSQL input string.
    /// </summary>
    [Fact]
    public void Parse_EmptyInput()
    {
        var (where, parameters) = Parse("");
        Assert.Equal("WHERE 1=1", where);
        Assert.Empty(parameters);
    }

    /// <summary>
    /// Tests parsing an RSQL constraint with a literal value enclosed in single quotes.
    /// </summary>
    [Fact]
    public void Parse_Literal_SingleQuotes()
    {
        var (where, parameters) = Parse("name=='Phone XL'");
        Assert.Equal("WHERE name = @p0", where);
        Assert.Single(parameters);
        Assert.Equal("Phone XL", parameters["@p0"]);
    }

    /// <summary>
    /// Tests parsing an RSQL constraint with a literal value enclosed in double quotes.
    /// </summary>
    [Fact]
    public void Parse_Literal_DoubleQuotes()
    {
        var (where, parameters) = Parse("description==\"A gadget\"");
        Assert.Equal("WHERE description = @p0", where);
        Assert.Single(parameters);
        Assert.Equal("A gadget", parameters["@p0"]);
    }

    /// <summary>
    /// Tests parsing an RSQL constraint with a literal value containing a comma inside quotes.
    /// </summary>
    [Fact]
    public void Parse_Literal_WithCommaInside()
    {
        var (where, parameters) = Parse("address=='Street, Number'");
        Assert.Equal("WHERE address = @p0", where);
        Assert.Single(parameters);
        Assert.Equal("Street, Number", parameters["@p0"]);
    }

    /// <summary>
    /// Tests parsing an RSQL constraint with a literal value containing a semicolon inside quotes.
    /// </summary>
    [Fact]
    public void Parse_Literal_WithSemicolonInside()
    {
        var (where, parameters) = Parse("notes=='High priority; check stock'");
        Assert.Equal("WHERE notes = @p0", where);
        Assert.Single(parameters);
        Assert.Equal("High priority; check stock", parameters["@p0"]);
    }

    /// <summary>
    /// Tests parsing an RSQL constraint with a literal value containing parentheses inside quotes.
    /// </summary>
    [Fact]
    public void Parse_Literal_WithParenthesesInside()
    {
        var (where, parameters) = Parse("tag=='(internal)'");
        Assert.Equal("WHERE tag = @p0", where);
        Assert.Single(parameters);
        Assert.Equal("(internal)", parameters["@p0"]);
    }

    /// <summary>
    /// Tests parsing an RSQL IN constraint with literal values enclosed in quotes.
    /// </summary>
    [Fact]
    public void Parse_Literal_InOperator_WithQuotes()
    {
         var (where, parameters) = Parse("city=in=('New York','Los Angeles')");
         Assert.Equal("WHERE city IN (@p0,@p1)", where);
         Assert.Equal(2, parameters.Count);
         Assert.Equal("New York", parameters["@p0"]);
         Assert.Equal("Los Angeles", parameters["@p1"]);
    }

    /// <summary>
    /// Tests that parsing throws <see cref="RsqlLexicalException"/> for an unterminated string literal with single quotes.
    /// </summary>
    [Fact]
    public void Parse_Error_UnterminatedString_SingleQuote()
    {
        Assert.Throws<RsqlLexicalException>(() => Parse("name=='Phone XL"));
    }

    /// <summary>
    /// Tests that parsing throws <see cref="RsqlLexicalException"/> for an unterminated string literal with double quotes.
    /// </summary>
    [Fact]
    public void Parse_Error_UnterminatedString_DoubleQuote()
    {
        Assert.Throws<RsqlLexicalException>(() => Parse("description==\"A gadget"));
    }

    /// <summary>
    /// Tests that parsing throws <see cref="RsqlLexicalException"/> for an invalid operator.
    /// </summary>
    [Fact]
    public void Parse_Error_InvalidOperator()
    {
        Assert.Throws<RsqlLexicalException>(() => Parse("name=invalid=Phone"));
    }

    /// <summary>
    /// Tests that parsing throws <see cref="RsqlSyntaxException"/> when a value is missing after an operator.
    /// </summary>
    [Fact]
    public void Parse_Error_MissingValueAfterOperator()
    {
        Assert.Throws<RsqlSyntaxException>(() => Parse("name=="));
        Assert.Throws<RsqlSyntaxException>(() => Parse("name==;age=gt=10"));
    }

    /// <summary>
    /// Tests that parsing throws <see cref="RsqlSyntaxException"/> when a field name is missing before an operator.
    /// </summary>
    [Fact]
    public void Parse_Error_MissingFieldBeforeOperator()
    {
        Assert.Throws<RsqlSyntaxException>(() => Parse("==value"));
        Assert.Throws<RsqlSyntaxException>(() => Parse("name==val1;==val2"));
    }

    /// <summary>
    /// Tests that parsing throws <see cref="RsqlSyntaxException"/> for a trailing AND operator.
    /// </summary>
    [Fact]
    public void Parse_Error_TrailingAnd()
    {
        Assert.Throws<RsqlSyntaxException>(() => Parse("name==Phone;"));
    }

    /// <summary>
    /// Tests that parsing throws <see cref="RsqlSyntaxException"/> for a trailing OR operator.
    /// </summary>
    [Fact]
    public void Parse_Error_TrailingOr()
    {
        Assert.Throws<RsqlSyntaxException>(() => Parse("name==Phone,"));
    }

    /// <summary>
    /// Tests that parsing throws <see cref="RsqlSyntaxException"/> for consecutive logical operators.
    /// </summary>
    [Fact]
    public void Parse_Error_ConsecutiveLogicalOperators()
    {
        Assert.Throws<RsqlSyntaxException>(() => Parse("name==Phone;,status==active"));
    }

    /// <summary>
    /// Tests that parsing throws <see cref="RsqlSyntaxException"/> for unbalanced parentheses (missing closing parenthesis).
    /// </summary>
    [Fact]
    public void Parse_Error_UnbalancedParentheses_MissingClosing()
    {
        Assert.Throws<RsqlSyntaxException>(() => Parse("(name==Phone"));
        Assert.Throws<RsqlSyntaxException>(() => Parse("((name==Phone)"));
    }

    /// <summary>
    /// Tests that parsing throws <see cref="RsqlSyntaxException"/> for unbalanced parentheses (missing opening parenthesis).
    /// </summary>
    [Fact]
    public void Parse_Error_UnbalancedParentheses_MissingOpening()
    {
         Assert.Throws<RsqlSyntaxException>(() => Parse("name==Phone)"));
         Assert.Throws<RsqlSyntaxException>(() => Parse("(name==Phone))"));
    }

    /// <summary>
    /// Tests that parsing throws <see cref="RsqlSyntaxException"/> for empty parentheses.
    /// </summary>
    [Fact]
    public void Parse_Error_EmptyParentheses()
    {
        Assert.Throws<RsqlSyntaxException>(() => Parse("()"));
        Assert.Throws<RsqlSyntaxException>(() => Parse("name==val;()"));
    }

    /// <summary>
    /// Tests the operator precedence when OR and AND operators are used together (OR then AND).
    /// Expects AND to be evaluated before OR.
    /// </summary>
    [Fact]
    public void Parse_Precedence_OrAnd()
    {
        var (where, parameters) = Parse("a==1,b==2;c==3");
        Assert.Equal("WHERE (a = @p0 OR (b = @p1 AND c = @p2))", where);
        Assert.Equal(3, parameters.Count);
    }

    /// <summary>
    /// Tests the operator precedence when AND and OR operators are used together (AND then OR).
    /// Expects AND to be evaluated before OR.
    /// </summary>
    [Fact]
    public void Parse_Precedence_AndOr()
    {
        var (where, parameters) = Parse("a==1;b==2,c==3");
        Assert.Equal("WHERE ((a = @p0 AND b = @p1) OR c = @p2)", where);
        Assert.Equal(3, parameters.Count);
    }

    /// <summary>
    /// Tests parsing a simple RSQL LIKE constraint (=like=).
    /// </summary>
    [Fact]
    public void Parse_SimpleLike()
    {
        var (where, parameters) = Parse("name=like=product%");
        Assert.Equal("WHERE name LIKE @p0", where);
        Assert.Single(parameters);
        Assert.Equal("product%", parameters["@p0"]);
    }

    /// <summary>
    /// Tests parsing a simple RSQL ILIKE constraint (=ilike=).
    /// </summary>
    [Fact]
    public void Parse_SimpleILike()
    {
        var (where, parameters) = Parse("name=ilike=Product%");
        Assert.Equal("WHERE name ILIKE @p0", where);
        Assert.Single(parameters);
        Assert.Equal("Product%", parameters["@p0"]);
    }

    /// <summary>
    /// Tests parsing an RSQL LIKE constraint with a literal value enclosed in quotes.
    /// </summary>
    [Fact]
    public void Parse_Like_WithQuotes()
    {
        var (where, parameters) = Parse("description=like='A%gadget'");
        Assert.Equal("WHERE description LIKE @p0", where);
        Assert.Single(parameters);
        Assert.Equal("A%gadget", parameters["@p0"]);
    }

    /// <summary>
    /// Tests parsing an RSQL ILIKE constraint with a literal value enclosed in quotes.
    /// </summary>
    [Fact]
    public void Parse_ILike_WithQuotes()
    {
        var (where, parameters) = Parse("description=ilike=\"%Gadget%\"");
        Assert.Equal("WHERE description ILIKE @p0", where);
        Assert.Single(parameters);
        Assert.Equal("%Gadget%", parameters["@p0"]);
    }
}
