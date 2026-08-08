namespace RssiCalibration.Core.Optimization
{
    /// <summary>
    /// Interfész az 1D optimalizáló algoritmusokhoz, amelyek célja egy adott célfüggvény minimalizálása egy adott intervallumban.
    /// </summary>
    public interface IOptimizer1D
    {
        /// <summary>
        /// A célfüggvény neve, amelyet a kalibrációs folyamat során a logban és a fájlnevekben használnak.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Minimalizálja a megadott célfüggvényt az adott intervallumban, és visszaadja a minimum adatait tartalmazó <see cref="Optimum1D"/> objektumot.
        /// </summary>
        /// 
        /// <param name="objective">
        /// A minimalizálandó célfüggvény, amely egy double típusú bemenetet vesz és egy double típusú kimenetet ad vissza.
        /// </param>
        /// 
        /// <param name="bounds">
        /// Az intervallum, amelyen belül a célfüggvényt minimalizálni kell.
        /// Ez egy <see cref="Interval"/> típusú objektum, amely a minimális és maximális értékeket tartalmazza.
        /// </param>
        /// 
        /// <returns>
        /// Egy <see cref="Optimum1D"/> objektum, amely tartalmazza a minimum helyét (X), a minimum értékét (Value) és a kiértékelések számát (Evaluations).
        /// </returns>
        Optimum1D Minimize(Func<double, double> objective, Interval bounds);
    }
}