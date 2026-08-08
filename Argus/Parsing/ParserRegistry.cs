using VisiLib.Args.Parsing.Builtin;

namespace VisiLib.Args.Parsing
{
    /// <summary>
    /// Represents a registry that maps target types to their corresponding value parsers.
    /// </summary>
    ///
    /// <remarks>
    /// The registration is chainable, allowing for a concise expression to assemble the complete set of types for the program:
    /// <code>
    /// ParserRegistry registry = ParserRegistry.CreateDefault()
    ///     .Register(new TimeSpanParser())
    ///     .Register("MAC address", MacAddress.Parse);
    /// </code>
    /// </remarks>
    public sealed class ParserRegistry
    {
        /// <summary>
        /// Dictionary for mapping target types to their corresponding parsers.
        /// </summary>
        private readonly Dictionary<Type, IValueParser> _byTargetType = [];

        /// <summary>
        /// Dictionary for mapping parser types to their corresponding parser instances.
        /// </summary>
        private readonly Dictionary<Type, IValueParser> _byParserType = [];

        /// <summary>
        /// Initializes a new instance of the <see cref="ParserRegistry"/> class.
        /// Most of the time, you will want to use <see cref="CreateDefault"/> instead of this constructor.
        /// </summary>
        public ParserRegistry() { }

        /// <summary>
        /// Creates a default <see cref="ParserRegistry"/> instance with built-in parsers for common types.
        /// </summary>
        ///
        /// <remarks>
        /// The default registry includes parsers for the following types:
        /// <list type="bullet">
        /// <item><description><see cref="string"/></description></item>
        /// <item><description><see cref="int"/></description></item>
        /// <item><description><see cref="double"/></description></item>
        /// <item><description><see cref="bool"/></description></item>
        /// <item><description><see cref="char"/></description></item>
        /// </list>
        /// </remarks>
        ///
        /// <returns>
        /// A <see cref="ParserRegistry"/> instance containing built-in parsers for common types.
        /// </returns>
        public static ParserRegistry CreateDefault() =>
            new ParserRegistry()
                .Register(new StringParser())
                .Register(new Int32Parser())
                .Register(new DoubleParser())
                .Register(new BooleanParser())
                .Register(new CharParser());

        /// <summary>
        /// Registers a value parser for its target type in the registry.
        /// If a parser for the same target type already exists, it will be replaced with the new one.
        /// </summary>
        ///
        /// <param name="parser">
        /// The value parser to be registered. Must not be null.
        /// </param>
        ///
        /// <returns>
        /// The current <see cref="ParserRegistry"/> instance, allowing for method chaining.
        /// </returns>
        public ParserRegistry Register(IValueParser parser)
        {
            if (parser is null)
            {
                throw new ArgumentNullException(nameof(parser));
            }

            _byTargetType[parser.TargetType] = parser;
            _byParserType[parser.GetType()] = parser;
            return this;
        }

        /// <summary>
        /// Registers a value parser for a specific target type using a delegate function for conversion.
        /// </summary>
        ///
        /// <param name="typeName">
        /// The name of the target type, used for error messages and display purposes.
        /// </param>
        ///
        /// <param name="convert">
        /// A delegate function that takes a string input and converts it to the target type <typeparamref name="T"/>.
        /// </param>
        public ParserRegistry Register<T>(string typeName, Func<string, T> convert)
        {
            return Register(new DelegateParser<T>(typeName, convert));
        }

        /// <summary>
        /// Attempts to resolve a value parser for the specified target type.
        /// </summary>
        ///
        /// <remarks>
        /// Unwraps the <c>Nullable&lt;T&gt;</c> wrapper and generates (and stores) an <see cref="EnumParser{TEnum}"/> instance for enums on demand.
        /// </remarks>
        ///
        /// <param name="targetType">
        /// The target type for which to resolve a value parser.
        /// </param>
        ///
        /// <param name="parser">
        /// When this method returns, contains the resolved value parser if found; otherwise, <see langword="null"/>.
        /// </param>
        ///
        /// <returns>
        /// <see langword="true"/> if a value parser was found for the specified target type; otherwise, <see langword="false"/>.
        /// </returns>
        public bool TryResolve(Type targetType, out IValueParser? parser)
        {
            // Unwrap Nullable<T> to get the underlying type
            Type type = Nullable.GetUnderlyingType(targetType) ?? targetType;

            if (_byTargetType.TryGetValue(type, out IValueParser? found))
            {
                parser = found;
                return true;
            }

            // For enum types, auto-generate and cache an EnumParser<T> instance
            if (type.IsEnum)
            {
                IValueParser created = (IValueParser)Activator.CreateInstance(
                    typeof(EnumParser<>).MakeGenericType(type))!;

                _byTargetType[type] = created;
                parser = created;
                return true;
            }

            parser = null;
            return false;
        }

        /// <summary>
        /// Gets an existing parser instance for the specified parser type, or creates a new one if it doesn't exist.
        /// </summary>
        ///
        /// <remarks>
        /// If the class / type is already registered, it will return the existing instance.
        /// If not, it will create a new instance using the parameterless constructor and store it in the registry.
        /// </remarks>
        public IValueParser GetOrCreate(Type parserType)
        {
            if (_byParserType.TryGetValue(parserType, out IValueParser? existing))
            {
                return existing;
            }

            if (!typeof(IValueParser).IsAssignableFrom(parserType))
            {
                throw new InvalidOperationException($"{parserType.Name} does not implement the IValueParser interface.");
            }

            IValueParser? instance = Activator.CreateInstance(parserType) as IValueParser;
            if (instance is null)
            {
                throw new InvalidOperationException($"{parserType.Name} must have a parameterless constructor or must be registered in the ParserRegistry");
            }

            _byParserType[parserType] = instance;
            return instance;
        }

        /// <summary>
        /// The simple parser behind <see cref="Register{T}(string, Func{string, T})"/>.
        /// Wraps a delegate function to act as a full <see cref="ValueParser{T}"/>.
        /// </summary>
        private sealed class DelegateParser<T> : ValueParser<T>
        {
            /// <summary>
            /// The conversion function that transforms a string into the target type.
            /// </summary>
            private readonly Func<string, T> _convert;

            /// <inheritdoc />
            public override string TypeName { get; }

            /// <summary>
            /// Initializes a new <see cref="DelegateParser{T}"/> with the given type name and conversion function.
            /// </summary>
            ///
            /// <param name="typeName">
            /// The display name for this type, shown in help and error messages.
            /// </param>
            ///
            /// <param name="convert">
            /// The delegate that performs the actual string-to-<typeparamref name="T"/> conversion.
            /// </param>
            public DelegateParser(string typeName, Func<string, T> convert)
            {
                TypeName = typeName;
                _convert = convert;
            }

            /// <inheritdoc />
            protected override ParseResult ParseCore(string text)
            {
                try
                {
                    return ParseResult.Ok(_convert(text));
                }
                catch (Exception ex) when (ex is FormatException or ArgumentException or OverflowException)
                {
                    return ParseResult.Fail(ex.Message);
                }
            }
        }
    }
}
