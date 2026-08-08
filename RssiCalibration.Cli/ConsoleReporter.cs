using RssiCalibration.Core.Grouping;
using RssiCalibration.Core.Models;

namespace RssiCalibration.Cli;

/// <summary>
/// Konzolra írja a kalibrációs eredményeket: stratégiánkénti táblázat, összehasonlítás
/// és a legnagyobb hibákat tartalmazó lista.
/// </summary>
internal static class ConsoleReporter
{
    /// <summary>
    /// Kiírja egy stratégia kalibrációs eredményeit táblázatos formában.
    /// </summary>
    ///
    /// <param name="strategy">A futtatott csoportosítási stratégia.</param>
    /// <param name="results">A stratégia által kapott kalibrációs eredmények.</param>
    /// <param name="showRssi0Offset">Ha igaz, az RSSI0 eltolás oszlop is megjelenik.</param>
    public static void PrintStrategy(
        IGroupingStrategy strategy,
        IReadOnlyList<CalibrationResult> results,
        bool showRssi0Offset)
    {
        Console.WriteLine();
        Console.WriteLine($"=== {strategy.Name.ToUpperInvariant()} - {strategy.Description} ===");

        // A fejléc tartalmazza az RSSI0 eltolás oszlopot, ha a felhasználó kérte
        string header = showRssi0Offset
            ? $"{"Csoport",-24} {"n",7} {"dRSSI0",7} {"db",4} {"MAE",8} {"medián",8} {"RMSE",8} {"P90",8} {"max",9}"
            : $"{"Csoport",-24} {"n",7} {"db",4} {"MAE",8} {"medián",8} {"RMSE",8} {"P90",8} {"max",9}";

        Console.WriteLine(header);
        Console.WriteLine(new string('-', header.Length));

        // Minden kalibrációs eredményt egy sorban jelenítünk meg
        foreach (CalibrationResult r in results)
        {
            ErrorStatistics s = r.Statistics;
            string line = showRssi0Offset
                ? $"{Truncate(r.Group.Value, 24),-24} {r.OptimalN,7:0.000} {r.Rssi0Offset,7:+0.0;-0.0;0.0} " +
                  $"{s.Count,4} {s.MeanAbsError,8:0.00} {s.MedianAbsError,8:0.00} " +
                  $"{s.Rmse,8:0.00} {s.P90AbsError,8:0.00} {s.MaxAbsError,9:0.00}"
                : $"{Truncate(r.Group.Value, 24),-24} {r.OptimalN,7:0.000} " +
                  $"{s.Count,4} {s.MeanAbsError,8:0.00} {s.MedianAbsError,8:0.00} " +
                  $"{s.Rmse,8:0.00} {s.P90AbsError,8:0.00} {s.MaxAbsError,9:0.00}";

            Console.WriteLine(line);
        }

        // Összesített sor: az összes csoport hibáit egyesítve
        ErrorStatistics overall = ErrorStatistics.From(
            results.SelectMany(r => r.Residuals).Select(r => r.Error).ToList());

        Console.WriteLine(new string('-', header.Length));
        Console.WriteLine(
            $"{"ÖSSZESÍTETT",-24} {"",7} {(showRssi0Offset ? new string(' ', 8) : "")}" +
            $"{overall.Count,4} {overall.MeanAbsError,8:0.00} {overall.MedianAbsError,8:0.00} " +
            $"{overall.Rmse,8:0.00} {overall.P90AbsError,8:0.00} {overall.MaxAbsError,9:0.00}");
    }

    /// <summary>
    /// Kiírja a stratégiák összehasonlítását: stratégiánként az összesített hibastatisztikákat.
    /// </summary>
    ///
    /// <param name="all">Az összes futtatott stratégia és az eredményeik.</param>
    public static void PrintComparison(
        IReadOnlyList<(IGroupingStrategy Strategy, IReadOnlyList<CalibrationResult> Results)> all)
    {
        Console.WriteLine();
        Console.WriteLine("=== STRATÉGIÁK ÖSSZEHASONLÍTÁSA (összesített hibák) ===");
        string header = $"{"Stratégia",-14} {"csoport",8} {"MAE",8} {"medián",8} {"RMSE",8} {"P90",8} {"max",9}";
        Console.WriteLine(header);
        Console.WriteLine(new string('-', header.Length));

        foreach ((IGroupingStrategy strategy, IReadOnlyList<CalibrationResult> results) in all)
        {
            ErrorStatistics stats = ErrorStatistics.From(
                results.SelectMany(r => r.Residuals).Select(r => r.Error).ToList());

            Console.WriteLine(
                $"{strategy.Name,-14} {results.Count,8} {stats.MeanAbsError,8:0.00} " +
                $"{stats.MedianAbsError,8:0.00} {stats.Rmse,8:0.00} " +
                $"{stats.P90AbsError,8:0.00} {stats.MaxAbsError,9:0.00}");
        }

        Console.WriteLine();
        Console.WriteLine("Megjegyzés: a több csoportra bontás mindig jobb illeszkedést ad, de");
        Console.WriteLine("kevesebb mintára támaszkodik. A 'per-ap' látszólagos fölénye lehet túlillesztés.");
    }

    /// <summary>
    /// Kiírja a legnagyobb hibájú méréseket részletesen (csoport, AP, pont, RSSI, távolságok, hiba).
    /// </summary>
    ///
    /// <param name="results">A kalibrációs eredmények, amelyekből a hibákat kigyűjtjük.</param>
    /// <param name="count">A megjelenítendő legnagyobb hibák száma.</param>
    public static void PrintWorstResiduals(IReadOnlyList<CalibrationResult> results, int count)
    {
        // Az összes reziduálist összegyűjtjük, abszolút hiba szerint csökkenő sorrendbe rendezzük
        List<(GroupKey Group, ResidualRow Row)> worst = results
            .SelectMany(r => r.Residuals.Select(x => (r.Group, Row: x)))
            .OrderByDescending(x => Math.Abs(x.Row.Error))
            .Take(count)
            .ToList();

        if (worst.Count == 0) return;

        Console.WriteLine();
        Console.WriteLine($"=== {count} LEGNAGYOBB HIBA ===");
        string header = $"{"Csoport",-20} {"AP",-8} {"Pont",-8} {"RSSI",7} {"valós",8} {"becsült",9} {"hiba",9}";
        Console.WriteLine(header);
        Console.WriteLine(new string('-', header.Length));

        foreach ((GroupKey group, ResidualRow row) in worst)
            Console.WriteLine(
                $"{Truncate(group.Value, 20),-20} {Truncate(row.ApId, 8),-8} {Truncate(row.PointId, 8),-8} " +
                $"{row.Rssi,7:0.0} {row.TrueDistance,8:0.00} {row.EstimatedDistance,9:0.00} " +
                $"{row.Error,9:+0.00;-0.00}");
    }

    /// <summary>
    /// Levágja a szöveget a megadott hosszra, és ha túl hosszú, a végére egy „…" karaktert tesz.
    /// </summary>
    ///
    /// <param name="text">A levágandó szöveg.</param>
    /// <param name="max">A maximális megengedett hossz.</param>
    private static string Truncate(string text, int max) =>
        text.Length <= max ? text : text[..(max - 1)] + "…";
}
