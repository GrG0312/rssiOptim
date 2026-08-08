namespace RssiCalibration.Core.Optimization
{
    /// <summary>
    /// Zárt intervallumot reprezentál [Min, Max] double értékekkel.
    /// </summary>
    public readonly record struct Interval
    {
        /// <summary>
        /// Az intervallum alsó határa.
        /// </summary>
        public readonly double Min;

        /// <summary>
        /// Az intervallum felső határa.
        /// </summary>
        public readonly double Max;

        /// <summary>
        /// Az intervallum hossza (Max - Min).
        /// </summary>
        public double Length => Max - Min;

        /// <summary>
        /// Inicializálja az intervallumot a megadott alsó és felső határral.
        /// </summary>
        ///
        /// <param name="min">Az intervallum alsó határa.</param>
        /// <param name="max">Az intervallum felső határa. Nagyobbnak kell lennie, mint <paramref name="min"/>.</param>
        ///
        /// <exception cref="ArgumentException">Ha a <paramref name="max"/> kisebb vagy egyenlő, mint a <paramref name="min"/>.</exception>
        public Interval(double min, double max)
        {
            if (max <= min)
            {
                throw new ArgumentException($"Érvénytelen intervallum: [{min}, {max}]");
            }
            Min = min;
            Max = max;
        }

        /// <summary>
        /// Levágja a megadott értéket az intervallum határaira: ha kisebb, mint Min, Min-t ad vissza;
        /// ha nagyobb, mint Max, Max-ot ad vissza; egyébként magát az értéket.
        /// </summary>
        ///
        /// <param name="x">A levágandó érték.</param>
        ///
        /// <returns>
        /// A levágott érték, amely az [Min, Max] intervallumba esik.
        /// </returns>
        public double Clamp(double x)
        {
            return Math.Clamp(x, Min, Max);
        }

        /// <summary>
        /// Egy <see cref="Interval"/>-t hoz létre a megadott minimum és maximum értékekből.
        /// Ha a maximum kisebb, mint a minimum, akkor az értékeket felcseréli.
        /// </summary>
        ///
        /// <param name="min">A létrehozandó intervallum minimum értéke.</param>
        /// <param name="max">A létrehozandó intervallum maximum értéke.</param>
        ///
        /// <returns>
        /// A létrehozott <see cref="Interval"/> objektum, amely a megadott minimum és maximum értékekből áll.
        /// </returns>
        public static Interval Of(double min, double max)
        {
            if (max >= min)
            {
                return new Interval(min, max);
            }
            else
            {
                return new Interval(max, min);
            }
        }
    }
}
