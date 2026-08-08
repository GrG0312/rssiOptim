namespace RssiCalibration.Core.Objectives.Basics
{
    /// <summary>
    /// Négyzetes középhiba.
    /// Erősen bünteti a nagy hibákat.
    /// </summary>
    public sealed class RootMeanSquareError : IErrorObjective
    {
        public string Name => "rmse";

        public double Evaluate(double[] signedErrors)
        {
            // Ha nincs hiba, akkor a RMSE értéke végtelen (nincs hiba).
            if (signedErrors.Length == 0)
            {
                return double.PositiveInfinity;
            }

            // Négyzetes középhiba számítása
            double sum = 0;
            foreach (double e in signedErrors)
            {
                sum += e * e;
            }
            return Math.Sqrt(sum / signedErrors.Length);
        }
    }
}
