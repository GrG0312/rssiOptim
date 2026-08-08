namespace VisiLib.Args.Shell
{
    /// <summary>
    /// Represents the result of executing a shell command, indicating whether to continue or exit the shell loop.
    /// </summary>
    public enum ShellResult
    {
        /// <summary>
        /// Waits for the next command to be entered and executed, continuing the shell loop.
        /// </summary>
        Continue,

        /// <summary>
        /// Exits the shell loop, terminating the interactive session.
        /// </summary>
        Exit
    }
}
