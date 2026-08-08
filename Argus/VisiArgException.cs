namespace VisiLib.Args
{
    /// <summary>
    /// User-facing exception type for VisiLib.Args.
    /// This is thrown when the user provides invalid input, and it is caught by the shell to display a friendly error message.
    /// </summary>
    public sealed class VisiArgException : Exception
    {
        /// <summary>
        /// Optional hint for the user, typically showing the correct usage.
        /// </summary>
        public string? Hint { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="VisiArgException"/> class with a specified error message and an optional hint.
        /// </summary>
        /// 
        /// <param name="message">
        /// The error message that explains the reason for the exception.
        /// </param>
        /// 
        /// <param name="hint">
        /// An optional hint for the user, typically showing the correct usage.
        /// </param>
        public VisiArgException(string message, string? hint = null) : base(message)
        {
            Hint = hint;
        }
    }
}
