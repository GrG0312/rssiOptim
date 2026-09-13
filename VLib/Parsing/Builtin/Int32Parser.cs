using System.Globalization;

namespace VLib.Args.Parsing.Builtin
{
    /// <summary>
    /// Parses a whole number (32-bit signed integer) using invariant culture.
    /// </summary>
    public sealed class Int32Parser : ValueParser<int>
    {
        /// <inheritdoc />
        public override string TypeName => "egész szám";

        /// <inheritdoc />
        protected override ParseResult ParseCore(string text)
        {
            if (int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out int value))
            {
                return ParseResult.Ok(value);
            }
            return ParseResult.Fail($"a(z) '{text}' nem érvényes egész szám.");
        }
    }
}
