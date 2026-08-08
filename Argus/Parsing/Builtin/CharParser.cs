namespace VisiLib.Args.Parsing.Builtin
{
    /// <summary>
    /// Parses a single character. Also accepts common punctuation names (tab, space, semicolon, comma)
    /// because characters like tab or space cannot be typed directly at the prompt.
    /// </summary>
    public sealed class CharParser : ValueParser<char>
    {
        /// <inheritdoc />
        public override string TypeName => "character";

        /// <inheritdoc />
        protected override ParseResult ParseCore(string text)
        {
            return text.ToLowerInvariant() switch
            {
                "tab" or "\\t" => ParseResult.Ok('\t'),
                "space" => ParseResult.Ok(' '),
                "semicolon" => ParseResult.Ok(';'),
                "comma" => ParseResult.Ok(','),
                _ => text.Length == 1
                    ? ParseResult.Ok(text[0])
                    : ParseResult.Fail(
                        $"'{text}' is not a single character. You can also use: tab, space, semicolon, comma.")
            };
        }

        /// <inheritdoc />
        public override string Format(object? value) => value switch
        {
            '\t' => "tab",
            ' ' => "space",
            char c => c.ToString(),
            _ => "(none)"
        };
    }
}
