namespace VisiLib.Args.Parsing
{
    /// <summary>
    /// The result of a parsing operation.
    /// It indicates whether the parsing was successful, and if so, provides the parsed value;
    /// if not, it provides an error message.
    /// </summary>
    /// 
    /// <remarks>
    /// Parsers will return a <see cref="ParseResult"/> to indicate the outcome of their parsing logic,
    /// and will not throw exceptions for expected parsing failures.
    /// This allows for a more functional approach to error handling in parsing scenarios.
    /// </remarks>
    public readonly record struct ParseResult
    {
        /// <summary>
        /// Indicates whether the parsing was successful.
        /// </summary>
        public bool Success { get; }

        /// <summary>
        /// The parsed value if the parsing was successful; otherwise, null.
        /// </summary>
        public object? Value { get; }

        /// <summary>
        /// The error message if the parsing failed; otherwise, null.
        /// </summary>
        public string? Error { get; }

        private ParseResult(bool success, object? value, string? error)
        {
            Success = success;
            Value = value;
            Error = error;
        }

        /// <summary>
        /// Creates a successful parse result with the given value.
        /// </summary>
        public static ParseResult Ok(object? value) => new ParseResult(true, value, null);

        /// <summary>
        /// Creates a failed parse result with the given error message.
        /// </summary>
        public static ParseResult Fail(string error) => new ParseResult(false, null, error);
    }
}
