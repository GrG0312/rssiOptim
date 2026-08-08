namespace VisiLib.Args.Parsing.Builtin
{
    /// <summary>
    /// Parses a boolean value. Deliberately accepts many spellings (English and Hungarian)
    /// so the user doesn't have to guess which form the program expects.
    /// </summary>
    public sealed class BooleanParser : ValueParser<bool>
    {
        /// <summary>
        /// All accepted truthy spellings (case-insensitive).
        /// </summary>
        private static readonly string[] TrueValues = ["true", "igen", "i", "yes", "y", "on", "be", "1"];

        /// <summary>
        /// All accepted falsy spellings (case-insensitive).
        /// </summary>
        private static readonly string[] FalseValues = ["false", "nem", "n", "no", "off", "ki", "0"];

        /// <inheritdoc />
        public override string TypeName => "yes/no";

        /// <inheritdoc />
        public override IReadOnlyList<string> Suggestions => ["yes", "no"];

        /// <inheritdoc />
        protected override ParseResult ParseCore(string text)
        {
            string normalized = text.Trim().ToLowerInvariant();

            if (TrueValues.Contains(normalized)) return ParseResult.Ok(true);
            if (FalseValues.Contains(normalized)) return ParseResult.Ok(false);

            return ParseResult.Fail($"'{text}' cannot be interpreted as a yes/no value.");
        }

        /// <inheritdoc />
        public override string Format(object? value)
        {
            return value is true ? "yes" : "no";
        }
    }
}
