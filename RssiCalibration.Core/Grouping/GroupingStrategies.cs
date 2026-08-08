namespace RssiCalibration.Core.Grouping
{
    /// <summary>
    /// Tartalmazza az összes elérhető csoportosítási stratégiát, és lehetővé teszi azok létrehozását név alapján.
    /// </summary>
    public static class GroupingStrategies
    {
        /// <summary>
        /// Az összes elérhető csoportosítási stratégia tömbje.
        /// </summary>
        public static IReadOnlyList<IGroupingStrategy> All { get; } = [
            new GlobalStrategy(),
            new VendorStrategy(),
            new BandStrategy(),
            new VendorBandStrategy(),
            new PerApStrategy()
        ];

        /// <summary>
        /// Létrehoz egy csoportosítási stratégiát a megadott név alapján.
        /// </summary>
        /// 
        /// <param name="name">
        /// A csoportosítási stratégia neve, amelyet létre szeretnénk hozni. A név nem érzékeny a kis- és nagybetűkre.
        /// </param>
        /// 
        /// <returns>
        /// A létrehozott csoportosítási stratégia, amely megfelel a megadott névnek.
        /// </returns>
        /// 
        /// <exception cref="ArgumentException"></exception>
        public static IGroupingStrategy Create(string name)
        {
            return All.FirstOrDefault(s => string.Equals(s.Name, name, StringComparison.OrdinalIgnoreCase))
                ?? throw new ArgumentException(
                    $"Ismeretlen csoportosítás: '{name}'. Elérhető: {string.Join(", ", All.Select(s => s.Name))}.");
        }
    }
}
