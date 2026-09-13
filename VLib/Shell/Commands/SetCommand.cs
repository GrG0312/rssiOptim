using VLib.Args.Binding;

namespace VLib.Args.Shell.Commands
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
        public string Summary => "Egy paraméter értékének beállítása.";

        /// <inheritdoc />
        public string Usage => "set <paraméter> <érték>";

        /// <inheritdoc />
        public string Details =>
            """
            Érték nélkül kiírja a paraméter jelenlegi értékét és a lehetséges értékeket:
              set name

            A kapcsolók átbillenthetők úgy, hogy csak a paraméter nevét adod meg:
              set isfree

            A szóközt vagy speciális karaktert tartalmazó értékeket idézőjelbe kell tenni:
              set datapath "C:\adatok mappája\"
            """;

        /// <inheritdoc />
        public ShellResult Execute(ShellContext<TSettings> context, ArgumentList args)
        {
            if (args.IsEmpty)
            {
                throw new VLibArgException("Nem adtál meg paraméternevet.", Usage);
            }

            OptionDescriptor option = context.Model.Find(args[0]);

            // If only the parameter name is specified, display the current value and possible values.
            if (args.Count == 1 && !option.IsFlag)
            {
                context.Output.Line($"{option.Name} = {option.FormatCurrent(context.Target)}");
                context.Output.Muted($"  Típus:     {option.Parser.TypeName}");
                context.Output.Muted($"  Alapérték: {option.FormatDefault()}");
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
