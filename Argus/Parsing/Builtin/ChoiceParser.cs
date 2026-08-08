namespace VisiLib.Args.Parsing.Builtin
{
    /// <summary>
    /// A <see cref="string"/> parser which only accepts a predefined set of values, optionally with aliases and a "none" keyword.
    /// </summary>
    /// 
    /// <remarks>
    /// With subclassing, you can create a parser for a specific set of values. For example:
    /// <code>
    /// sealed class FruitParser() : ChoiceParser(["apple", "pear", "peach"]);
    /// </code>
    /// </remarks>
    public class ChoiceParser : ValueParser<string>
    {
        private const string DEFAULT_NONE_KEYWORD = "none";
        /// <summary>
        /// Accepted canonical values. This is displayed in the help.
        /// </summary>
        private readonly Dictionary<string, string> _aliases;

        /// <summary>
        /// Keyword that maps to <see langword="null"/> - thus expressing the "not set" state for a textual setting.
        /// </summary>
        private readonly string _noneKeyword;

        /// <summary>
        /// Initializes a new instance of the <see cref="ChoiceParser"/> class with the specified allowed values, optional aliases, and an optional "none" keyword.
        /// </summary>
        /// 
        /// <param name="allowed">
        /// The set of allowed canonical values. This is displayed in the help.
        /// </param>
        /// 
        /// <param name="aliases">
        /// <inheritdoc cref="_aliases" path="/summary"/>
        /// </param>
        /// 
        /// <param name="noneKeyword">
        /// <inheritdoc cref="_noneKeyword" path="/summary"/>
        /// </param>
        public ChoiceParser(
            IEnumerable<string> allowed,
            IReadOnlyDictionary<string, string>? aliases = null,
            string? noneKeyword = DEFAULT_NONE_KEYWORD)
        {
            Suggestions = allowed.ToArray();
            _noneKeyword = noneKeyword ?? DEFAULT_NONE_KEYWORD;

            if (aliases is null)
            {
                _aliases = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            }
            else
            {
                _aliases = new Dictionary<string, string>(aliases, StringComparer.OrdinalIgnoreCase);
            }
        }

        /// <inheritdoc />
        public override IReadOnlyList<string> Suggestions { get; }

        /// <inheritdoc />
        public override string TypeName => string.Join(" | ", _noneKeyword is null ? Suggestions : [_noneKeyword, .. Suggestions]);

        /// <inheritdoc />
        protected override ParseResult ParseCore(string text)
        {
            string value = text.Trim();

            if (_noneKeyword is not null &&
                string.Equals(value, _noneKeyword, StringComparison.OrdinalIgnoreCase))
            {
                return ParseResult.Ok(null);
            }

            if (_aliases.TryGetValue(value, out var canonical))
            {
                return ParseResult.Ok(canonical);
            }

            string? match = Suggestions.FirstOrDefault(s => string.Equals(s, value, StringComparison.OrdinalIgnoreCase));

            // If the input matches one of the allowed values (case-insensitive), return it.
            if (match is not null)
            {
                return ParseResult.Ok(match);
            }
            else
            {
                return ParseResult.Fail($"'{text}' is not valid. Available: {TypeName.Replace(" | ", ", ")}.");
            }
        }

        /// <inheritdoc />
        public override string Format(object? value)
        {
            return value as string ?? _noneKeyword;
        }
    }
}
