namespace RssiCalibration.Core.Optimization
{
    /// <summary>
    /// Az optimalizációs algoritmusok gyártója, amely leképezi a nevet a megfelelő optimalizáló implementációjára.
    /// </summary>
    public static class OptimizerFactory
    {
        /// <summary>
        /// Létrehoz egy optimalizálót a megadott név alapján.
        /// </summary>
        /// 
        /// <param name="name">
        /// Az optimalizáló neve. Lehetséges értékek: "grid", "golden", "hybrid".
        /// </param>
        /// 
        /// <returns>
        /// Az optimalizáló implementációja, amely az IOptimizer1D interfészt valósítja meg.
        /// </returns>
        /// 
        /// <exception cref="ArgumentException"></exception>
        public static IOptimizer1D Create(string name) => name.ToLowerInvariant() switch
        {
            "grid" => new GridSearchOptimizer(),
            "golden" => new GoldenSectionOptimizer(),
            "hybrid" => new HybridOptimizer(),
            _ => throw new ArgumentException(
                $"Ismeretlen optimalizáló: '{name}'. Elérhető: grid, golden, hybrid.")
        };
    }
}