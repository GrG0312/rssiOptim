using RssiCalibration.Core.Models;
using System.Globalization;
using System.Text;

namespace RssiCalibration.Cli;

/// <summary>
/// CSV formátumú riportfájlokat ír a kalibrációs eredményekből.
/// Három típusú riportot tud előállítani: összefoglaló, reziduálisok és n-sweep görbe.
/// Az oszlopelválasztó minden esetben a bemeneti fájloknál is használt karakter,
/// így a kimenet ugyanazzal a beállítással olvasható vissza, amivel a bemenet készült.
/// </summary>
internal static class CsvReportWriter
{
    /// <summary>
    /// Az invariáns kultúra, amelyet a számok formázásához használunk a CSV-ben.
    /// </summary>
    private static readonly CultureInfo C = CultureInfo.InvariantCulture;

    /// <summary>
    /// Az <c>ApIds</c> oszlopon belüli listát elválasztó karakter. Alapesetben szóköz,
    /// de ha maga az oszlopelválasztó is szóköz, akkor vesszőre váltunk, hogy a lista
    /// egyetlen mezőben maradjon.
    /// </summary>
    ///
    /// <param name="separator">Az oszlopelválasztó karakter.</param>
    private static char ListJoin(char separator) => separator == ' ' ? ',' : ' ';

    /// <summary>
    /// Összefoglaló riportot ír a megadott fájlba: stratégiánként és csoportonként
    /// az optimális n, az RSSI0 eltolás és a hibastatisztikák.
    /// </summary>
    ///
    /// <param name="path">A kimeneti CSV fájl elérési útja.</param>
    /// <param name="results">A kalibrációs eredmények listája.</param>
    /// <param name="separator">Az oszlopelválasztó karakter.</param>
    public static void WriteSummary(string path, IReadOnlyList<CalibrationResult> results, char separator)
    {
        char d = separator;
        char join = ListJoin(separator);

        StringBuilder sb = new StringBuilder();
        sb.AppendLine(string.Join(d,
            "Strategy", "Group", "N", "Rssi0Offset", "Objective", "Count",
            "MeanError", "MAE", "MedianAE", "RMSE", "P90", "MaxAE", "ApIds"));

        foreach (CalibrationResult r in results)
        {
            ErrorStatistics s = r.Statistics;
            sb.AppendLine(string.Create(C,
                $"{r.StrategyName}{d}{r.Group.Value}{d}{r.OptimalN:0.0000}{d}{r.Rssi0Offset:0.00}{d}" +
                $"{r.ObjectiveValue:0.0000}{d}{s.Count}{d}{s.MeanError:0.0000}{d}{s.MeanAbsError:0.0000}{d}" +
                $"{s.MedianAbsError:0.0000}{d}{s.Rmse:0.0000}{d}{s.P90AbsError:0.0000}{d}{s.MaxAbsError:0.0000}{d}" +
                $"{string.Join(join, r.ApIds)}"));
        }

        File.WriteAllText(path, sb.ToString(), Encoding.UTF8);
    }

    /// <summary>
    /// Részletes reziduális riportot ír: minden egyes méréshez az RSSI értéket,
    /// a valós és becsült távolságot, illetve a hibát.
    /// </summary>
    ///
    /// <param name="path">A kimeneti CSV fájl elérési útja.</param>
    /// <param name="results">A kalibrációs eredmények listája, amelyekből a reziduálisokat kigyűjtjük.</param>
    /// <param name="separator">Az oszlopelválasztó karakter.</param>
    public static void WriteResiduals(string path, IReadOnlyList<CalibrationResult> results, char separator)
    {
        char d = separator;

        StringBuilder sb = new StringBuilder();
        sb.AppendLine(string.Join(d,
            "Strategy", "Group", "N", "ApId", "PointId", "Vendor", "FrequencyGHz",
            "Rssi", "TrueDistance", "EstimatedDistance", "Error", "AbsError"));

        foreach (CalibrationResult r in results)
        foreach (ResidualRow row in r.Residuals)
            sb.AppendLine(string.Create(C,
                $"{r.StrategyName}{d}{r.Group.Value}{d}{r.OptimalN:0.0000}{d}{row.ApId}{d}{row.PointId}{d}" +
                $"{row.Vendor}{d}{row.FrequencyGHz:0.###}{d}" +
                $"{row.Rssi:0.0}{d}{row.TrueDistance:0.0000}{d}{row.EstimatedDistance:0.0000}{d}" +
                $"{row.Error:0.0000}{d}{Math.Abs(row.Error):0.0000}"));

        File.WriteAllText(path, sb.ToString(), Encoding.UTF8);
    }

    /// <summary>
    /// Az n → célfüggvényérték görbét írja CSV-be csoportonként, ahol az első oszlop
    /// az n értéke, a többi oszlop az egyes csoportok célfüggvényértékei.
    /// </summary>
    ///
    /// <param name="path">A kimeneti CSV fájl elérési útja.</param>
    /// <param name="curves">A csoportonkénti (n, érték) párok szótára.</param>
    /// <param name="separator">Az oszlopelválasztó karakter.</param>
    public static void WriteSweep(
        string path,
        IReadOnlyDictionary<string, IReadOnlyList<(double N, double Value)>> curves,
        char separator)
    {
        if (curves.Count == 0) return;

        List<string> groups = curves.Keys.ToList();
        int length = curves[groups[0]].Count;

        StringBuilder sb = new StringBuilder();
        sb.AppendLine("N" + separator + string.Join(separator, groups));

        // Soronként kiírjuk az n értéket, majd az összes csoport célfüggvényértékét
        for (int i = 0; i < length; i++)
        {
            sb.Append(curves[groups[0]][i].N.ToString("0.0000", C));
            foreach (string g in groups)
            {
                sb.Append(separator);
                sb.Append(curves[g][i].Value.ToString("0.0000", C));
            }
            sb.AppendLine();
        }

        File.WriteAllText(path, sb.ToString(), Encoding.UTF8);
    }
}
