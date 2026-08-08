namespace VisiLib.Args.Parsing
{
    /// <summary>
    /// Represents a parser that can convert a string into a value of a specific type and vice versa.
    /// </summary>
    /// 
    /// <remarks>
    /// This is the extension point of the library. To support a new type, you don't need to modify either
    /// the binding or the shell: it is enough to write a descendant of <see cref="ValueParser{T}"/> and register it in the <see cref="ParserRegistry"/>,
    /// or directly apply it to a property using the <see cref="OptionAttribute.Parser"/> attribute.
    /// </remarks>
    public interface IValueParser
    {
        /// <summary>
        /// The target type that this parser can convert to and from.
        /// </summary>
        public Type TargetType { get; }

        /// <summary>
        /// The name of the target type, used for help and error messages.
        /// It is recommended to use a short, lowercase name.
        /// </summary>
        public string TypeName { get; }

        /// <summary>
        /// List of possible values for the target type, used for help and error messages.
        /// If empty, the parser does not provide suggestions.
        /// </summary>
        public IReadOnlyList<string> Suggestions { get; }

        /// <summary>
        /// Parses the given <paramref name="text"/> into a value of the target type.
        /// </summary>
        public ParseResult Parse(string text);

        /// <summary>
        /// Formats the given <paramref name="value"/> of the target type into a string.
        /// The <paramref name="value"/> must be of the type specified by <see cref="TargetType"/>.
        /// </summary>
        public string Format(object? value);
    }
}
