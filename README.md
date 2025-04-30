# Simple Rsql Parse

[![NuGet](https://img.shields.io/nuget/v/SimpleRsqlParse.svg)](https://www.nuget.org/packages/SimpleRsqlParse/)

`SimpleRsqlParse` é uma biblioteca .NET para parseamento da sintaxe [RSQL](https://github.com/jirutka/rsql-parser), com o objetivo de facilitar a construção de consultas SQL seguras e parametrizadas a partir de filtros dinâmicos.

> ⚠️ **Aviso:** Esta versão suporta apenas a geração de cláusulas `WHERE` para **SQL Server** e **ainda não há suporte a LINQ**. O projeto está em estágio inicial, mas será evoluído com mais funcionalidades futuramente.

> 📌 **Nota:** Esta biblioteca foi inspirada na excelente [rsql-parser](https://github.com/jirutka/rsql-parser) escrita em Java.

## Instalação

Você pode instalar o pacote via NuGet:

```bash
dotnet add package SimpleRsqlParse
```

## Exemplo de uso

```csharp
using SimpleRsqlParse;

string rsql = "name==john;age=gt=30";

var parser = new RsqlParser();
var result = parser.Parse(rsql);

Console.WriteLine("WHERE " + result.SqlWhereClause);
// Saída esperada: WHERE [name] = @p0 AND [age] > @p1

foreach (var param in result.Parameters)
{
    Console.WriteLine($"{param.Key} = {param.Value}");
    // @p0 = "john"
    // @p1 = 30
}
```

## Recursos atuais

- Suporte à sintaxe básica RSQL (comparações, AND/OR).
- Suporte a aliases de campos.
- Geração de cláusulas WHERE seguras e parametrizadas (evita SQL Injection).
- Compatível com SQL Server.

## Regras e Semântica

A seguir está a especificação gramatical da RSQL usada neste projeto, escrita em notação EBNF (ISO 14977):

Uma expressão RSQL é composta por uma ou mais comparações, relacionadas por operadores lógicos:

- **AND lógico**: `;` ou `and`
- **OR lógico**: `,` ou `or`

Por padrão, o operador AND tem precedência (ou seja, é avaliado antes de qualquer operador OR). No entanto, expressões entre parênteses podem ser usadas para alterar a precedência.

```ebnf
input          = or, EOF;
or             = and, { "," , and };
and            = constraint, { ";" , constraint };
constraint     = ( group | comparison );
group          = "(", or, ")";
comparison     = selector, comparison-op, arguments;
selector       = unreserved;
```

O selector identifica um campo (ou atributo, elemento etc.) da entidade a ser filtrada. Pode ser qualquer string Unicode não vazia que não contenha caracteres reservados ou espaços.

Os operadores de comparação seguem a notação FIQL, com algumas alternativas:

- Igual a: `==`
- Diferente de: `!=`
- Menor que: `=lt=` ou `<`
- Menor ou igual a: `=le=` ou `<=`
- Maior que: `=gt=` ou `>`
- Maior ou igual a: `=ge=` ou `>=`
- Dentro da lista (IN): `=in=`
- Fora da lista (NOT IN): `=out=`

## Contribuições

Sinta-se à vontade para abrir _issues_ ou enviar _pull requests_. O projeto ainda está em desenvolvimento e sua colaboração é bem-vinda!

## Licença

Este projeto está licenciado sob a [Licença Apache 2.0](LICENSE).

