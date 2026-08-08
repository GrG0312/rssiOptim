using RssiCalibration.Core.Models;

namespace RssiCalibration.Core.Objectives.Basics
{
    /// <summary>
    /// Robusztus kompozit hibafüggvény, amely a hibák abszolút értékének mediánját és egy megadott kvantilisét kombinálja.
    /// Hasznos lehet olyan esetekben, amikor a hibák eloszlása nem normális (vagyis sok különböző mértékű hiba van),
    /// és a kiugró értékek torzíthatják az átlagot.
    /// </summary>
    public sealed class RobustComposite : IErrorObjective
    {
        /// <summary>
        /// A kvantilis súlya a kompozit hibafüggvényben.
        /// 0 esetén csak a mediánt vesszük figyelembe, 1 esetén csak a kvantilis értékét.
        /// </summary>
        private readonly double _tailWeight;

        /// <summary>
        /// A figyelembe vett kvantilis szintje (0 és 1 között), amely a hibák abszolút
        /// értékének eloszlásából kerül kiszámításra.
        /// </summary>
        private readonly double _tailQuantile;

        /// <summary>
        /// Inicializálja a robusztus kompozit hibafüggvényt a megadott súllyal és kvantilissel.
        /// </summary>
        ///
        /// <param name="tailWeight">A kvantilis súlya (0–1). Alapértelmezés: 0.3.</param>
        /// <param name="tailQuantile">A figyelembe vett kvantilis szintje. Alapértelmezés: 0.90.</param>
        public RobustComposite(double tailWeight = 0.3, double tailQuantile = 0.90)
        {
            _tailWeight = tailWeight;
            _tailQuantile = tailQuantile;
        }

        /// <inheritdoc />
        public string Name => $"composite(w={_tailWeight:0.##},q={_tailQuantile:0.##})";

        /// <inheritdoc />
        public double Evaluate(double[] signedErrors)
        {
            // Ha nincsenek hibák, akkor a hibafüggvény értéke végtelen.
            if (signedErrors.Length == 0)
            {
                return double.PositiveInfinity;
            }

            // Számítsuk ki a hibák abszolút értékeit, majd rendezzük azokat.
            double[] abs = new double[signedErrors.Length];
            for (int i = 0; i < signedErrors.Length; i++)
            {
                abs[i] = Math.Abs(signedErrors[i]);
            }
            Array.Sort(abs);

            // Kombináljuk a mediánt és a tailQuantile-t a tailWeight súlyával.
            double median = ErrorStatistics.Percentile(abs, 0.50);
            double tail = ErrorStatistics.Percentile(abs, _tailQuantile);
            return (1 - _tailWeight) * median + _tailWeight * tail;
        }
    }
}
