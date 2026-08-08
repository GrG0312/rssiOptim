using VisiLib.Args.Binding;
using VisiLib.Args.Parsing;
using VisiLib.Args.Shell.Commands;

namespace VisiLib.Args.Shell
{
    /// <summary>
    /// Az interaktív parancsértelmező: beolvassa a sorokat, megkeresi a hozzájuk
    /// tartozó parancsot, és lefuttatja.
    /// </summary>
    /// 
    /// <remarks>
    /// A shell alapból ismeri a beállításokat kezelő parancsokat (<c>help</c>,
    /// <c>show</c>, <c>set</c>, <c>reset</c>, <c>exit</c>). Az alkalmazás ezekhez
    /// adja hozzá a saját műveleteit - jellemzően egy "futtasd le" parancsot.
    /// <code>
    /// var registry = ParserRegistry.CreateDefault();
    ///
    /// CommandShell&lt;MySettings&gt;
    ///     .Create(registry, new MySettings())
    ///     .WithPrompt("app")
    ///     .Register(new RunCommand())
    ///     .Run();
    /// </code>
    /// </remarks>
    /// 
    /// <typeparam name="TSettings">
    /// The type of the settings class that the shell will manage. It must have a parameterless constructor.
    /// </typeparam>
    public sealed class CommandShell<TSettings> where TSettings : new()
    {
        /// <summary>
        /// Commands known to the shell. The built-in commands are always present, and additional commands can be registered by the application.
        /// </summary>
        private readonly List<IShellCommand<TSettings>> _commands;

        /// <summary>
        /// The model of the settings class, which contains the discovered parameters and their metadata.
        /// </summary>
        private readonly OptionModel _model;

        /// <summary>
        /// The instance of the settings class that the shell will modify and manage.
        /// </summary>
        private readonly TSettings _settings;

        /// <summary>
        /// The output interface used by the shell to display messages, errors, and other information. 
        /// By default, it writes to the console, but it can be replaced with a custom implementation.
        /// </summary>
        private readonly ShellOutput _output;

        /// <summary>
        /// The prompt string displayed at the beginning of each input line. 
        /// The default value is ">", but it can be customized using the WithPrompt method.
        /// </summary>
        private string _prompt = ">";

        /// <summary>
        /// The banner action that is executed once when the shell starts.
        /// It can be used to display a welcome message or other information.
        /// </summary>
        private Action<ShellOutput>? _banner;

        /// <summary>
        /// Initializes a new instance of the class with the specified settings model, settings instance, and optional output interface.
        /// </summary>
        /// 
        /// <param name="model">
        /// The <see cref="OptionModel"/> that describes the parameters of the settings class.
        /// It is used to discover and manage the settings.
        /// </param>
        /// 
        /// <param name="settings">
        /// The instance of the settings class that the shell will manage.
        /// It must be of type <typeparamref name="TSettings"/> and have a parameterless constructor.
        /// </param>
        /// 
        /// <param name="output">
        /// An optional <see cref="ShellOutput"/> instance that defines how the shell will display messages and errors.
        /// </param>
        public CommandShell(OptionModel model, TSettings settings, ShellOutput? output = null)
        {
            _model = model;
            _settings = settings;
            _output = output ?? new ShellOutput();

            _commands =
            [
                new HelpCommand<TSettings>(),
                new ShowCommand<TSettings>(),
                new SetCommand<TSettings>(),
                new ResetCommand<TSettings>()
            ];
        }

        /// <summary>
        /// Creates a new instance of the <see cref="CommandShell{TSettings}"/> class with the specified parser registry and settings instance.
        /// </summary>
        public static CommandShell<TSettings> Create(ParserRegistry registry, TSettings settings)
        {
            return new CommandShell<TSettings>(OptionModel.For<TSettings>(registry), settings);
        }

        /// <summary>
        /// Sets the prompt string that will be displayed at the beginning of each input line.
        /// The default value is ">".
        /// </summary>
        public CommandShell<TSettings> WithPrompt(string prompt)
        {
            _prompt = prompt;
            return this;
        }

