namespace VisiLib.Args
{
    /// <summary>
    /// <para>
    /// Creates a command line option from a property.
    /// </para>
    ///
    /// Usage example:
    ///
    /// <code>
    /// [Option("nmax", Aliases = new[] { "n-max" }, Category = "Optimization", Help = "The upper bound of N.")]
    /// public double NMax { get; set; } = 6.0;
    /// </code>
    ///
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public sealed class OptionAttribute : Attribute
    {
        /// <summary>
        /// Name of the option, used in the command line. It is case-insensitive.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Additional names for the option, used in the command line. They are case-insensitive.
        /// </summary>
        public string[] Aliases { get; set; } = [];

        /// <summary>
        /// Short description of the option. Can be used in the help message.
        /// </summary>
        public string Help { get; set; } = "";

        /// <summary>
        /// Category of the option, used in the help message. Options with the same category are grouped together. Default is "Other".
        /// </summary>
        public string Category { get; set; } = "Other";

        /// <summary>
        /// Type of the parser to use for this option.
        /// The parser must implement the IOptionParser interface.
        /// If not specified, the default parser for the property type will be used.
        /// </summary>
        public Type? Parser { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="OptionAttribute"/> class with the specified name.
        /// </summary>
        ///
        /// <param name="name">
        /// The primary name of the option, used to identify it in the command line.
        /// </param>
        public OptionAttribute(string name)
        {
            Name = name;
        }
    }
}
