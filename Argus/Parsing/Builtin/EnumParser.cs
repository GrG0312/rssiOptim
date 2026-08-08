namespace VisiLib.Args.Parsing.Builtin
{
    /// <summary>
    /// Parses any enum value from its member names, case-insensitively.
    /// </summary>
    ///
    /// <remarks>
    /// You do not need to register this parser manually: <see cref="ParserRegistry"/> automatically
    /// creates an instance for every enum property it encounters.
    /// </remarks>
    ///
    /// <typeparam name="TEnum">The enum type to parse.</typeparam>
    public sealed class EnumParser<TEnum> : ValueParser<TEnum> where TEnum : struct, Enum
    {
        /// <inheritdoc />
        public override string TypeName => string.Join(" | ", Suggestions);

        /// <inheritdoc />
        public override IReadOnlyList<string> Suggestions { get; } =
            Enum.GetNames<TEnum>().Select(n => n.ToLowerInvariant()).ToArray();

        /// <inheritdoc />
        protected override ParseResult ParseCore(string text)
        {
            // Try to parse the text as an enum member name (case-insensitive)
            // and verify that the parsed value is actually defined in the enum
            if (Enum.TryParse<TEnum>(text, ignoreCase: true, out TEnum value) && Enum.IsDefined(value))
            {
                return ParseResult.Ok(value);
            }

            return ParseResult.Fail(
                $"'{text}' is not recognized. Available: {string.Join(", ", Suggestions)}.");
        }

        /// <inheritdoc />
        public override string Format(object? value)
        {
            return value?.ToString()?.ToLowerInvariant() ?? "(none)";
        }
    }
}
