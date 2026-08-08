using System.Text;

namespace VisiLib.Args.Shell
{
    /// <summary>
    /// Splits a command line into words, respecting quoted strings.
    /// </summary>
    /// 
    /// <remarks>
    /// Inside quoted strings, every character is taken literally, including whitespace.
    /// To include a quote character inside a quoted string, use two consecutive quotes ("").
    /// </remarks>
    /// 
    // TODO: change from "" to \" (?)
    public static class CommandLexer
    {
        /// <summary>
        /// Splits a command line into words, respecting quoted strings.
        /// </summary>
        /// 
        /// <param name="line">
        /// The command line to split.
        /// </param>
        /// 
        /// <returns>
        /// A list of words extracted from the command line.
        /// </returns>
        /// 
        /// <exception cref="VisiArgException"></exception>
        public static IReadOnlyList<string> Split(string line)
        {
            List<string> tokens = new List<string>();
            StringBuilder current = new StringBuilder();
            bool inQuotes = false;
            bool started = false;

            for (int i = 0; i < line.Length; i++)
            {
                char c = line[i];

                if (c == '"')
                {
                    // If we are inside quotes and the next character is also a quote, treat it as an escaped quote.
                    if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
                    {
                        current.Append('"');
                        i++;
                        continue;
                    }

                    inQuotes = !inQuotes;
                    started = true;
                    continue;
                }

                if (!inQuotes && char.IsWhiteSpace(c))
                {
                    if (started)
                    {
                        tokens.Add(current.ToString());
                        current.Clear();
                        started = false;
                    }
                    continue;
                }

                current.Append(c);
                started = true;
            }

            if (inQuotes)
            {
                throw new VisiArgException("Unmatched quote in command line.");
            }

            if (started)
            {
                tokens.Add(current.ToString());
            }

            return tokens;
        }
    }
}
