using VisiLib.Args.Binding;

namespace VisiLib.Args.Shell.Commands
{
    /// <summary>
    /// Models a command that sets the value of a parameter in the shell context.
    /// </summary>
    public sealed class SetCommand<TSettings> : IShellCommand<TSettings>
    {
        /// <inheritdoc />
        public string Name => "set";

        /// <inheritdoc />
        public IReadOnlyList<string> Aliases => ["s"];

        /// <inheritdoc />
        public string Summary => "Sets the value of a parameter.";

        /// <inheritdoc />
        public string Usage => "set <parameter> <value>";

        /// <inheritdoc />
        public string Details =>
            """
            Displays the current value and possible values when not used with a value parameter:
              set name

            Flags can be toggled by specifying only the parameter name:
              set isfree

            Values containing spaces or special characters must be quoted:
              set datapath "C:\folder of data\"
            """;

        /// <inheritdoc />
        public ShellResult Execute(ShellContext<TSettings> context, ArgumentList args)
        {
            if (args.IsEmpty)
            {
                throw new VisiArgException("There is no parameter name specified.", Usage);
            }

            OptionDescriptor option = context.Model.Find(args[0]);

            // If only the parameter name is specified, display the current value and possible values.
            if (args.Count == 1 && !option.IsFlag)
            {
                context.Output.Line($"{option.Name} = {option.FormatCurrent(context.Target)}");
                context.Output.Muted($"  Type: {option.Parser.TypeName}");
                context.Output.Muted($"  Default:  {option.FormatDefault()}");
                return ShellResult.Continue;
            }

            // If the parameter is a flag and no value is specified, toggle the value.
            string text = args.Count == 1 ? "true" : args.From(1);

            option.SetFromText(context.Target, text);
            context.Output.Success($"{option.Name} = {option.FormatCurrent(context.Target)}");

            return ShellResult.Continue;
        }
    }
}
