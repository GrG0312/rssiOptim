using System.Reflection;
using VisiLib.Args.Parsing;

namespace VisiLib.Args.Binding
{
    /// <summary>
    /// A setting class's parameter discovery model: 
    /// what can be set,
    /// under what name,
    /// with what type,
    /// and what is the default value.
    /// </summary>
    /// 
    /// <remarks>
    /// The model is built from the setting class's properties marked with <see cref="OptionAttribute"/>,
    /// and the appropriate parser is resolved for each property.
    /// </remarks>
    public sealed class OptionModel
    {
        /// <summary>
        /// The list of all parameters, in declaration order.
        /// </summary>
        private readonly List<OptionDescriptor> _options;

        private OptionModel(Type settingsType, List<OptionDescriptor> options)
        {
            SettingsType = settingsType;
            _options = options;
        }

        /// <summary>
        /// The type of the settings class for which this model was built.
        /// </summary>
        public Type SettingsType { get; }

        /// <summary>
        /// The list of all parameters, in declaration order.
        /// </summary>
        public IReadOnlyList<OptionDescriptor> Options => _options;

        /// <summary>
        /// The parameters grouped by category, in the order of their first occurrence.
        /// </summary>
        public IEnumerable<IGrouping<string, OptionDescriptor>> ByCategory => _options.GroupBy(o => o.Category);

        /// <summary>
        /// Builds a parameter model for the given settings class type from the properties marked with <see cref="OptionAttribute"/>.
        /// </summary>
        /// 
        /// <typeparam name="TSettings">
        /// The settings class type for which the parameter model is built. Must have a public parameterless constructor.
        /// </typeparam>
        /// 
        /// <param name="registry">
        /// The parser registry used to resolve parsers for the properties.
        /// </param>
        /// 
        /// <returns>
        /// An <see cref="OptionModel"/> instance containing the discovered parameters and their metadata.
        /// </returns>
        /// 
        /// <exception cref="InvalidOperationException"></exception>
        public static OptionModel For<TSettings>(ParserRegistry registry) where TSettings : new()
        {
            // Validate the input registry
            if (registry is null)
            {
                throw new ArgumentNullException(nameof(registry));
            }

            // Create a default instance of the settings class to retrieve default values
            // and prepare a list to hold the discovered option descriptors
            TSettings defaults = new TSettings();
            List<OptionDescriptor> options = new List<OptionDescriptor>();

            // Retrieve all public instance properties of the settings class,
            // ordered by their metadata token (declaration order)
            IOrderedEnumerable<PropertyInfo> properties = typeof(TSettings)
                .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .OrderBy(p => p.MetadataToken);

            // Iterate through each property to discover options
            foreach (PropertyInfo property in properties)
            {
                // Check if the property has the OptionAttribute
                OptionAttribute? attribute = property.GetCustomAttribute<OptionAttribute>();
                if (attribute is null) continue;

                // Ensure the property is both readable and writable
                if (!property.CanWrite || !property.CanRead)
                {
                    throw new InvalidOperationException($"The {typeof(TSettings).Name}.{property.Name} parameter must be readable and writeable.");
                }

                // Resolve the appropriate parser for the property, either from the attribute or the registry
                IValueParser parser = ResolveParser(registry, property, attribute);
                // Create an OptionDescriptor for the property, including its default value
                OptionDescriptor descriptor = new OptionDescriptor(property, attribute, parser, property.GetValue(defaults));

                // Check for name clashes with existing options
                OptionDescriptor? clash = options.FirstOrDefault(o => o.Matches(descriptor.Name));
                if (clash is not null)
                {
                    throw new InvalidOperationException($"The {typeof(TSettings).Name}.{property.Name} parameter name '{descriptor.Name}' clashes with {clash.Name}.");
                }

                options.Add(descriptor);
            }

            if (options.Count == 0)
            {
                throw new InvalidOperationException($"The {typeof(TSettings).Name} class does not contain any [Option] property.");
            }

            return new OptionModel(typeof(TSettings), options);
        }

