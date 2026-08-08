namespace RssiCalibration.Core.Objectives.Basics
{
    /// <summary>
    /// Átlagos abszolút hiba (MAE). 
    /// Kiegyensúlyozott, de érzékeny a kiugró értékekre.
    /// </summary>
    public sealed class MeanAbsoluteError : IErrorObjective
    {
        public string Name => "mean";

        public double Evaluate(double[] signedErrors)
        {
            // Ha nincs hiba, akkor a célfüggvény értéke végtelen, hogy a kereső algoritmus ne találja meg a nullát.
            if (signedErrors.Length == 0)
            {
                return double.PositiveInfinity;
            }

            // Átlagos abszolút hiba kiszámítása
            double sum = 0;
            foreach (double e in signedErrors)
            {
                sum += Math.Abs(e);
            }

            return sum / signedErrors.Length;
        }
    }
}
