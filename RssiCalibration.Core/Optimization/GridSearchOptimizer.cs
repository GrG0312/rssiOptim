namespace RssiCalibration.Core.Optimization;

/// <summary>
/// Egyenletes rácskeresés. Nem elegáns, de a mi feladatméretünknél (néhány ezer
/// kiértékelés) ez ezredmásodperc, és garantáltan megtalálja a globális minimumot
/// a rács felbontásán belül. Fontos, mert a medián alapú célfüggvény lépcsős,
/// nem sima és nem unimodális - a gradiens- vagy metszet-alapú módszerek
/// beragadhatnak egy lokális minimumba.
/// </summary>
public sealed class GridSearchOptimizer : IOptimizer1D
{
    /// <summary>
    /// A rács felbontása, azaz hány pontot vizsgálunk a megadott intervallumban.
    /// </summary>
    private readonly int _steps;

    public string Name => $"grid({_steps})";

    public GridSearchOptimizer(int steps = 5001)
    {
        if (steps < 2)
        {
            throw new ArgumentOutOfRangeException(nameof(steps), "Legalább 2 lépés kell.");
        }
        _steps = steps;
    }

    /// <inheritdoc/>
    public Optimum1D Minimize(Func<double, double> objective, Interval bounds)
    {
        // A rács lépésköze az intervallum hossza és a lépések száma alapján.
        double stepSize = bounds.Length / (_steps - 1);

        // Eredmény változók
        double bestX = bounds.Min;
        double bestValue = double.PositiveInfinity;

        // Végigmegyünk a rács pontjain, és kiértékeljük a célfüggvényt.
        for (int i = 0; i < _steps; i++)
        {
            double x = bounds.Min + i * stepSize;
            double value = objective(x);
            if (value < bestValue)
            {
                bestValue = value;
                bestX = x;
            }
        }

        return new Optimum1D(bestX, bestValue, _steps);
    }

    /// <summary>
    /// Teljes rács kiértékelése az n -> hiba görbe exportálásához.
    /// </summary>
    public IEnumerable<(double N, double Value)> Sweep(Func<double, double> objective, Interval bounds)
    {
        double stepSize = bounds.Length / (_steps - 1);
        for (double i = 0; i < _steps; i++)
        {
            double x = bounds.Min + i * stepSize;
            yield return (x, objective(x));
        }
    }
}
