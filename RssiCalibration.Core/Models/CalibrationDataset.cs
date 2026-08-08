namespace RssiCalibration.Core.Models
{

    /// <summary>
    /// A kalibrációs adatkészletet reprezentáló osztály, amely tartalmazza az AP-kat és a méréseket.
    /// </summary>
    public sealed class CalibrationDataset
    {
        /// <summary>
        /// A mérésekben szereplő AP-k azonosítója szerinti gyors eléréséhez használt szótár.
        /// </summary>
        private readonly Dictionary<string, AccessPoint> _apsById;

        /// <summary>
        /// A hozzáférési pontok (Access Point-ok) listája.
        /// </summary>
        public IReadOnlyList<AccessPoint> AccessPoints { get; }

        /// <summary>
        /// A mérések listája, amelyek az AP-khoz kapcsolódnak.
        /// </summary>
        public IReadOnlyList<Measurement> Measurements { get; }

        /// <summary>
        /// Visszaadja a mérésekben szereplő AP azonosítóhoz tartozó Access Point objektumot.
        /// </summary>
        /// 
        /// <param name="m">
        /// A Measurement objektum, amelynek az AP azonosítóját szeretnénk lekérdezni.
        /// </param>
        /// 
        /// <returns>
        /// Az Access Point objektum, amely megfelel a mérésekben szereplő AP azonosítónak. (m.ApId == ap.Id)
        /// </returns>
        public AccessPoint ApOf(Measurement m) => _apsById[m.ApId];

        public CalibrationDataset(IReadOnlyList<AccessPoint> accessPoints, IReadOnlyList<Measurement> measurements)
        {
            AccessPoints = accessPoints;
            Measurements = measurements;

            // Access Point-ok átalakítása szótárrá az AP azonosítója szerint, hogy gyorsan elérhetőek legyenek.
            _apsById = accessPoints.ToDictionary(a => a.Id, StringComparer.OrdinalIgnoreCase);

            // Ellenőrizzük, hogy a mérésekben szereplő AP azonosítók mindegyike létezik-e az AP-k között.
            string[] orphans = measurements
                // Kiválasztjuk a mérésekben szereplő AP azonosítókat
                .Select(m => m.ApId)
                // Kiszűrjük azokat, amelyek nem találhatók meg az AP-k szótárában
                .Where(id => !_apsById.ContainsKey(id))
                // Eltávolítjuk az ismétlődő azonosítókat, figyelmen kívül hagyva a kis- és nagybetűket
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();

            // Ha az előző lépés eredményeként van olyan AP azonosító, amely nem található meg az AP-k között, dobunk egy kivételt.
            if (orphans.Length > 0)
            {
                throw new InvalidDataException($"A mérésekben ismeretlen AP azonosító(k) szerepelnek: {string.Join(", ", orphans)}");
            }
        }
    }
}