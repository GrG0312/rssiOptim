namespace VLib.Args.Shell.Commands
{
    /// <summary>
    /// Represents a command that exits the program.
    /// </summary>
    public sealed class ExitCommand<TSettings> : IShellCommand<TSettings>
    {
        /// <inheritdoc />
        public string Name => "exit";

        /// <inheritdoc />
        public IReadOnlyList<string> Aliases => ["quit", "q"];

        /// <inheritdoc />
        public string Summary => "Kilépés a programból.";

        /// <inheritdoc />
        public string Usage => "exit";

        /// <inheritdoc />
        public ShellResult Execute(ShellContext<TSettings> context, ArgumentList args) => ShellResult.Exit;
    }
}
