using RssiCalibration.Cli.Settings;
using VisiLib.Args.Shell;

namespace RssiCalibration.Cli.Commands
{
    /// <summary>
    /// Lefuttatja a kalibrációt a pillanatnyi beállításokkal.
    /// </summary>
    /// <remarks>
    /// A parancs nem változtat a beállításokon, így egymás után többször is
    /// futtatható más-más célfüggvénnyel vagy stratégiával.
    /// </remarks>
    public sealed class RunCommand : IShellCommand<CalibrationSettings>
    {
        /// <inheritdoc />
        public string Name => "run";

        /// <inheritdoc />
        public IReadOnlyList<string> Aliases => ["futtat", "r"];

        /// <inheritdoc />
        public string Summary => "A kalibráció lefuttatása a mostani beállításokkal.";

        /// <inheritdoc />
        public string Usage => "run";

        /// <inheritdoc />
        public string Details =>
            """
            Beolvassa a megadott CSV fájlokat, megkeresi az optimális n értéket, kiírja az
            eredményt, és riportokat ír a kimeneti könyvtárba (summary.csv, residuals.csv,
            és ha a sweep be van kapcsolva, sweep.csv).

            A futtatás nem változtat a beállításokon, így egymás után többször is
            futtatható más-más célfüggvénnyel:
              set objective median
              run
              set objective composite
              run
            """;

        /// <inheritdoc />
        public ShellResult Execute(ShellContext<CalibrationSettings> context, ArgumentList args)
        {
            CalibrationSettings settings = context.Settings;

            // Az egymásnak ellentmondó vagy hiányzó bemenetet a futtatás előtt jelezzük,
            // hogy ne egy félbeszakadt számítás közepén derüljön ki.
            settings.Validate();

            CalibrationRunner.Run(settings);

            context.Output.Line();
            context.Output.Success($"Riportok kiírva ide: {Path.GetFullPath(settings.OutputDirectory)}");

            return ShellResult.Continue;
        }
    }
}
