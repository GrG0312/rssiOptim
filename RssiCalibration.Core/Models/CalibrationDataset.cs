namespace RssiCalibration.Core.Models
{

    /// <summary>
    /// A kalibrációs adatkészletet reprezentáló osztály, amely tartalmazza az eszközkatalógust
    /// (gyártó + frekvencia páronként az RSSI0-t) és a mérési leolvasásokat.
    /// </summary>
    public sealed class CalibrationDataset
    {
        /// <summary>
        /// Az eszközök gyors elérése a gyártó + frekvencia páros szerint.
        /// </summary>
        private readonly Dictionary<DeviceKey, AccessPoint> _apsByDevice;

        /// <summary>
        /// Az eszközkatalógus: gyártó + frekvencia páronként egy bejegyzés.
        /// </summary>
        public IReadOnlyList<AccessPoint> AccessPoints { get; }

        /// <summary>
        /// A mérések listája, amelyek egy-egy eszköz leolvasását írják le egy AP-helyen.
        /// </summary>
        public IReadOnlyList<Measurement> Measurements { get; }

        /// <summary>
        /// Visszaadja a méréshez tartozó eszközt (gyártó + frekvencia páros szerint).
        /// </summary>
        ///
        /// <param name="m">
        /// A Measurement objektum, amelynek az eszközét szeretnénk lekérdezni.
        /// </param>
        ///
        /// <returns>
        /// Az AccessPoint objektum, amely megfelel a mérésben szereplő gyártó + frekvencia párosnak.
        /// </returns>
        public AccessPoint DeviceOf(Measurement m) => _apsByDevice[m.Device];

        public CalibrationDataset(IReadOnlyList<AccessPoint> accessPoints, IReadOnlyList<Measurement> measurements)
        {
            AccessPoints = accessPoints;
            Measurements = measurements;

            // Az eszközök átalakítása szótárrá a gyártó + frekvencia páros szerint, hogy gyorsan elérhetőek legyenek.
            _apsByDevice = accessPoints.ToDictionary(a => a.Key);

            // Ellenőrizzük, hogy a mérésekben szereplő minden gyártó + frekvencia páros létezik-e a katalógusban.
            string[] orphans = measurements
                // Kiválasztjuk a mérésekben szereplő eszközöket
                .Select(m => m.Device)
                // Kiszűrjük azokat, amelyek nem találhatók meg az eszközök szótárában
                .Where(key => !_apsByDevice.ContainsKey(key))
                // Eltávolítjuk az ismétlődő párosokat
                .Distinct()
                .Select(key => key.ToString())
                .ToArray();

            // Ha az előző lépés eredményeként van olyan eszköz, amely nem található meg a katalógusban, dobunk egy kivételt.
            if (orphans.Length > 0)
            {
                throw new InvalidDataException(
                    $"A leolvasásokban ismeretlen gyártó + frekvencia páros(ok) szerepelnek: {string.Join(", ", orphans)}");
            }
        }
    }
}