        /// <summary>
        /// Tries to find a parameter by <paramref name="name"/>, returning true if found, false otherwise.
        /// </summary>
        /// 
        /// <param name="name">
        /// The name of the parameter to search for.
        /// </param>
        /// 
        /// <param name="descriptor">
        /// When the method returns, contains the <see cref="OptionDescriptor"/> of the found parameter if successful; otherwise, null.
        /// </param>
        /// 
        /// <returns>
        /// True if a parameter with the specified name was found; otherwise, false.
        /// </returns>
        public bool TryFind(string name, out OptionDescriptor descriptor)
        {
            descriptor = _options.FirstOrDefault(o => o.Matches(name))!;
            return descriptor is not null;
        }

        /// <summary>
        /// Finds a parameter by <paramref name="name"/>. If not found, throws a <see cref="VisiArgException"/> with a suggestion.
        /// </summary>
        /// 
        /// <param name="name">
        /// The name of the parameter to search for.
        /// </param>
        /// 
        /// <returns>
        /// The <see cref="OptionDescriptor"/> of the found parameter.
        /// </returns>
        /// 
        /// <exception cref="VisiArgException"></exception>
        public OptionDescriptor Find(string name)
        {
            if (TryFind(name, out OptionDescriptor descriptor))
            {
                return descriptor;
            }

            throw new VisiArgException($"No parameter named '{name}'.", SuggestName(name));
        }

        /// <summary>
        /// Suggests a parameter name based on the provided <paramref name="name"/>.
        /// If similar names are found, they are returned as suggestions;
        /// otherwise, a message directing to the full list is returned.
        /// </summary>
        /// 
        /// <param name="name">
        /// The name of the parameter for which to suggest alternatives.
        /// </param>
        /// 
        /// <returns>
        /// A suggestion string containing similar parameter names or a message directing to the full list.
        /// </returns>
        private string? SuggestName(string name)
        {
            List<string> close = _options
                // Find options whose names contain the input name or vice versa, ignoring case
                .Where(o => o.Name.Contains(name, StringComparison.OrdinalIgnoreCase) ||
                            name.Contains(o.Name, StringComparison.OrdinalIgnoreCase))
                // Order the suggestions by the length of the option name (shorter names first)
                .Select(o => o.Name)
                .ToList();

            return close.Count > 0
                ? $"Maybe you were thinking about these: {string.Join(", ", close)}"
                : "For a full list: 'help param'";
        }

        /// <summary>
        /// Resolves the appropriate parser for a given property, either from the <see cref="OptionAttribute"/> or from the <paramref name="registry"/>.
        /// </summary>
        /// 
        /// <param name="registry">
        /// The parser registry used to resolve parsers for the properties.
        /// </param>
        /// 
        /// <param name="property">
        /// The property for which to resolve a parser.
        /// </param>
        /// 
        /// <param name="attribute">
        /// The <see cref="OptionAttribute"/> associated with the property, which may specify a custom parser.
        /// </param>
        /// 
        /// <returns>
        /// The resolved <see cref="IValueParser"/> for the property.
        /// </returns>
        /// 
        /// <exception cref="InvalidOperationException"></exception>
        private static IValueParser ResolveParser(
            ParserRegistry registry,
            PropertyInfo property,
            OptionAttribute attribute)
        {
            // If the attribute specifies a custom parser, use it
            if (attribute.Parser is not null)
            {
                return registry.GetOrCreate(attribute.Parser);
            }

            // Otherwise, try to resolve a parser from the registry based on the property's type
            if (registry.TryResolve(property.PropertyType, out var parser))
            {
                return parser;
            }

            throw new InvalidOperationException(
                $"The {property.DeclaringType!.Name}.{property.Name} parameter type '{property.PropertyType.Name}' does not have a registered parser.");
        }
    }
}
