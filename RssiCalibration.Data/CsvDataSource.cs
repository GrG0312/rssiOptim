using RssiCalibration.Core.Models;

namespace RssiCalibration.Data
{

    /// <summary>
    /// CSV fájlokból betöltő adatforrás. A használt CSV fájlok a(z) <c>access-points.csv</c> és a <c>measurements.csv</c>.
    /// </summary>
    public sealed class CsvDataSource
    {
        /// <summary>
        /// A CSV fájl elérési útja, amely az AP-ket tartalmazza.
        /// </summary>
        private readonly string accessPointsPath;

        /// <summary>
        /// A CSV fájl elérési útja, amely a méréseket tartalmazza.
        /// </summary>
        private readonly string measurementsPath;

        /// <summary>
        /// A minták aggregálásának módja. Alapértelmezésben nincs aggregálás.
        /// </summary>
        private readonly SampleAggregation aggregation;

        /// <summary>
        /// A CSV fájlban használt elválasztó karakter. Alapértelmezésben pontosvessző (<c>;</c>).
        /// </summary>
        private readonly char separator;

        /// <summary>
        /// Inicializálja a CSV adatforrást a megadott fájlokkal és beállításokkal.
        /// </summary>
        /// 
        /// <param name="accessPointsPath">
        /// A CSV fájl elérési útja, amely az AP-ket tartalmazza.
        /// A fájlnak a következő oszlopokat kell tartalmaznia: <c>apid</c>, <c>vendor</c>, <c>frequencymhz</c>, <c>rssi0</c>.
        /// </param>
        /// 
        /// <param name="measurementsPath">
        /// A CSV fájl elérési útja, amely a méréseket tartalmazza.
        /// A fájlnak a következő oszlopokat kell tartalmaznia: <c>apid</c>, <c>pointid</c>, <c>rssi</c>, <c>truedistance</c>.
        /// </param>
        /// 
        /// <param name="aggregation">
        /// A minták aggregálásának módja. Alapértelmezésben nincs aggregálás.
        /// </param>
        /// 
        /// <param name="separator">
        /// 
        /// </param>
        public CsvDataSource(string accessPointsPath, string measurementsPath, SampleAggregation aggregation = SampleAggregation.None, char separator = ';')
        {
            this.accessPointsPath = accessPointsPath;
            this.measurementsPath = measurementsPath;
            this.aggregation = aggregation;
            this.separator = separator;
        }

        public CalibrationDataset Load()
        {
            List<AccessPoint> aps =
                // Beolvassuk a CSV fájlból az értékekekt
                CsvReader.Read(accessPointsPath, separator)
                // Minden sorból létrehozunk egy AccessPoint objektumot
                .Select(r => new AccessPoint(
                    id: CsvReader.Required(r, "apid", accessPointsPath),
                    vendor: CsvReader.Required(r, "vendor", accessPointsPath),
                    frequencyMHz: CsvReader.Int(r, "frequencymhz", accessPointsPath),
                    rssi0: CsvReader.Double(r, "rssi0", accessPointsPath)))
                // Az összes AccessPoint objektumot listává alakítjuk
                .ToList();

            if (aps.Count == 0)
            {
                throw new InvalidDataException("Nem tartalmaz AP-t az access-points fájl.");
            }

            string[] duplicates =
                // Csoportosítsuk AP-kat Id alapján
                aps.GroupBy(a => a.Id, StringComparer.OrdinalIgnoreCase)
                // Szűrjük ki azokat a csoportokat, amelyek több mint egy elemet tartalmaznak (ismétlődő AP-k)
                .Where(g => g.Count() > 1)
                // Válasszuk ki az ismétlődő AP-k azonosítóit
                .Select(g => g.Key)
                .ToArray();

            // Ha vannak ismétlődő AP-k, dobjunk kivételt
            if (duplicates.Length > 0)
            {
                throw new InvalidDataException($"Ismétlődő AP azonosító: {string.Join(", ", duplicates)}");
            }

            // Amennyiben nincs ismétlődés, folytassuk a mérések beolvasásával
            // Gondolkodás hasonló mint az AP-knál
            List<Measurement> measurements =
                // Beolvassuk a CSV fájlból az értékeket
                CsvReader.Read(measurementsPath, separator)
                // Minden sorból létrehozunk egy Measurement objektumot
                .Select(r => new Measurement(
                    apId: CsvReader.Required(r, "apid", measurementsPath),
                    pointId: CsvReader.Required(r, "pointid", measurementsPath),
                    rssi: CsvReader.Double(r, "rssi", measurementsPath),
                    trueDistance: CsvReader.Double(r, "truedistance", measurementsPath)))
                .ToList();

            // Ha nincs mérés, dobjunk kivételt
            if (measurements.Count == 0)
            {
                throw new InvalidDataException("Nem tartalmaz mérést a measurements fájl.");
            }

            // Ha van kért aggregálás, végezzük el az aggregálást a méréseken
            if (aggregation != SampleAggregation.None)
            {
                measurements = Aggregate(measurements, aggregation);
            }

            // Visszaadjuk a létrehozott CalibrationDataset objektumot az AP-k és mérések listájával
            return new CalibrationDataset(aps, measurements);
        }

