namespace RssiCalibration.Core.Models;

/// <summary>
/// Egy hibavektor (becsült - valós távolság, méterben) leíró statisztikái.
/// </summary>
public sealed record ErrorStatistics
{
    /// <summary>
    /// A minták száma, amelyekből a statisztikák készültek.
    /// </summary>
    public readonly int Count;

    /// <summary>
    /// Az előjeles hibák átlaga (szisztematikus torzítás mutatója).
    /// </summary>
    public readonly double MeanError;

    /// <summary>
    /// Az abszolút hibák átlaga (MAE – Mean Absolute Error).
    /// </summary>
    public readonly double MeanAbsError;

    /// <summary>
    /// Az abszolút hibák mediánja (kiugró értékekre robusztus mutató).
    /// </summary>
    public readonly double MedianAbsError;

    /// <summary>
    /// A négyzetes középérték gyöke (RMSE – Root Mean Square Error).
    /// </summary>
    public readonly double Rmse;

    /// <summary>
    /// A 90. percentilis abszolút hiba (P90): a hibák 90%-a ennél kisebb.
    /// </summary>
    public readonly double P90AbsError;

    /// <summary>
    /// A legnagyobb abszolút hiba.
    /// </summary>
    public readonly double MaxAbsError;

    /// <summary>
    /// Inicializálja a hibastatisztikákat a megadott értékekkel.
    /// </summary>
    public ErrorStatistics(int count, double meanError, double meanAbsError, double medianAbsError, double rmse, double p90AbsError, double maxAbsError)
    {
        Count = count;
        MeanError = meanError;
        MeanAbsError = meanAbsError;
        MedianAbsError = medianAbsError;
        Rmse = rmse;
        P90AbsError = p90AbsError;
        MaxAbsError = maxAbsError;
    }

    /// <summary>
    /// A hibákból kiszámítja a statisztikákat.
    /// </summary>
    ///
    /// <param name="signedErrors">
    /// A hibák (becsült - valós távolság, méterben) listája.
    /// </param>
    ///
    /// <returns>
    /// Egy <see cref="ErrorStatistics"/> példány, amely tartalmazza a hibák statisztikáit.
    /// </returns>
    public static ErrorStatistics From(IReadOnlyList<double> signedErrors)
    {
        if (signedErrors.Count == 0)
        {
            return new ErrorStatistics(0, 0, 0, 0, 0, 0, 0);
        }

        // Az abszolút hibák rendezett tömbjét készítjük el a percentilis-számításhoz
        double[] abs = new double[signedErrors.Count];

        for (int i = 0; i < signedErrors.Count; i++)
        {
            abs[i] = Math.Abs(signedErrors[i]);
        }

        Array.Sort(abs);

        // Az összegeket egyetlen végigfutással számoljuk ki
        double sum = 0;
        double sumAbs = 0;
        double sumSq = 0;
        foreach (double e in signedErrors)
        {
            sum += e;
            sumAbs += Math.Abs(e);
            sumSq += e * e;
        }

        return new ErrorStatistics(
            count: signedErrors.Count,
            meanError: sum / signedErrors.Count,
            meanAbsError: sumAbs / signedErrors.Count,
            medianAbsError: Percentile(abs, 0.50),
            rmse: Math.Sqrt(sumSq / signedErrors.Count),
            p90AbsError: Percentile(abs, 0.90),
            maxAbsError: abs[^1]);
    }

    /// <summary>
    /// A rendezett tömb q-kvantilisét számítja ki lineáris interpolációval.
    /// </summary>
    ///
    /// <remarks>
    /// A q-kvantilis a rendezett tömb azon értéke, amelynél a tömb q*100%-a kisebb vagy egyenlő.
    /// </remarks>
    ///
    /// <param name="sorted">Az előre rendezett értékek tömbje.</param>
    /// <param name="q">A kívánt kvantilis (0 és 1 között).</param>
    ///
    /// <returns>A q-kvantilis értéke.</returns>
    public static double Percentile(double[] sorted, double q)
    {
        if (sorted.Length == 0) return double.NaN;
        if (sorted.Length == 1) return sorted[0];

        // A q-kvantilis pozíciója a rendezett tömbben
        double pos = q * (sorted.Length - 1);
        int lo = (int)Math.Floor(pos);
        int hi = (int)Math.Ceiling(pos);

        // Ha a pozíció egész, pontosan az adott elem; egyébként lineáris interpoláció
        return lo == hi ? sorted[lo] : sorted[lo] + (pos - lo) * (sorted[hi] - sorted[lo]);
    }
}
