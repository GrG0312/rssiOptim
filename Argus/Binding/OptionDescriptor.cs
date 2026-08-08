using System.Reflection;
using VisiLib.Args.Parsing;

namespace VisiLib.Args.Binding
{
    /// <summary>
    /// Represents a single discovered parameter: everything described by the <see cref="OptionAttribute"/>,
    /// </summary>
    public sealed class OptionDescriptor
    {
        /// <summary>
        /// The reflected property that this parameter corresponds to. Used for getting and setting the value on the settings instance.
        /// </summary>
        private readonly PropertyInfo _property;

        internal OptionDescriptor(
            PropertyInfo property,
            OptionAttribute attribute,
            IValueParser parser,
            object? defaultValue)
        {
            _property = property;
            Parser = parser;
            DefaultValue = defaultValue;
            Name = attribute.Name;
            Aliases = attribute.Aliases;
            Help = attribute.Help;
            Category = attribute.Category;
        }

        /// <summary>
        /// The name of the parameter as specified in the <see cref="OptionAttribute"/>.
        /// This is the primary name used to identify the parameter in command-line arguments.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// The list of alternative names (aliases) for the parameter as specified in the <see cref="OptionAttribute"/>.
        /// </summary>
        public IReadOnlyList<string> Aliases { get; }

        /// <summary>
        /// The help text for the parameter as specified in the <see cref="OptionAttribute"/>.
        /// </summary>
        public string Help { get; }

        /// <summary>
        /// The category of the parameter as specified in the <see cref="OptionAttribute"/>.
        /// </summary>
        public string Category { get; }

        /// <summary>
        /// The parser used to convert between the parameter's string representation and its actual type.
        /// </summary>
        public IValueParser Parser { get; }

        /// <summary>
        /// The default value of the parameter as specified in the <see cref="OptionAttribute"/>.
        /// </summary>
        public object? DefaultValue { get; }

        /// <summary>
        /// Indicates whether the parameter is a boolean flag (i.e., its type is <see cref="bool"/>).
        /// </summary>
        public bool IsFlag => _property.PropertyType == typeof(bool);

        /// <summary>
        /// Determines whether the given name matches this parameter's name or any of its aliases, ignoring case.
        /// </summary>
        /// 
        /// <param name="name">
        /// The name to check against this parameter's name and aliases.
        /// </param>
        /// 
        /// <returns>
        /// <see langword="true"/> if the given name matches this parameter's name or any of its aliases; otherwise, <see langword="false"/>.
        /// </returns>
        public bool Matches(string name)
        {
            // If the name matches the primary name OR any of the aliases, return true; otherwise, return false.
            return string.Equals(Name, name, StringComparison.OrdinalIgnoreCase) ||
                   Aliases.Any(a => string.Equals(a, name, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Gets the current value of the parameter from the given target object.
        /// </summary>
        /// 
        /// <param name="target">
        /// The object instance from which to retrieve the parameter's value.
        /// </param>
        /// 
        /// <returns>
        /// The current value of the parameter, or <see langword="null"/> if the value is not set.
        /// </returns>
        public object? GetValue(object target) => _property.GetValue(target);

        /// <summary>
        /// Sets the value of the parameter on the given target object by parsing the provided text.
        /// </summary>
        /// 
        /// <param name="target">
        /// The object instance on which to set the parameter's value.
        /// </param>
        /// 
        /// <param name="text">
        /// The text representation of the value to set for the parameter.
        /// This text will be parsed using the associated <see cref="Parser"/>.
        /// </param>
        /// 
        /// <exception cref="VisiArgException"></exception>
        public void SetFromText(object target, string text)
        {
            ParseResult result = Parser.Parse(text);

            if (!result.Success)
            {
                throw new VisiArgException($"Invalid value for parameter '{Name}': {result.Error}", SuggestionHint(result.Error));
            }

            // If the parsed value is null and the property type is a non-nullable value type, throw an exception.
            if (result.Value is null && _property.PropertyType.IsValueType && Nullable.GetUnderlyingType(_property.PropertyType) is null)
            {
                throw new VisiArgException($"'{Name}' cannot be empty.");
            }

            _property.SetValue(target, result.Value);
        }

        /// <summary>
        /// Provides a suggestion hint based on the parser's suggestions if the provided error message does not already contain the first suggestion.
        /// </summary>
        /// 
        /// <param name="error">
        /// The error message that may contain the first suggestion. If it does, no additional hint is provided.
        /// </param>
        /// 
        /// <returns>
        /// A suggestion hint string if there are suggestions and the error message does not contain the first suggestion; otherwise, <see langword="null"/>.
        /// </returns>
        private string? SuggestionHint(string? error)
        {
            if (Parser.Suggestions.Count == 0) return null;
            if (error is not null && error.Contains(Parser.Suggestions[0], StringComparison.Ordinal))
            {
                return null;
            }

            return $"Possible values: {string.Join(", ", Parser.Suggestions)}";
        }

        /// <summary>
        /// Resets the parameter's value on the given target object to its default value.
        /// </summary>
        /// 
        /// <param name="target">
        /// The object instance on which to reset the parameter's value to its default.
        /// </param>
        public void Reset(object target) => _property.SetValue(target, DefaultValue);

        /// <summary>
        /// Formats the current value of the parameter on the given target object using the associated <see cref="Parser"/>.
        /// </summary>
        /// 
        /// <param name="target">
        /// The object instance from which to retrieve and format the parameter's current value.
        /// </param>
        /// 
        /// <returns>
        /// A string representation of the current value of the parameter, formatted using the associated <see cref="Parser"/>.
        /// </returns>
        public string FormatCurrent(object target) => Parser.Format(GetValue(target));

        /// <summary>
        /// Formats the default value of the parameter using the associated <see cref="Parser"/>.
        /// </summary>
        /// 
        /// <returns>
        /// A string representation of the default value of the parameter, formatted using the associated <see cref="Parser"/>.
        /// </returns>
        public string FormatDefault() => Parser.Format(DefaultValue);

        /// <summary>
        /// Determines whether the current value of the parameter on the given target object is equal to its default value.
        /// </summary>
        /// 
        /// <param name="target">
        /// The object instance from which to retrieve the parameter's current value for comparison with the default value.
        /// </param>
        /// 
        /// <returns>
        /// <see langword="true"/> if the current value of the parameter is equal to its default value; otherwise, <see langword="false"/>.
        /// </returns>
        public bool IsAtDefault(object target) => Equals(GetValue(target), DefaultValue);
    }
}
