using System.Globalization;
using System.Text;
using RssiCalibration.Cli.Commands;
using RssiCalibration.Cli.Settings;
using VisiLib.Args.Parsing;
using VisiLib.Args.Shell;

namespace RssiCalibration.Cli
{
    public class Program
    {
        public static int Main(string[] args)
        {
            // A számítás és a CSV-k mindenhol invariáns kultúrával dolgoznak, a beolvasás pedig
            // a tizedesvesszőt is elfogadja - így a gép területi beállítása nem befolyásolja az eredményt.
            CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;
            TryUseUtf8Console();

            // A registry tudja, melyik típust melyik parser olvassa. A célfüggvény, az optimalizáló
            // és a stratégia saját parsert kap a CalibrationSettings-ben, a többi típus beépített.
            ParserRegistry registry = ParserRegistry.CreateDefault();

            return CommandShell<CalibrationSettings>
                .Create(registry, new CalibrationSettings())
                .WithPrompt("rssi")
                .WithBanner(PrintBanner)
                .Register(new RunCommand())
                .Run();
        }

        /// <summary>
        /// Megpróbálja UTF-8-ra állítani a konzol kódlapját, hogy a magyar ékezetek
        /// helyesen jelenjenek meg. Átirányított kimenetnél ez nem mindig lehetséges.
        /// </summary>
        private static void TryUseUtf8Console()
        {
            try
            {
                Console.OutputEncoding = Encoding.UTF8;
            }
            catch (IOException)
            {
                // Átirányított kimenetnél vagy korlátozott konzolon nem mindig állítható
            }
        }

        /// <summary>
        /// Kiírja az üdvözlő szöveget a shell indításakor.
        /// </summary>
        private static void PrintBanner(ShellOutput output)
        {
            output.Line("RSSI path-loss kalibráció");
            output.Muted("Az optimális n (környezeti csillapítás) keresése a log-distance modellhez.");
            output.Line();
            output.Line("Így használd:");
            output.Line("  show                    a jelenlegi beállítások megtekintése");
            output.Line("  set <paraméter> <érték> egy beállítás módosítása");
            output.Line("  run                     a kalibráció lefuttatása");
            output.Line("  help                    minden parancs és paraméter");
            output.Line("  exit                    kilépés");
            output.Line();
            output.Muted("Az alapbeállítások a data/ könyvtár mintaadataira mutatnak:");
            output.Muted("írd be, hogy 'run', és máris látod az eredményt.");
            output.Line();
        }
    }
}