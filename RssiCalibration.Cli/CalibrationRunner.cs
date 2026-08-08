using RssiCalibration.Cli.Settings;
using RssiCalibration.Core.Grouping;
using RssiCalibration.Core.Models;
using RssiCalibration.Core.Objectives;
using RssiCalibration.Core.Optimization;
using RssiCalibration.Core.PathLoss;
using RssiCalibration.Core.Services;
using RssiCalibration.Data;

namespace RssiCalibration.Cli
{
    /// <summary>
    /// Egy teljes kalibrációs futtatás: adatbetöltés, optimalizálás, jelentés, riportok.
    /// </summary>
    /// 
    /// <remarks>
    /// A futtatás minden alkalommal újraolvassa a CSV-ket. Ez szándékos: az interaktív
    /// felületen a felhasználó menet közben átírhatja a bemeneti fájlokat vagy az
    /// útvonalukat, és ilyenkor a friss adatot várja, nem egy korábbi állapot gyorsítótárát.
    /// </remarks>
    public static class CalibrationRunner
    {
        /// <summary>
        /// Lefuttatja a kalibrációt a megadott beállításokkal.
        /// </summary>
        /// 
        /// <exception cref="InvalidDataException">Ha a bemeneti CSV hibás.</exception>
        public static void Run(CalibrationSettings settings)
        {
            // A bemeneti adatok betöltése a CSV fájlokból
            CalibrationDataset dataset = new CsvDataSource(
                settings.AccessPointsPath,
                settings.MeasurementsPath,
                settings.Aggregation,
                settings.Separator).Load();

            // Az optimalizáláshoz szükséges komponensek összeállítása
            IErrorObjective objective = ObjectiveFactory.Create(settings.Objective);
            IOptimizer1D optimizer = OptimizerFactory.Create(settings.Optimizer);
            CalibrationEngine engine = new CalibrationEngine(new LogDistanceModel(), optimizer);

            CalibrationOptions calibrationOptions = new CalibrationOptions
            {
                NBounds = settings.NBounds,
                OptimizeRssi0Offset = settings.OptimizeRssi0
            };

            PrintDatasetSummary(dataset, objective, optimizer, settings);

            // Ha egy konkrét stratégia van megadva, csak azt futtatjuk; egyébként mindet
            IReadOnlyList<IGroupingStrategy> strategies;
            if (settings.Strategy is null)
            {
                strategies = GroupingStrategies.All;
            }
            else
            {
                strategies = new List<IGroupingStrategy> { GroupingStrategies.Create(settings.Strategy) };
            }

            List<(IGroupingStrategy Strategy, IReadOnlyList<CalibrationResult> Results)> all =
                new List<(IGroupingStrategy, IReadOnlyList<CalibrationResult>)>();

            foreach (IGroupingStrategy strategy in strategies)
            {
                IReadOnlyList<CalibrationResult> results = engine.Calibrate(dataset, strategy, objective, calibrationOptions);
                all.Add((strategy, results));
                ConsoleReporter.PrintStrategy(strategy, results, settings.OptimizeRssi0);
            }

            // Több stratégia esetén összehasonlító táblázatot is mutatunk
            if (all.Count > 1)
            {
                ConsoleReporter.PrintComparison(all);
            }

            // A részletes hibalistát a legfinomabb futtatott stratégiából mutatjuk
            ConsoleReporter.PrintWorstResiduals(all[^1].Results, settings.WorstCount);

            WriteReports(settings, dataset, engine, objective, all);
        }

        /// <summary>
        /// Kiírja az adathalmaz összefoglalóját a konzolra: AP-k, mérések, sávok, célfüggvény stb.
        /// </summary>
        private static void PrintDatasetSummary(
            CalibrationDataset dataset,
            IErrorObjective objective,
            IOptimizer1D optimizer,
            CalibrationSettings settings)
        {
            Console.WriteLine();
            Console.WriteLine("=== ADATHALMAZ ===");
            Console.WriteLine($"Access Point-ok : {dataset.AccessPoints.Count}");
            Console.WriteLine($"Mérésipontok    : {dataset.Measurements.Select(m => m.PointId).Distinct().Count()}");
            Console.WriteLine($"Minták          : {dataset.Measurements.Count}");
            Console.WriteLine($"Gyártók         : {string.Join(", ", dataset.AccessPoints.Select(a => a.Vendor).Distinct())}");
            Console.WriteLine($"Sávok           : {string.Join(", ", dataset.AccessPoints.Select(a => a.Band).Distinct())}");
            Console.WriteLine($"Célfüggvény     : {objective.Name}");
            Console.WriteLine($"Optimalizáló    : {optimizer.Name}");
            Console.WriteLine($"n tartomány     : [{settings.NBounds.Min:0.##}, {settings.NBounds.Max:0.##}]");

            // Referenciaértékként kiírjuk a zárt képlettel becsült n-t is
            double lsN = LeastSquaresInitializer.EstimateN(dataset, dataset.Measurements);
            Console.WriteLine($"n zárt képlettel: {lsN:0.000}  (log-térbeli legkisebb négyzetek, referenciaérték)");
        }

        /// <summary>
        /// A kimeneti könyvtárba írja a CSV riportokat: összefoglaló, reziduálisok és opcionálisan az n-sweep görbe.
        /// </summary>
        private static void WriteReports(
            CalibrationSettings settings,
            CalibrationDataset dataset,
            CalibrationEngine engine,
            IErrorObjective objective,
            List<(IGroupingStrategy Strategy, IReadOnlyList<CalibrationResult> Results)> all)
        {
            Directory.CreateDirectory(settings.OutputDirectory);

            List<CalibrationResult> flat = all.SelectMany(x => x.Results).ToList();
            CsvReportWriter.WriteSummary(Path.Combine(settings.OutputDirectory, "summary.csv"), flat);
            CsvReportWriter.WriteResiduals(Path.Combine(settings.OutputDirectory, "residuals.csv"), flat);

            if (!settings.ExportSweep) return;

            // Az n -> hiba görbe a legrészletesebb stratégia csoportjaira
            (IGroupingStrategy Strategy, IReadOnlyList<CalibrationResult> Results) finest = all[^1];
            Dictionary<string, IReadOnlyList<(double N, double Value)>> curves =
                new Dictionary<string, IReadOnlyList<(double N, double Value)>>();

            foreach (CalibrationResult result in finest.Results)
            {
                IEnumerable<Measurement> samples = dataset.Measurements
                    .Where(m => finest.Strategy.GetKey(dataset.ApOf(m)) == result.Group);

                curves[result.Group.Value] = engine.Sweep(dataset, samples, objective, settings.NBounds, 501);
            }

            CsvReportWriter.WriteSweep(Path.Combine(settings.OutputDirectory, "sweep.csv"), curves);
        }
    }
}
