namespace VisiLib.Args.Parsing
{
    /// <summary>
    /// A handy base class for custom parsers: it handles returning <see cref="IValueParser.TargetType"/>
    /// and provides a typed value to the derived class.
    /// </summary>
    /// 
    /// <typeparam name="T">
    /// The type of the value that this parser can parse.
    /// </typeparam>
    public abstract class ValueParser<T> : IValueParser
    {
        /// <inheritdoc />
        public Type TargetType => typeof(T);

        /// <inheritdoc />
        public abstract string TypeName { get; }

        /// <inheritdoc />
        public virtual IReadOnlyList<string> Suggestions => [];

        /// <summary>
        /// Parses the given text into a value of type <typeparamref name="T"/>.
        /// </summary>
        /// 
        /// <remarks>
        /// <paramref name="text"/> must not be null, but can be empty.
        /// </remarks>
        /// 
        /// <param name="text">
        /// The text to parse. Must not be null but can be empty.
        /// </param>
        /// 
        /// <returns>
        /// A <see cref="ParseResult"/> indicating whether the parsing was successful or not.
        /// </returns>
        protected abstract ParseResult ParseCore(string text);

        /// <inheritdoc />
        public ParseResult Parse(string text) => ParseCore(text);

        /// <inheritdoc />
        public virtual string Format(object? value) => value?.ToString() ?? "(none)";
    }
}
