using VLib.Args.Binding;

namespace VLib.Args.Shell.Commands
{
    /// <summary>
    /// Represents a command that resets the settings to their default values.
    /// </summary>
    public sealed class ResetCommand<TSettings> : IShellCommand<TSettings>
    {
        /// <inheritdoc />
        public string Name => "reset";

        /// <inheritdoc />
        public string Summary => "Visszaállítás az alapértékekre.";

        /// <inheritdoc />
        public string Usage => "reset [paraméter]";

        /// <inheritdoc />
        public string Details => "Paraméter nélkül minden beállítást visszaállít az alapértékére. Ha megadsz egy paramétert, csak az áll vissza.";

        /// <inheritdoc />
        public ShellResult Execute(ShellContext<TSettings> context, ArgumentList args)
        {
            if (args.IsEmpty)
            {
                foreach (OptionDescriptor option in context.Model.Options)
                {
                    option.Reset(context.Target);
                }

                context.Output.Success("Minden paraméter visszaállt az alapértékére.");
                return ShellResult.Continue;
            }

            OptionDescriptor target = context.Model.Find(args[0]);
            target.Reset(context.Target);
            context.Output.Success($"{target.Name} = {target.FormatCurrent(context.Target)}");

            return ShellResult.Continue;
        }
    }
}
