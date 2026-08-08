using VisiLib.Args.Binding;

namespace VisiLib.Args.Shell
{
    /// <summary>
    /// Shell context containing every necessary information for executing commands.
    /// </summary>
    /// 
    /// <typeparam name="TSettings">
    /// The type of the settings object that will be modified by the commands.
    /// </typeparam>
    public sealed class ShellContext<TSettings>
    {
        /// <summary>
        /// Describes the command-line options and their mapping to the settings object.
        /// </summary>
        public OptionModel Model { get; }

        /// <summary>
        /// The settings object that will be modified by the commands.
        /// </summary>
        public TSettings Settings { get; }

        /// <summary>
        /// The output handler that manages console output, including coloring and formatting.
        /// </summary>
        public ShellOutput Output { get; }

        /// <summary>
        /// The list of available commands that can be executed in this shell context.
        /// </summary>
        public IReadOnlyList<IShellCommand<TSettings>> Commands { get; }

        /// <summary>
        /// Gets the target <see cref="object"/> that will be modified by the commands. This is the same as the Settings property.
        /// </summary>
        public object Target => Settings!;

        /// <summary>
        /// Initializes a new instance of the <see cref="ShellContext{TSettings}"/> class.
        /// </summary>
        /// 
        /// <param name="model">
        /// The <see cref="OptionModel"/> that describes the command-line options and their mapping to the settings object.
        /// </param>
        /// 
        /// <param name="settings">
        /// The settings object that will be modified by the commands.
        /// </param>
        /// 
        /// <param name="output">
        /// The <see cref="ShellOutput"/> handler that manages console output, including coloring and formatting.
        /// </param>
        /// 
        /// <param name="commands">
        /// The list of available commands that can be executed in this shell context.
        /// </param>
        public ShellContext(OptionModel model, TSettings settings, ShellOutput output, params IShellCommand<TSettings>[] commands)
        {
            Model = model;
            Settings = settings;
            Output = output;
            Commands = commands;
        }

        /// <summary>
        /// Finds a command by its name or alias, ignoring case. Returns null if no matching command is found.
        /// </summary>
        /// 
        /// <param name="name">
        /// The name or alias of the command to find.
        /// </param>
        /// 
        /// <returns>
        /// The matching <see cref="IShellCommand{TSettings}"/> if found; otherwise, null.
        /// </returns>
        public IShellCommand<TSettings>? FindCommand(string name)
        {
            return Commands.FirstOrDefault(c =>
                // Check if the command's name matches the provided name, ignoring case
                string.Equals(c.Name, name, StringComparison.OrdinalIgnoreCase) ||
                // Check if any of the command's aliases match the provided name, ignoring case
                c.Aliases.Any(a => string.Equals(a, name, StringComparison.OrdinalIgnoreCase)));
        }
    }
}
