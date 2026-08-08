namespace RssiCalibration.Core.Optimization
{

    /// <summary>
    /// Arany metszés szerinti keresés. Gyors, de CSAK unimodális célfüggvényre (unimodális = egyetlen minimum van)
    /// garantált. Sima célfüggvényekre (RMSE, Huber) használható; mediánra nem ajánlott.
    /// </summary>
    public sealed class GoldenSectionOptimizer : IOptimizer1D
    {
        /// <summary>
        /// A konvergencia kritérium, azaz a keresési intervallum hossza, amely alatt a minimumot elfogadjuk.
        /// </summary>
        private readonly double _tolerance;

        /// <summary>
        /// A maximális iterációk száma, amely után a keresést leállítjuk, ha nem konvergált.
        /// </summary>
        private readonly int _maxIterations;

        /// <summary>
        /// Az aranymetszés aránya, amelyet a keresési intervallum felosztására használunk.
        /// </summary>
        private static readonly double PHI = (Math.Sqrt(5.0) - 1.0) / 2.0; // ~0.618

        public string Name => "golden-section";

        public GoldenSectionOptimizer(double tolerance = 1e-6, int maxIterations = 200)
        {
            if (tolerance <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(tolerance), "A tolerancia pozitív kell legyen.");
            }
            if (maxIterations <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(maxIterations), "A maximális iterációk száma pozitív kell legyen.");
            }
            _tolerance = tolerance;
            _maxIterations = maxIterations;
        }

        public Optimum1D Minimize(Func<double, double> objective, Interval bounds)
        {
            double a = bounds.Min;
            double b = bounds.Max;

            double c = b - PHI * (b - a);
            double d = a + PHI * (b - a);

            double fc = objective(c), fd = objective(d);
            int evals = 2;

            for (int i = 0; i < _maxIterations && Math.Abs(b - a) > _tolerance; i++)
            {
                if (fc < fd)
                {
                    b = d;
                    d = c;
                    fd = fc;
                    c = b - PHI * (b - a);
                    fc = objective(c);
                }
                else
                {
                    a = c;
                    c = d;
                    fc = fd;
                    d = a + PHI * (b - a);
                    fd = objective(d);
                }
                evals++;
            }

            double x = (a + b) / 2.0;
            return new Optimum1D(x, objective(x), evals + 1);
        }
    }
}