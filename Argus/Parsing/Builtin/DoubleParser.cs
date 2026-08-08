using System.Globalization;

namespace VisiLib.Args.Parsing.Builtin
{
    /// <summary>
    /// Parses a floating-point number using invariant culture.
    /// Accepts both comma and dot as decimal separators, because on Hungarian keyboards
    /// the comma is more convenient, while the code always uses invariant culture internally.
    /// </summary>
    public sealed class DoubleParser : ValueParser<double>
    {
        /// <inheritdoc />
        public override string TypeName => "number";

        /// <inheritdoc />
        protected override ParseResult ParseCore(string text)
        {
            // Replace comma with dot to support Hungarian keyboard input, then trim whitespace
            text = text.Replace(',', '.').Trim();
            if (double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out double value))
            {
                return ParseResult.Ok(value);
            }
            return ParseResult.Fail($"'{text}' is not a valid number.");
        }

        /// <inheritdoc />
        public override string Format(object? value)
        {
            return value is double d ? d.ToString("0.####", CultureInfo.InvariantCulture) : "(none)";
        }
    }
}
