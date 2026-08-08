using RssiCalibration.Core.Models;

namespace RssiCalibration.Core.Objectives.Basics
{
    /// <summary>
    /// Medián abszolút hiba. Robusztus, a kiugró értékeket gyakorlatilag figyelmen kívül hagyja.
    /// </summary>
    public sealed class MedianAbsoluteError : IErrorObjective
    {
        public string Name => "median";

        public double Evaluate(double[] signedErrors)
        {
            // Ha nincs hiba, akkor a medián abszolút hiba végtelen.
            if (signedErrors.Length == 0)
            {
                return double.PositiveInfinity;
            }

            // Kiszámítjuk az abszolút hibákat.
            double[] abs = new double[signedErrors.Length];
            for (int i = 0; i < signedErrors.Length; i++)
            {
                abs[i] = Math.Abs(signedErrors[i]);
            }

            // Rendezés a medián kiszámításához.
            Array.Sort(abs);
            // A medián abszolút hiba a 50. percentilis.
            return ErrorStatistics.Percentile(abs, 0.50);
        }
    }
}