        /// <summary>
        /// Aggregálja a méréseket az AP azonosító és a pont azonosító alapján, a megadott aggregálási mód szerint.
        /// </summary>
        /// 
        /// <param name="raw">
        /// A nyers mérések listája, amelyet aggregálni kell.
        /// </param>
        /// 
        /// <param name="mode">
        /// A minták aggregálásának módja, amely meghatározza, hogy az átlagot vagy a mediánt számoljuk-e.
        /// </param>
        /// 
        /// <returns>
        /// Egy új listát, amely az aggregált méréseket tartalmazza, az AP azonosító és a pont azonosító szerint csoportosítva.
        /// </returns>
        private static List<Measurement> Aggregate(List<Measurement> raw, SampleAggregation mode)
        {
            // Egy fail-safe ellenőrzés, hogy ha nincs aggregálás, akkor egyszerűen visszaadjuk a nyers méréseket
            if (mode == SampleAggregation.None)
            {
                return raw;
            }

            return raw
                // Csoportosítsuk a méréseket az AP azonosító és a pont azonosító alapján
                // Ez azt eredményezi, hogy ha van egy mérés, ahol az AccessPoint azonosítója és a mérési pont azonosítója megegyezik,
                // akkor azok egy 'csoportba' kerülnek
                .GroupBy(m => (m.ApId, m.PointId))
                // Minden csoportot átkonvertálunk egyetlen pontra a megadott mód szerint:
                .Select(g =>
                {
                    // Egy csoportból összeszedjük az RSSI értékeket egy tömbbe
                    double[] values = g.Select(m => m.Rssi).ToArray();

                    // Az aggregálási mód alapján kiszámoljuk az RSSI értéket:
                    double rssi = mode switch
                    {
                        // Ha az aggregálási mód az átlag, akkor kiszámoljuk az RSSI értékek átlagát
                        SampleAggregation.Mean => values.Average(),
                        // Ha az aggregálási mód a medián, akkor kiszámoljuk az RSSI értékek mediánját
                        SampleAggregation.Median => Median(values),
                        // Akármi más esetben dobunk egy kivételt, mivel az nem megengedett aggregálási mód (vagy csak nem lett implementálva)
                        _ => throw new InvalidOperationException($"Nem megengedett aggregálási mód: {mode}")
                    };
                    return new Measurement(g.Key.ApId, g.Key.PointId, rssi, g.First().TrueDistance);
                })
                .ToList();
        }

        /// <summary>
        /// Kiszámolja a megadott értékek mediánját.
        /// </summary>
        /// 
        /// <param name="values">
        /// A számok tömbje, amelyből a mediánt kell kiszámolni.
        /// </param>
        /// 
        /// <returns>
        /// A megadott számok mediánja. Ha a tömb hossza páratlan, akkor a középső értéket adja vissza; ha páros, akkor a két középső érték átlagát adja vissza.
        /// </returns>
        private static double Median(double[] values)
        {
            // Másolatot készítünk a tömbről, hogy véletlen se módosítsuk az eredeti tömböt
            double[] copy = (double[])values.Clone();

            // Rendezzük a másolatot növekvő sorrendbe
            Array.Sort(copy);

            // Meghatározzuk a középső indexet
            int mid = copy.Length / 2;

            // Ha a tömb hossza páratlan, akkor a középső értéket adjuk vissza
            // Ha a tömb hossza páros, akkor a két középső érték átlagát adjuk vissza
            if (copy.Length % 2 == 1)
            {
                return copy[mid];
            }
            else
            {
                return (copy[mid - 1] + copy[mid]) / 2.0;
            }
        }
    }

}