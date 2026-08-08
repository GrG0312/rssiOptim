using RssiCalibration.Core.Models;

namespace RssiCalibration.Core.Services
{
    /// <summary>
    /// <para>
    /// Zárt képletű becslés az n-re, log-térbeli legkisebb négyzetek módszerével.
    /// </para>
    /// 
    /// <code>
    /// log10(d) = (RSSI0 - RSSI) / (10n)
    /// </code>
    /// 
    /// Tehát 1/n lineáris együttható:
    /// 
    /// <code>
    /// y = log10(d_valós)
    /// x = (RSSI0 - RSSI) / 10   =>   y = x / n
    /// </code>
    /// 
    /// Iteráció nélkül, egy lépésben. Kiváló kiindulási/ellenőrző értéknek:
    /// ha a numerikus optimalizáló ettől nagyon messze áll meg, az gyanús.
    /// </summary>
    public static class LeastSquaresInitializer
    {
        /// <summary>
        /// Becsüli az n értékét a log-térbeli legkisebb négyzetek módszerével, a megadott minták alapján.
        /// </summary>
        /// 
        /// <param name="data">
        /// A kalibrációs adathalmaz, amely tartalmazza az AP-kat és a méréseket.
        /// </param>
        /// 
        /// <param name="samples">
        /// A minta mérések, amelyek alapján az n értékét becsülni szeretnénk.
        /// </param>
        /// 
        /// <returns>
        /// A becsült n értéke. Ha a minták nem alkalmasak a becslésre (például nincs elég adat), akkor NaN (Not-a-Number) értéket ad vissza.
        /// </returns>
        public static double EstimateN(CalibrationDataset data, IEnumerable<Measurement> samples)
        {
            double sumXy = 0;
            double sumYy = 0;

            // A minták feldolgozása, és a szükséges összegzések elvégzése
            foreach (Measurement m in samples)
            {
                // Csak azokat a mintákat vesszük figyelembe, amelyekhez van érvényes AP és pozitív távolság
                if (m.TrueDistance <= 0)
                {
                    continue;
                }

                // Az AP azonosító alapján lekérjük a hozzá tartozó AccessPoint objektumot
                // és kiszámítjuk az x és y értékeket a logaritmikus térben
                double x = (data.ApOf(m).Rssi0 - m.Rssi) / 10.0;
                double y = Math.Log10(m.TrueDistance);
                sumXy += x * y;
                sumYy += y * y;
            }

            // Origón átmenő illesztés: x = n * y  =>  n = sum(x * y) / sum(y * y)
            if (sumYy > 1e-12)
            {
                return sumXy / sumYy;
            }
            return double.NaN;
        }
    }
}