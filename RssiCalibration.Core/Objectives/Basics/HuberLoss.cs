namespace RssiCalibration.Core.Objectives.Basics
{
    /// <summary>
    /// A Huber-veszteségfüggvény implementációja.
    /// A Huber-veszteség a hibák abszolút értékét és négyzetét kombinálja, így érzékeny a kiugró értékekre,
    /// de nem annyira, mint a négyzetes veszteség.
    /// </summary>
    public sealed class HuberLoss : IErrorObjective
    {
        /// <summary>
        /// A Huber-veszteség küszöbértéke. A hibák abszolút értéke alatti hibák négyzetesen,
        /// a feletti hibák lineárisan növelik a veszteséget.
        /// </summary>
        private readonly double _delta;

        /// <summary>
        /// Inicializálja a Huber-veszteségfüggvényt a megadott küszöbértékkel.
        /// </summary>
        ///
        /// <param name="delta">
        /// A küszöbérték, amely felett a veszteség lineárissá válik. Alapértelmezés: 10.0.
        /// </param>
        public HuberLoss(double delta = 10.0)
        {
            _delta = delta;
        }

        /// <inheritdoc />
        public string Name => $"huber({_delta:0.#})";

        /// <inheritdoc />
        public double Evaluate(double[] signedErrors)
        {
            // Ha nincs hiba, a veszteség végtelen, mivel nincs adat a kiértékeléshez.
            if (signedErrors.Length == 0)
            {
                return double.PositiveInfinity;
            }

            // A Huber-veszteség kiszámítása a hibák abszolút értéke alapján.
            double sum = 0;
            foreach (double e in signedErrors)
            {
                double a = Math.Abs(e);

                if (a <= _delta)
                {
                    sum += 0.5 * e * e; // Négyzetes veszteség a küszöb alatt
                }
                else
                {
                    sum += _delta * (a - 0.5 * _delta); // Lineáris veszteség a küszöb felett
                }
            }
            return sum / signedErrors.Length;
        }
    }
}
