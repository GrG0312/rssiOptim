namespace VisiLib.Args.Parsing.Builtin
{
    /// <summary>
    /// Parses a plain text value. Returns the trimmed, unquoted text as-is.
    /// </summary>
    public sealed class StringParser : ValueParser<string>
    {
        /// <inheritdoc />
        public override string TypeName => "text";

        /// <inheritdoc />
        protected override ParseResult ParseCore(string text) => ParseResult.Ok(text);

        /// <inheritdoc />
        public override string Format(object? value)
        {
            return value is string s && s.Length > 0 ? s : "(none)";
        }
    }
}