        /// <summary>
        /// Sets the banner action that will be executed once when the shell starts.
        /// </summary>
        /// 
        /// <param name="banner">
        /// An action that takes a <see cref="ShellOutput"/> parameter and is executed to display a banner or welcome message when the shell starts.
        /// </param>
        /// 
        /// <returns>
        /// The current instance of the <see cref="CommandShell{TSettings}"/> class, allowing for method chaining.
        /// </returns>
        public CommandShell<TSettings> WithBanner(Action<ShellOutput> banner)
        {
            _banner = banner;
            return this;
        }

        /// <summary>
        /// Registers a new command with the shell.
        /// The command must implement the <see cref="IShellCommand{TSettings}"/> interface.
        /// </summary>
        /// 
        /// <param name="command">
        /// The command to be registered with the shell.
        /// It must implement the <see cref="IShellCommand{TSettings}"/> interface.
        /// </param>
        /// 
        /// <returns>
        /// The current instance of the <see cref="CommandShell{TSettings}"/> class, allowing for method chaining.
        /// </returns>
        public CommandShell<TSettings> Register(IShellCommand<TSettings> command)
        {
            _commands.Add(command);
            return this;
        }

        /// <summary>
        /// Starts the command shell, displaying the prompt and waiting for user input.
        /// Ends when the user enters the "exit" command or presses Ctrl+Z (EOF).
        /// </summary>
        /// 
        /// <returns>
        /// An integer exit code, where 0 indicates successful termination of the shell.
        /// </returns>
        public int Run()
        {
            // Combine the built-in commands with the user-registered commands, and add the exit command at the end.
            List<IShellCommand<TSettings>> commands = new List<IShellCommand<TSettings>>(_commands)
            {
                new ExitCommand<TSettings>()
            };

            ShellContext<TSettings> context = new ShellContext<TSettings>(_model, _settings, _output, commands.ToArray());

            _banner?.Invoke(_output);

            while (true)
            {
                Console.Write($"{_prompt}> ");

                string? line = Console.ReadLine();

                // If the user presses Ctrl+Z (EOF), Console.ReadLine() returns null, and we exit the shell.
                if (line is null)
                {
                    _output.Line();
                    return 0;
                }

                if (Dispatch(context, line) == ShellResult.Exit)
                {
                    return 0;
                }
            }
        }

        /// <summary>
        /// Dispatches the input line to the appropriate command based on the first token.
        /// </summary>
        /// 
        /// <param name="context">
        /// The <see cref="ShellContext{TSettings}"/> that contains the settings, output, and registered commands.
        /// </param>
        /// 
        /// <param name="line">
        /// The input line entered by the user, which will be tokenized and matched against registered commands.
        /// </param>
        /// 
        /// <returns>
        /// A <see cref="ShellResult"/> indicating whether to continue the shell loop or exit.
        /// </returns>
        private ShellResult Dispatch(ShellContext<TSettings> context, string line)
        {
            try
            {
                // Ignore empty lines and comments (lines starting with '#').
                // TODO: add support for multi-line commands and other command markers.
                if (string.IsNullOrWhiteSpace(line) || line.TrimStart().StartsWith('#'))
                {
                    return ShellResult.Continue;
                }

                IReadOnlyList<string> tokens = CommandLexer.Split(line);
                if (tokens.Count == 0)
                {
                    return ShellResult.Continue;
                }

                IShellCommand<TSettings>? command = context.FindCommand(tokens[0]);
                if (command is null)
                {
                    throw new VisiArgException($"Unknown command: {tokens[0]}. Type 'help' to see the list of available commands.");
                }

                return command.Execute(context, new ArgumentList(tokens.Skip(1).ToList()));
            }
            catch (VisiArgException ex)
            {
                _output.Error(ex.Message, ex.Hint);
                return ShellResult.Continue;
            }
            catch (Exception ex) when (ex is ArgumentException or InvalidDataException or IOException or FormatException)
            {
                // Handle common exceptions that may occur during command execution and provide user-friendly error messages.
                _output.Error(ex.Message);
                return ShellResult.Continue;
            }
        }
    }
}
