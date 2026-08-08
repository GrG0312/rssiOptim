using RssiCalibration.Core.Optimization;
using RssiCalibration.Data;
using VisiLib.Args;

namespace RssiCalibration.Cli.Settings
{
    /// <summary>
    /// A kalibráció összes állítható paramétere.
    /// </summary>
    /// <remarks>
    /// Ez az osztály az egyetlen hely, ahol a paraméterek léteznek: a név, a súgószöveg,
    /// az alapérték és az értelmezés módja itt van egymás mellett. Az interaktív felület
    /// ebből az osztályból derít ki mindent - új paraméter felvételéhez elég ide egy
    /// property, sem a shellhez, sem a súgóhoz nem kell hozzányúlni.
    /// </remarks>
    public sealed class CalibrationSettings
    {
        private const string DataCategory = "ADATFORRÁS";
        private const string ModelCategory = "MODELL ÉS CÉLFÜGGVÉNY";
        private const string SearchCategory = "KERESÉS";
        private const string OutputCategory = "KIMENET";

        [Option("aps",
            Aliases = new[] { "a" },
            Category = DataCategory,
            Parser = typeof(PathParser),
            Help = "Az access point-ok CSV fájlja (ApId, Vendor, FrequencyMHz, Rssi0).")]
        public string AccessPointsPath { get; set; } = Path.Combine("data", "access-points.csv");

        [Option("measurements",
            Aliases = new[] { "m", "mer" },
            Category = DataCategory,
            Parser = typeof(PathParser),
            Help = "A mérések CSV fájlja (ApId, PointId, Rssi, TrueDistance).")]
        public string MeasurementsPath { get; set; } = Path.Combine("data", "measurements.csv");

        [Option("separator",
            Aliases = new[] { "sep" },
            Category = DataCategory,
            Help = "A CSV oszlopelválasztó karaktere. Írható 'tab' vagy 'comma' is.")]
        // A data/ mintafájlok - és a magyar Excel alapértelmezése - pontosvesszőt használnak.
        public char Separator { get; set; } = ';';

        [Option("aggregate",
            Aliases = new[] { "agg" },
            Category = DataCategory,
            Help = "Több RSSI minta összevonása (AP, pont) páronként.")]
        public SampleAggregation Aggregation { get; set; } = SampleAggregation.None;

        [Option("objective",
            Aliases = new[] { "obj" },
            Category = ModelCategory,
            Parser = typeof(ObjectiveParser),
            Help = "A minimalizálandó hibametrika.")]
        public string Objective { get; set; } = "median";

        [Option("free-rssi0",
            Aliases = new[] { "rssi0" },
            Category = ModelCategory,
            Help = "Csoportonként az RSSI0 referenciaszintet is hangolja, ne csak az n-t.")]
        public bool OptimizeRssi0 { get; set; }

        [Option("strategy",
            Aliases = new[] { "s" },
            Category = ModelCategory,
            Parser = typeof(StrategyParser),
            Help = "Melyik AP-k osztozzanak egy n értéken. 'mind' esetén mind lefut és összevetjük.")]
        public string? Strategy { get; set; }

        [Option("optimizer",
            Aliases = new[] { "opt" },
            Category = SearchCategory,
            Parser = typeof(OptimizerParser),
            Help = "A keresési eljárás az optimális n megtalálásához.")]
        public string Optimizer { get; set; } = "hybrid";

        [Option("nmin",
            Category = SearchCategory,
            Help = "Az n keresési tartományának alsó határa.")]
        public double NMin { get; set; } = 1.0;

        [Option("nmax",
            Category = SearchCategory,
            Help = "Az n keresési tartományának felső határa.")]
        public double NMax { get; set; } = 6.0;

        [Option("out",
            Aliases = new[] { "o" },
            Category = OutputCategory,
            Parser = typeof(PathParser),
            Help = "A riportok könyvtára. Ha nincs meg, a futtatás létrehozza.")]
        public string OutputDirectory { get; set; } = "output";

        [Option("worst",
            Category = OutputCategory,
            Help = "Hány legnagyobb hibájú mérést listázzon a futtatás végén.")]
        public int WorstCount { get; set; } = 10;

        [Option("sweep",
            Category = OutputCategory,
            Help = "Exportálja-e az n -> hiba görbét a sweep.csv fájlba.")]
        public bool ExportSweep { get; set; } = true;

        /// <summary>
        /// Az <see cref="NMin"/> és <see cref="NMax"/> párosból képzett keresési tartomány.
        /// </summary>
        public Interval NBounds => Interval.Of(NMin, NMax);

        /// <summary>
        /// Azokat a feltételeket ellenőrzi, amiket egyetlen paraméter önmagában nem tud:
        /// két érték egymáshoz való viszonyát, illetve a bemeneti fájlok meglétét.
        /// </summary>
        /// <exception cref="VisiArgException">Ha a beállítások így nem futtathatók.</exception>
        public void Validate()
        {
            if (NMax <= NMin)
            {
                throw new VisiArgException(
                    $"Az nmax ({NMax}) nem lehet kisebb vagy egyenlő az nmin-nél ({NMin}).",
                    "Például: set nmin 1.5   majd   set nmax 5");
            }

            if (WorstCount < 0)
            {
                throw new VisiArgException("A worst értéke nem lehet negatív.");
            }

            RequireFile(AccessPointsPath, "aps");
            RequireFile(MeasurementsPath, "measurements");
        }

        /// <summary>
        /// Ellenőrzi, hogy a megadott fájl létezik-e. Ha nem, kivételt dob a beállítás nevével.
        /// </summary>
        ///
        /// <param name="path">Az ellenőrizendő fájl elérési útja.</param>
        /// <param name="optionName">A beállítás neve, amely a hibaüzenetben megjelenik.</param>
        private static void RequireFile(string path, string optionName)
        {
            if (!File.Exists(path))
            {
                throw new VisiArgException(
                    $"Nincs meg a fájl: {Path.GetFullPath(path)}",
                    $"Állítsd át így: set {optionName} <útvonal>");
            }
        }
    }
}
