namespace RssiCalibration.Core.Optimization
{

    /// <summary>
    /// Durva rácskeresés a globális minimum környékének bekerítésére, majd
    /// arany metszés a szomszédos rácspontok közti finomításra.
    /// A legjobb kompromisszum: globális biztonság + tetszőleges pontosság.
    /// </summary>
    public sealed class HybridOptimizer : IOptimizer1D
    {
        /// <summary>
        /// A durva rácskereséshez használt optimalizáló.
        /// </summary>
        private readonly GridSearchOptimizer _grid;

        /// <summary>
        /// A finomításra használt aranymetszéses optimalizáló.
        /// </summary>
        private readonly GoldenSectionOptimizer _refine;

        /// <summary>
        /// A durva rácskereséshez használt rácspontok száma.
        /// </summary>
        private readonly int _coarseSteps;

        public string Name => $"hybrid({_coarseSteps})";

        public HybridOptimizer(int coarseSteps = 201, double tolerance = 1e-7)
        {
            _coarseSteps = coarseSteps;
            _grid = new GridSearchOptimizer(coarseSteps);
            _refine = new GoldenSectionOptimizer(tolerance);
        }

        /// <inheritdoc/>
        public Optimum1D Minimize(Func<double, double> objective, Interval bounds)
        {
            Optimum1D coarse = _grid.Minimize(objective, bounds);
            double stepSize = bounds.Length / (_coarseSteps - 1);

            Interval localBounds = Interval.Of(
                bounds.Clamp(coarse.X - stepSize),
                bounds.Clamp(coarse.X + stepSize));

            Optimum1D fine = _refine.Minimize(objective, localBounds);

            return fine.Value <= coarse.Value
                ? fine with { Evaluations = fine.Evaluations + coarse.Evaluations }
                : coarse with { Evaluations = fine.Evaluations + coarse.Evaluations };
        }
    }
}