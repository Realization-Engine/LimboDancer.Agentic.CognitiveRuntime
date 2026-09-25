using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace LimboDancer.Domains.Asl.Units.Rendering.Styles;

public sealed record StyleParseResult(StyleSheet? Sheet, IReadOnlyList<StyleDiagnostic> Diagnostics)
{
    public bool Succeeded => Sheet is not null;
}

/// <summary>
/// Parses the unit style language of Unit Display Design section 3.3: rules of selectors and declarations, and the
/// <c>@tokens</c>, <c>@palette</c>, and <c>@detail far | mid | near</c> blocks. It is our own small parser, not browser
/// CSS. A syntax error refuses the sheet with its line and column; an unknown property is a warning and is ignored.
/// </summary>
public static class StyleSheetParser
{
    public static StyleParseResult Parse(string name, string text)
    {
        ArgumentNullException.ThrowIfNull(name);
        ArgumentNullException.ThrowIfNull(text);
        var normalized = text.ReplaceLineEndings("\n");
        var hash = Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(normalized)));
        var diagnostics = new List<StyleDiagnostic>();
        try
        {
            var tokens = Tokenizer.Tokenize(normalized);
            var parser = new Parser(tokens, normalized, diagnostics);
            var sheet = parser.ParseSheet(name, hash);
            return new StyleParseResult(sheet, diagnostics);
        }
        catch (StyleSyntaxException exception)
        {
            diagnostics.Add(new StyleDiagnostic(StyleDiagnosticSeverity.Error, exception.Message, exception.Line, exception.Column));
            return new StyleParseResult(null, diagnostics);
        }
    }

    private enum TokenKind
    {
        Ident,
        String,
        Number,
        Hash,
        AtKeyword,
        Delim,
        DoubleColon,
        End,
    }

    private sealed record Token(TokenKind Kind, string Text, int Line, int Column, bool SpaceBefore, int Start, int End)
    {
        public bool Is(char delimiter) => Kind == TokenKind.Delim && Text.Length == 1 && Text[0] == delimiter;
    }

    private sealed class StyleSyntaxException(string message, int line, int column) : Exception(message)
    {
        public int Line => line;

        public int Column => column;
    }

    private static class Tokenizer
    {
        public static List<Token> Tokenize(string text)
        {
            var tokens = new List<Token>();
            var index = 0;
            var line = 1;
            var lineStart = 0;
            var space = true;
            while (true)
            {
                // Whitespace and comments.
                while (index < text.Length)
                {
                    if (char.IsWhiteSpace(text[index]))
                    {
                        if (text[index] == '\n')
                        {
                            line++;
                            lineStart = index + 1;
                        }

                        index++;
                        space = true;
                    }
                    else if (text[index] == '/' && index + 1 < text.Length && text[index + 1] == '*')
                    {
                        var close = text.IndexOf("*/", index + 2, StringComparison.Ordinal);
                        if (close < 0)
                        {
                            throw new StyleSyntaxException("The comment is not closed.", line, index - lineStart + 1);
                        }

                        for (var i = index; i < close; i++)
                        {
                            if (text[i] == '\n')
                            {
                                line++;
                                lineStart = i + 1;
                            }
                        }

                        index = close + 2;
                        space = true;
                    }
                    else
                    {
                        break;
                    }
                }

                var column = index - lineStart + 1;
                if (index >= text.Length)
                {
                    tokens.Add(new Token(TokenKind.End, string.Empty, line, column, space, index, index));
                    return tokens;
                }

                var start = index;
                var character = text[index];
                Token token;
                if (character is '"' or '\'')
                {
                    var builder = new StringBuilder();
                    index++;
                    while (true)
                    {
                        if (index >= text.Length || text[index] == '\n')
                        {
                            throw new StyleSyntaxException("The string is not closed.", line, column);
                        }

                        if (text[index] == '\\' && index + 1 < text.Length)
                        {
                            builder.Append(text[index + 1]);
                            index += 2;
                            continue;
                        }

                        if (text[index] == character)
                        {
                            index++;
                            break;
                        }

                        builder.Append(text[index++]);
                    }

                    token = new Token(TokenKind.String, builder.ToString(), line, column, space, start, index);
                }
                else if (IsNumberStart(text, index))
                {
                    index++;
                    while (index < text.Length && (char.IsAsciiDigit(text[index]) || text[index] == '.'))
                    {
                        index++;
                    }

                    var number = text[start..index];
                    if (!double.TryParse(number, NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out _))
                    {
                        throw new StyleSyntaxException($"'{number}' is not a number.", line, column);
                    }

                    token = new Token(TokenKind.Number, number, line, column, space, start, index);
                }
                else if (IsIdentStart(text, index))
                {
                    var name = ReadIdent(text, ref index);
                    token = new Token(TokenKind.Ident, name, line, column, space, start, index);
                }
                else if (character == '#')
                {
                    index++;
                    while (index < text.Length && char.IsAsciiHexDigit(text[index]))
                    {
                        index++;
                    }

                    var hex = text[(start + 1)..index];
                    if (hex.Length is not (3 or 6))
                    {
                        throw new StyleSyntaxException($"'#{hex}' is not a color; write #rgb or #rrggbb.", line, column);
                    }

                    token = new Token(TokenKind.Hash, hex.ToLowerInvariant(), line, column, space, start, index);
                }
                else if (character == '@')
                {
                    index++;
                    if (!IsIdentStart(text, index))
                    {
                        throw new StyleSyntaxException("'@' must start an at-rule such as @detail.", line, column);
                    }

                    var name = ReadIdent(text, ref index);
                    token = new Token(TokenKind.AtKeyword, name, line, column, space, start, index);
                }
                else if (character == ':' && index + 1 < text.Length && text[index + 1] == ':')
                {
                    index += 2;
                    token = new Token(TokenKind.DoubleColon, "::", line, column, space, start, index);
                }
                else if ("{}():;,.[]=|*".Contains(character, StringComparison.Ordinal))
                {
                    index++;
                    token = new Token(TokenKind.Delim, character.ToString(), line, column, space, start, index);
                }
                else
                {
                    throw new StyleSyntaxException($"Unexpected character '{character}'.", line, column);
                }

                tokens.Add(token);
                space = false;
            }
        }

        private static bool IsNumberStart(string text, int index)
        {
            var character = text[index];
            if (char.IsAsciiDigit(character))
            {
                return true;
            }

            var next = index + 1 < text.Length ? text[index + 1] : '\0';
            return (character == '.' && char.IsAsciiDigit(next)) ||
                   (character is '-' or '+' && (char.IsAsciiDigit(next) || (next == '.' && index + 2 < text.Length && char.IsAsciiDigit(text[index + 2]))));
        }

        private static bool IsIdentStart(string text, int index)
        {
            if (index >= text.Length)
            {
                return false;
            }

            var character = text[index];
            if (char.IsAsciiLetter(character) || character is '_' or '\\')
            {
                return true;
            }

            return character == '-' && index + 1 < text.Length && (char.IsAsciiLetter(text[index + 1]) || text[index + 1] is '_' or '-' or '\\');
        }

        private static string ReadIdent(string text, ref int index)
        {
            var builder = new StringBuilder();
            while (index < text.Length)
            {
                var character = text[index];
                if (character == '\\' && index + 1 < text.Length && text[index + 1] != '\n')
                {
                    builder.Append(text[index + 1]);
                    index += 2;
                }
                else if (char.IsAsciiLetterOrDigit(character) || character is '-' or '_')
                {
                    builder.Append(character);
                    index++;
                }
                else
                {
                    break;
                }
            }

            return builder.ToString();
        }
    }

    private sealed class Parser(List<Token> tokens, string text, List<StyleDiagnostic> diagnostics)
    {
        private int position;
        private int order;

        private Token Current => tokens[position];

        public StyleSheet ParseSheet(string name, string hash)
        {
            var rules = new List<StyleRule>();
            var tokenValues = new Dictionary<string, IReadOnlyList<StyleComponent>>(StringComparer.Ordinal);
            string? palette = null;
            while (Current.Kind != TokenKind.End)
            {
                if (Current.Kind == TokenKind.AtKeyword)
                {
                    var at = Next();
                    switch (at.Text)
                    {
                        case "tokens":
                            Expect('{', "'{' after @tokens");
                            while (!Current.Is('}'))
                            {
                                var declaration = ParseDeclaration();
                                tokenValues[declaration.Property] = declaration.Value;
                                if (!Current.Is('}'))
                                {
                                    Expect(';', "';' after a token");
                                }
                            }

                            Next();
                            break;
                        case "palette":
                            if (Current.Kind != TokenKind.Ident)
                            {
                                throw Error("@palette needs the name of a palette set.", Current);
                            }

                            palette = Next().Text;
                            Expect(';', "';' after @palette");
                            break;
                        case "detail":
                            var tierToken = Current;
                            if (Current.Kind != TokenKind.Ident || !TryTier(Current.Text, out var tier))
                            {
                                throw Error("@detail needs far, mid, or near.", tierToken);
                            }

                            Next();
                            Expect('{', "'{' after the detail tier");
                            while (!Current.Is('}'))
                            {
                                if (Current.Kind is TokenKind.End or TokenKind.AtKeyword)
                                {
                                    throw Error("A @detail block holds rules only and must be closed with '}'.", Current);
                                }

                                rules.Add(ParseRule(tier));
                            }

                            Next();
                            break;
                        default:
                            throw Error($"'@{at.Text}' is not an at-rule; use @tokens, @palette, or @detail.", at);
                    }

                    continue;
                }

                rules.Add(ParseRule(null));
            }

            return new StyleSheet(name, tokenValues, rules, palette, diagnostics.Where(d => d.Severity == StyleDiagnosticSeverity.Warning).ToList(), hash);
        }

        private static bool TryTier(string name, out DetailTier tier)
        {
            tier = name switch
            {
                "far" => DetailTier.Far,
                "mid" => DetailTier.Mid,
                _ => DetailTier.Near,
            };
            return name is "far" or "mid" or "near";
        }

        private StyleRule ParseRule(DetailTier? tier)
        {
            var selectors = new List<Selector> { ParseSelector() };
            while (Current.Is(','))
            {
                Next();
                selectors.Add(ParseSelector());
            }

            Expect('{', "'{' after the selector");
            var declarations = new List<Declaration>();
            while (!Current.Is('}'))
            {
                if (Current.Is(';'))
                {
                    Next();
                    continue;
                }

                var declaration = ParseDeclaration();
                if (StyleProperties.Known.Contains(declaration.Property))
                {
                    declarations.Add(declaration);
                }
                else
                {
                    diagnostics.Add(new StyleDiagnostic(StyleDiagnosticSeverity.Warning,
                        $"'{declaration.Property}' is not a known property and is ignored.", declaration.Line, declaration.Column));
                }

                if (!Current.Is('}'))
                {
                    Expect(';', "';' between declarations");
                }
            }

            Next();
            return new StyleRule(selectors, declarations, tier, order++);
        }

        private Declaration ParseDeclaration()
        {
            var property = Current;
            if (property.Kind != TokenKind.Ident)
            {
                throw Error("A declaration must start with a property name.", property);
            }

            Next();
            Expect(':', $"':' after '{property.Text}'");
            var value = new List<StyleComponent>();
            while (!Current.Is(';') && !Current.Is('}'))
            {
                if (Current.Kind == TokenKind.End)
                {
                    throw Error("Expected ';' or '}' after the value.", Current);
                }

                value.Add(ParseComponent());
            }

            if (value.Count == 0)
            {
                throw Error($"'{property.Text}' has no value.", Current);
            }

            return new Declaration(property.Text, value, property.Line, property.Column);
        }

        private StyleComponent ParseComponent()
        {
            var token = Current;
            switch (token.Kind)
            {
                case TokenKind.String:
                    Next();
                    return new StringComponent(token.Text);
                case TokenKind.Number:
                    Next();
                    return new NumberComponent(double.Parse(token.Text, NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture));
                case TokenKind.Hash:
                    Next();
                    var hex = token.Text.Length == 3 ? string.Concat(token.Text.Select(c => $"{c}{c}")) : token.Text;
                    return new ColorComponent("#" + hex);
                case TokenKind.Ident:
                    Next();
                    if (Current.Is('(') && !Current.SpaceBefore)
                    {
                        Next();
                        if (token.Text is not ("attr" or "token" or "side" or "glyph"))
                        {
                            throw Error($"'{token.Text}()' is not a value function; use attr, token, side, or glyph.", token);
                        }

                        var arguments = new List<StyleComponent>();
                        while (!Current.Is(')'))
                        {
                            if (Current.Kind == TokenKind.End || Current.Is(';') || Current.Is('}'))
                            {
                                throw Error($"'{token.Text}(' is not closed.", Current);
                            }

                            arguments.Add(ParseComponent());
                            if (Current.Is(','))
                            {
                                Next();
                            }
                            else if (!Current.Is(')'))
                            {
                                throw Error("Separate function arguments with ','.", Current);
                            }
                        }

                        Next();
                        if (arguments.Count == 0)
                        {
                            throw Error($"'{token.Text}()' needs an argument.", token);
                        }

                        return new FunctionComponent(token.Text, arguments);
                    }

                    return new IdentComponent(token.Text);
                default:
                    throw Error($"'{token.Text}' is not a value.", token);
            }
        }

        private Selector ParseSelector()
        {
            var first = Current;
            var subject = ParseCompound(first);
            CompoundSelector? attached = null;
            string? slot = null;
            while (Current.Kind == TokenKind.DoubleColon)
            {
                NoSpace();
                var colon = Next();
                if (slot is not null)
                {
                    throw Error("Nothing may follow ::slot().", colon);
                }

                var name = Current;
                if (name.Kind != TokenKind.Ident || name.Text is not ("slot" or "attached"))
                {
                    throw Error("Use ::slot(name) or ::attached(selector).", name);
                }

                Next();
                ExpectAdjacent('(', $"'(' after ::{name.Text}");
                if (name.Text == "slot")
                {
                    if (Current.Kind != TokenKind.Ident)
                    {
                        throw Error("::slot() needs a slot name.", Current);
                    }

                    slot = Next().Text;
                }
                else
                {
                    if (attached is not null)
                    {
                        throw Error("A selector may name ::attached() once.", name);
                    }

                    attached = ParseCompound(Current);
                }

                Expect(')', $"')' to close ::{name.Text}(");
            }

            if (!Current.Is(',') && !Current.Is('{'))
            {
                throw Error(Current.SpaceBefore
                    ? "Descendant combinators are not supported; use ::attached() for carried equipment."
                    : $"Unexpected '{Current.Text}' in the selector.", Current);
            }

            var end = tokens[position - 1].End;
            return new Selector(subject, attached, slot, text[first.Start..end]);
        }

        private CompoundSelector ParseCompound(Token first)
        {
            string? kind = null;
            var parts = 0;
            var traits = new List<string>();
            var attributes = new List<AttributeCondition>();
            var states = new List<string>();
            var concealed = false;
            string? face = null;
            if (Current.Is('*'))
            {
                Next();
                parts++;
            }
            else if (Current.Kind == TokenKind.Ident)
            {
                var name = Next().Text;
                if (Current.Is('|') && !Current.SpaceBefore)
                {
                    Next();
                    if (Current.Kind != TokenKind.Ident || Current.SpaceBefore)
                    {
                        throw Error($"'{name}|' needs a kind name.", Current);
                    }

                    kind = name + ":" + Next().Text;
                }
                else
                {
                    kind = name;
                }

                parts++;
            }

            while (true)
            {
                var token = Current;
                if (token.Is('.'))
                {
                    NoSpace(parts);
                    Next();
                    traits.Add(ExpectIdentAdjacent("a trait name after '.'"));
                }
                else if (token.Is('['))
                {
                    NoSpace(parts);
                    Next();
                    var attribute = ExpectIdent("an attribute name after '['");
                    string? value = null;
                    if (Current.Is('='))
                    {
                        Next();
                        var valueToken = Current;
                        if (valueToken.Kind is not (TokenKind.Ident or TokenKind.String or TokenKind.Number))
                        {
                            throw Error("An attribute selector compares with a name, a string, or a number.", valueToken);
                        }

                        value = Next().Text;
                    }

                    Expect(']', "']' to close the attribute selector");
                    attributes.Add(new AttributeCondition(attribute, value));
                }
                else if (token.Is(':'))
                {
                    NoSpace(parts);
                    Next();
                    var name = ExpectIdentAdjacent("a state after ':'");
                    if (name == "concealed")
                    {
                        concealed = true;
                    }
                    else if (name == "face")
                    {
                        ExpectAdjacent('(', "'(' after :face");
                        face = ExpectIdent("a face name in :face()");
                        Expect(')', "')' to close :face(");
                    }
                    else
                    {
                        states.Add(name);
                    }
                }
                else
                {
                    break;
                }

                parts++;
            }

            if (parts == 0)
            {
                throw Error($"'{first.Text}' does not start a selector.", first);
            }

            return new CompoundSelector(kind, traits, attributes, states, concealed, face);
        }

        private void NoSpace(int parts = 1)
        {
            if (parts > 0 && Current.SpaceBefore)
            {
                throw Error("Descendant combinators are not supported; use ::attached() for carried equipment.", Current);
            }
        }

        private string ExpectIdent(string what)
        {
            if (Current.Kind != TokenKind.Ident)
            {
                throw Error($"Expected {what}.", Current);
            }

            return Next().Text;
        }

        private string ExpectIdentAdjacent(string what)
        {
            if (Current.Kind != TokenKind.Ident || Current.SpaceBefore)
            {
                throw Error($"Expected {what}.", Current);
            }

            return Next().Text;
        }

        private void Expect(char delimiter, string what)
        {
            if (!Current.Is(delimiter))
            {
                throw Error($"Expected {what}.", Current);
            }

            Next();
        }

        private void ExpectAdjacent(char delimiter, string what)
        {
            if (!Current.Is(delimiter) || Current.SpaceBefore)
            {
                throw Error($"Expected {what}.", Current);
            }

            Next();
        }

        private Token Next() => tokens[position < tokens.Count - 1 ? position++ : position];

        private static StyleSyntaxException Error(string message, Token token) => new(message, token.Line, token.Column);
    }
}
