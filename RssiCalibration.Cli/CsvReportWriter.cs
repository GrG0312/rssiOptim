using RssiCalibration.Core.Models;
using System.Globalization;
using System.Text;

namespace RssiCalibration.Cli;

/// <summary>
/// CSV formátumú riportfájlokat ír a kalibrációs eredményekből.
/// Három típusú riportot tud előállítani: összefoglaló, reziduálisok és n-sweep görbe.
/// </summary>
internal static class CsvReportWriter
{
    /// <summary>
    /// Az invariáns kultúra, amelyet a számok formázásához használunk a CSV-ben.
    /// </summary>
    private static readonly CultureInfo C = CultureInfo.InvariantCulture;

    /// <summary>
    /// Összefoglaló riportot ír a megadott fájlba: stratégiánként és csoportonként
    /// az optimális n, az RSSI0 eltolás és a hibastatisztikák.
    /// </summary>
    ///
    /// <param name="path">A kimeneti CSV fájl elérési útja.</param>
    /// <param name="results">A kalibrációs eredmények listája.</param>
    public static void WriteSummary(string path, IReadOnlyList<CalibrationResult> results)
    {
        StringBuilder sb = new StringBuilder();
        sb.AppendLine("Strategy,Group,N,Rssi0Offset,Objective,Count,MeanError,MAE,MedianAE,RMSE,P90,MaxAE,ApIds");

        foreach (CalibrationResult r in results)
        {
            ErrorStatistics s = r.Statistics;
            sb.AppendLine(string.Create(C,
                $"{r.StrategyName},{r.Group.Value},{r.OptimalN:0.0000},{r.Rssi0Offset:0.00}," +
                $"{r.ObjectiveValue:0.0000},{s.Count},{s.MeanError:0.0000},{s.MeanAbsError:0.0000}," +
                $"{s.MedianAbsError:0.0000},{s.Rmse:0.0000},{s.P90AbsError:0.0000},{s.MaxAbsError:0.0000}," +
                $"{string.Join(' ', r.ApIds)}"));
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
    public static void WriteResiduals(string path, IReadOnlyList<CalibrationResult> results)
    {
        StringBuilder sb = new StringBuilder();
        sb.AppendLine("Strategy,Group,N,ApId,PointId,Rssi,TrueDistance,EstimatedDistance,Error,AbsError");

        foreach (CalibrationResult r in results)
        foreach (ResidualRow row in r.Residuals)
            sb.AppendLine(string.Create(C,
                $"{r.StrategyName},{r.Group.Value},{r.OptimalN:0.0000},{row.ApId},{row.PointId}," +
                $"{row.Rssi:0.0},{row.TrueDistance:0.0000},{row.EstimatedDistance:0.0000}," +
                $"{row.Error:0.0000},{Math.Abs(row.Error):0.0000}"));

        File.WriteAllText(path, sb.ToString(), Encoding.UTF8);
    }

    /// <summary>
    /// Az n → célfüggvényérték görbét írja CSV-be csoportonként, ahol az első oszlop
    /// az n értéke, a többi oszlop az egyes csoportok célfüggvényértékei.
    /// </summary>
    ///
    /// <param name="path">A kimeneti CSV fájl elérési útja.</param>
    /// <param name="curves">A csoportonkénti (n, érték) párok szótára.</param>
    public static void WriteSweep(
        string path,
        IReadOnlyDictionary<string, IReadOnlyList<(double N, double Value)>> curves)
    {
        if (curves.Count == 0) return;

        List<string> groups = curves.Keys.ToList();
        int length = curves[groups[0]].Count;

        StringBuilder sb = new StringBuilder();
        sb.AppendLine("N," + string.Join(',', groups));

        // Soronként kiírjuk az n értéket, majd az összes csoport célfüggvényértékét
        for (int i = 0; i < length; i++)
        {
            sb.Append(curves[groups[0]][i].N.ToString("0.0000", C));
            foreach (string g in groups)
            {
                sb.Append(',');
                sb.Append(curves[g][i].Value.ToString("0.0000", C));
            }
            sb.AppendLine();
        }

        File.WriteAllText(path, sb.ToString(), Encoding.UTF8);
    }
}
