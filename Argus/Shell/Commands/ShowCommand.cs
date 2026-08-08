using VisiLib.Args.Binding;

namespace VisiLib.Args.Shell.Commands
{
    /// <summary>
    /// Outputs the current settings to the console.
    /// </summary>
    /// 
    /// <typeparam name="TSettings">
    /// The type of the settings object that the command operates on.
    /// </typeparam>
    public sealed class ShowCommand<TSettings> : IShellCommand<TSettings>
    {
        /// <inheritdoc />
        public string Name => "show";

        /// <inheritdoc />
        public IReadOnlyList<string> Aliases => ["ls", "list"];

        /// <inheritdoc />
        public string Summary => "Writes the current settings to the console, with an asterisk (*) marking those that differ from their default values.";

        /// <inheritdoc />
        public string Usage => "show <parameter>";

        /// <inheritdoc />
        public string Details =>
            """
            Displays the current settings of the application.
            If a specific parameter name is provided, it will show the value and details of that parameter.
            If no parameter is specified, it will list all parameters along with their current values,
            marking those that differ from their default values with an asterisk (*).
            """;

        /// <inheritdoc />
        public ShellResult Execute(ShellContext<TSettings> context, ArgumentList args)
        {
            // If a specific parameter name is provided, show its details.
            if (!args.IsEmpty)
            {
                ShowOne(context, args[0]);
                return ShellResult.Continue;
            }

            // Otherwise, show all parameters and their current values.
            int width = context.Model.Options.Max(o => o.Name.Length);

            foreach (IGrouping<string, OptionDescriptor> category in context.Model.ByCategory)
            {
                context.Output.Heading(category.Key);

                foreach (OptionDescriptor option in category)
                {
                    string changed = option.IsAtDefault(context.Target) ? " " : "*";
                    string value = option.FormatCurrent(context.Target);

                    context.Output.Line($" {changed} {option.Name.PadRight(width)}  = {value}");
                }
            }

            context.Output.Line();
            context.Output.Muted("* = differs from its default value.  For details: help <parameter>");

            return ShellResult.Continue;
        }

        /// <summary>
        /// Shows the details of a single parameter with the name <paramref name="name"/>.
        /// </summary>
        /// 
        /// <param name="context">
        /// The shell context containing the settings and output.
        /// </param>
        /// 
        /// <param name="name">
        /// The name of the parameter to show.
        /// </param>
        private static void ShowOne(ShellContext<TSettings> context, string name)
        {
            OptionDescriptor option = context.Model.Find(name);

            context.Output.Line($"{option.Name} = {option.FormatCurrent(context.Target)}");

            if (option.Help.Length > 0)
            {
                context.Output.Line($"  {option.Help}");
            }

            context.Output.Muted($"  Type: {option.Parser.TypeName}");
            context.Output.Muted($"  Default:  {option.FormatDefault()}");

            if (option.Aliases.Count > 0)
            {
                context.Output.Muted($"  Aliases: {string.Join(", ", option.Aliases)}");
            }
        }
    }
}
