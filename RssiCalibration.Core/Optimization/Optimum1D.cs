namespace RssiCalibration.Core.Optimization
{
    /// <summary>
    /// Az egyváltozós optimalizáció eredménye, amely tartalmazza a minimum helyét, az értékét és a kiértékelések számát.
    /// </summary>
    public sealed record Optimum1D
    {
        /// <summary>
        /// A minimum helye az egyváltozós függvényben.
        /// </summary>
        /// <remarks>
        /// A három érték init-only property, nem readonly mező: a rekord <c>with</c>
        /// kifejezése csak így tud másolatot készíteni (pl. a hibrid optimalizáló a
        /// kiértékelések számát összegzi a két fázisból).
        /// </remarks>
        public double X { get; init; }

        /// <summary>
        /// A minimum értéke az egyváltozós függvényben (Y).
        /// </summary>
        public double Value { get; init; }

        /// <summary>
        /// A kiértékelések száma, amely a minimum megtalálásához szükséges volt.
        /// </summary>
        public int Evaluations { get; init; }

        public Optimum1D(double x, double value, int evaluations)
        {
            X = x;
            Value = value;
            Evaluations = evaluations;
        }
    }
}
