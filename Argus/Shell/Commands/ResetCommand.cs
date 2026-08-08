using VisiLib.Args.Binding;

namespace VisiLib.Args.Shell.Commands
{
    /// <summary>
    /// Represents a command that resets the settings to their default values.
    /// </summary>
    public sealed class ResetCommand<TSettings> : IShellCommand<TSettings>
    {
        /// <inheritdoc />
        public string Name => "reset";

        /// <inheritdoc />
        public string Summary => "Reset to default values.";

        /// <inheritdoc />
        public string Usage => "reset [parameter]";

        /// <inheritdoc />
        public string Details => "Without parameters, resets all settings to their default values. If a parameter is specified, only that parameter is reset.";

        /// <inheritdoc />
        public ShellResult Execute(ShellContext<TSettings> context, ArgumentList args)
        {
            if (args.IsEmpty)
            {
                foreach (OptionDescriptor option in context.Model.Options)
                {
                    option.Reset(context.Target);
                }

                context.Output.Success("All parameters have been reset to their default values.");
                return ShellResult.Continue;
            }

            OptionDescriptor target = context.Model.Find(args[0]);
            target.Reset(context.Target);
            context.Output.Success($"{target.Name} = {target.FormatCurrent(context.Target)}");

            return ShellResult.Continue;
        }
    }
}
