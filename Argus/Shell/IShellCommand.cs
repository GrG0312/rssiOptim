namespace VisiLib.Args.Shell
{
    /// <summary>
    /// Defines a command that can be executed in the shell.
    /// Each command has a name, optional aliases, a summary, usage information, and an execution method.
    /// </summary>
    /// 
    /// <typeparam name="TSettings">
    /// The type of the settings object that will be modified by the command.
    /// </typeparam>
    public interface IShellCommand<TSettings>
    {
        /// <summary>
        /// Original name of the command, used to invoke it in the shell.
        /// This is the primary identifier for the command.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Optional alternative names for the command.
        /// These can be used to invoke the command in addition to the primary name.
        /// </summary>
        public IReadOnlyList<string> Aliases => [];

        /// <summary>
        /// A brief description of what the command does, used in the <c>help</c> output.
        /// </summary>
        public string Summary { get; }

        /// <summary>
        /// A short usage string that shows how to invoke the command, used in the <c>help</c> output.
        /// </summary>
        public string Usage { get; }

        /// <summary>
        /// Optional detailed information about the command, used in the <c>help</c> output.
        /// </summary>
        public string? Details => null;

        /// <summary>
        /// Executes the command with the given shell context and arguments.
        /// </summary>
        /// 
        /// <param name="context">
        /// The shell context containing the settings object, output handler, and available commands.
        /// </param>
        /// 
        /// <param name="args">
        /// The list of arguments provided to the command, excluding the command name itself.
        /// </param>
        /// 
        /// <returns>
        /// A <see cref="ShellResult"/> indicating the outcome of the command execution, which can be used to control the shell's flow (e.g., continue, exit, etc.).
        /// </returns>
        public ShellResult Execute(ShellContext<TSettings> context, ArgumentList args);
    }
}
