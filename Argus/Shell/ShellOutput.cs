namespace VisiLib.Args.Shell
{
    /// <summary>
    /// Shell output. 
    /// Handles coloring in one place so that commands don't mess with the console state, 
    /// and so that redirected output (to a file, pipe) automatically omits coloring.
    /// </summary>
    public sealed class ShellOutput
    {
        /// <summary>
        /// True if coloring is enabled. False if coloring is disabled. 
        /// Null if coloring should be automatically determined based on whether the output is redirected.
        /// </summary>
        private readonly bool _colorEnabled;

        /// <summary>
        /// Creates a new instance of ShellOutput.
        /// </summary>
        /// 
        /// <param name="colorEnabled">
        /// True to enable coloring, false to disable coloring,
        /// null to automatically determine based on whether the output is redirected.
        /// </param>
        public ShellOutput(bool? colorEnabled = null)
        {
            _colorEnabled = colorEnabled ?? !Console.IsOutputRedirected;
        }

        /// <summary>
        /// Writes a line of text to the console. If no text is provided, writes an empty line.
        /// </summary>
        /// 
        /// <param name="text">
        /// The text to write to the console. If null or empty, an empty line is written.
        /// </param>
        public void Line(string text = "")
        {
            Console.WriteLine(text);
        }

        /// <summary>
        /// Writes a heading line of text to the console in cyan color.
        /// If no text is provided, writes an empty line.
        /// </summary>
        /// 
        /// <param name="text">
        /// The text to write to the console.
        /// If null or empty, an empty line is written.
        /// </param>
        public void Heading(string text)
        {
            Console.WriteLine();
            Colored(ConsoleColor.Cyan, text);
        }

        /// <summary>
        /// Writes a muted line of text to the console in dark gray color.
        /// </summary>
        /// 
        /// <param name="text">
        /// The text to write to the console. If null or empty, an empty line is written.
        /// </param>
        public void Muted(string text)
        {
            Colored(ConsoleColor.DarkGray, text);
        }

        /// <summary>
        /// Writes a success line of text to the console in green color.
        /// </summary>
        /// 
        /// <param name="text">
        /// The text to write to the console. If null or empty, an empty line is written.
        /// </param>
        public void Success(string text)
        {
            Colored(ConsoleColor.Green, text);
        }

        /// <summary>
        /// Writes an error line of text to the console in red color, and optionally writes a hint in muted color.
        /// </summary>
        /// 
        /// <param name="message">
        /// The error message to write to the console. If null or empty, an empty line is written.
        /// </param>
        /// 
        /// <param name="hint">
        /// An optional hint message to write to the console in muted color. If null, no hint is written.
        /// </param>
        public void Error(string message, string? hint = null)
        {
            if (_colorEnabled)  Console.ForegroundColor = ConsoleColor.Red;
            Console.Error.WriteLine(message);
            if (_colorEnabled) Console.ResetColor();

            if (hint is not null)
            {
                Muted("  " + hint);
            }
        }

        /// <summary>
        /// Writes a line of text to the console in the specified color, if coloring is enabled.
        /// </summary>
        /// 
        /// <param name="color">
        /// The color to use for the text. If coloring is disabled, this parameter is ignored.
        /// </param>
        /// 
        /// <param name="text">
        /// The text to write to the console. If null or empty, an empty line is written.
        /// </param>
        private void Colored(ConsoleColor color, string text)
        {
            if (_colorEnabled) Console.ForegroundColor = color;
            Console.WriteLine(text);
            if (_colorEnabled) Console.ResetColor();
        }
    }
}
