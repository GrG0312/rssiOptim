using VLib.Args;
using VLib.Args.Binding;
using VLib.Args.Shell;

namespace VLib.Args.Shell.Commands
{
    /// <summary>
    /// Represents the "help" command, which provides information about commands and parameters in the shell.
    /// </summary>
    /// 
    /// <remarks>
    /// The same <c>help X</c> format works for both commands and parameters.
    /// If <c>X</c> is a command, it will display detailed information about that command.
    /// If <c>X</c> is a parameter, it will display detailed information about that parameter.
    /// First the command is searched, and if not found, the parameter is searched.
    /// </remarks>
    public sealed class HelpCommand<TSettings> : IShellCommand<TSettings>
    {
        /// <inheritdoc />
        public string Name => "help";

        /// <inheritdoc />
        public IReadOnlyList<string> Aliases => ["?", "h"];

        /// <inheritdoc />
        public string Summary => "Súgó a parancsokról és a paraméterekről.";

        /// <inheritdoc />
        public string Usage => "help <parancs vagy paraméter>";

        /// <inheritdoc />
        public string Details =>
            """
            help              - a parancsok listája
            help param        - minden paraméter a leírásával
            help <parancs>    - egy adott parancs részletes leírása
            help <paraméter>  - egy adott paraméter részletes leírása
            """;

        /// <inheritdoc />
        public ShellResult Execute(ShellContext<TSettings> context, ArgumentList args)
        {
            // If no arguments are provided, print the list of commands.
            if (args.IsEmpty)
            {
                PrintCommands(context);
                return ShellResult.Continue;
            }

            // If an argument is provided, treat it as a topic (command or parameter) to display help for.
            string topic = args[0];

            if (topic is "param" or "params" or "parameter")
            {
                PrintParameters(context);
                return ShellResult.Continue;
            }

            // First, try to find a command with the given topic.
            IShellCommand<TSettings>? command = context.FindCommand(topic);
            if (command is not null)
            {
                PrintCommand(context, command);
                return ShellResult.Continue;
            }

            // If no command is found, try to find a parameter with the given topic.
            if (context.Model.TryFind(topic, out var option))
            {
                context.Output.Heading(option.Name);
                if (option.Help.Length > 0) context.Output.Line(option.Help);
                context.Output.Muted($"Típus:     {option.Parser.TypeName}");
                context.Output.Muted($"Jelenleg:  {option.FormatCurrent(context.Target)}");
                context.Output.Muted($"Alapérték: {option.FormatDefault()}");
                return ShellResult.Continue;
            }

            throw new VLibArgException(
                $"Nincs '{topic}' nevű parancs vagy paraméter.",
                "Próbáld ezt: help vagy help param");
        }

        /// <summary>
        /// Prints the list of available commands in the shell <paramref name="context"/>.
        /// </summary>
        /// 
        /// <param name="context">
        /// The shell context containing the commands to be printed.
        /// </param>
        private static void PrintCommands(ShellContext<TSettings> context)
        {
            context.Output.Heading("PARANCSOK");

            int width = context.Commands.Max(c => c.Usage.Length);

            foreach (IShellCommand<TSettings> command in context.Commands)
            {
                context.Output.Line($"  {command.Usage.PadRight(width)}   {command.Summary}");
            }

            context.Output.Line();
            context.Output.Muted("A beállítható paraméterek listája:  help param");
            context.Output.Muted("Egy parancs részletei:              help <parancs>");
        }

        /// <summary>
        /// Prints detailed information about a specific command in the shell <paramref name="context"/>.
        /// </summary>
        /// 
        /// <param name="context">
        /// The shell context containing the command to be printed.
        /// </param>
        /// 
        /// <param name="command">
        /// The command for which detailed information will be printed.
        /// </param>
        private static void PrintCommand(ShellContext<TSettings> context, IShellCommand<TSettings> command)
        {
            context.Output.Heading(command.Usage);
            context.Output.Line(command.Summary);

            if (command.Aliases.Count > 0)
            {
                context.Output.Muted($"Rövidítések: {string.Join(", ", command.Aliases)}");
            }

            if (command.Details is not null)
            {
                context.Output.Line();
                context.Output.Line(command.Details);
            }
        }

        /// <summary>
        /// Prints detailed information about all parameters in the shell <paramref name="context"/>.
        /// </summary>
        /// 
        /// <param name="context">
        /// The shell context containing the parameters to be printed.
        /// </param>
        private static void PrintParameters(ShellContext<TSettings> context)
        {
            int width = context.Model.Options.Max(o => o.Name.Length);

            foreach (IGrouping<string, OptionDescriptor> category in context.Model.ByCategory)
            {
                context.Output.Heading(category.Key);

                foreach (OptionDescriptor option in category)
                {
                    context.Output.Line($"  {option.Name.PadRight(width)}   {option.Help}");
                    context.Output.Muted($"  {new string(' ', width)}   {option.Parser.TypeName}, " +
                                      $"alapérték: {option.FormatDefault()}");
                }
            }

            context.Output.Line();
            context.Output.Muted("Beállítás: set <paraméter> <érték>");
        }
    }
}
