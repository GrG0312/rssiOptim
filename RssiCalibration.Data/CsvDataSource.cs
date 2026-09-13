using RssiCalibration.Core.Models;

namespace RssiCalibration.Data
{

    /// <summary>
    /// CSV fájlokból betöltő adatforrás. A használt CSV fájlok a(z) <c>access-points.csv</c>,
    /// a <c>measurements.csv</c> és a <c>readings.csv</c>.
    /// </summary>
    /// <remarks>
    /// Az adat három táblázatra van bontva, hogy kézzel is kényelmesen tölthető legyen:
    /// <list type="bullet">
    /// <item><description><c>access-points.csv</c> - eszközkatalógus: gyártó + frekvencia páronként az RSSI0.</description></item>
    /// <item><description><c>measurements.csv</c> - geometria: AP-helyenként és mérésipontonként a valós távolság.</description></item>
    /// <item><description><c>readings.csv</c> - leolvasások: melyik AP-helyen, melyik ponton, melyik eszközzel mekkora RSSI jött ki.</description></item>
    /// </list>
    /// A <see cref="Load"/> ezt a hármat fűzi össze: a leolvasás az (AP, pont) párral kapja meg
    /// a valós távolságot, a gyártó + frekvencia párossal pedig a referencia RSSI0-t.
    /// </remarks>
    public sealed class CsvDataSource
    {
        /// <summary>
        /// A CSV fájl elérési útja, amely az eszközkatalógust tartalmazza.
        /// </summary>
        private readonly string accessPointsPath;

        /// <summary>
        /// A CSV fájl elérési útja, amely az AP-hely + mérésipont távolságokat tartalmazza.
        /// </summary>
        private readonly string measurementsPath;

        /// <summary>
        /// A CSV fájl elérési útja, amely az eszközönkénti RSSI leolvasásokat tartalmazza.
        /// </summary>
        private readonly string readingsPath;

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
        /// A CSV fájl elérési útja, amely az eszközkatalógust tartalmazza.
        /// A fájlnak a következő oszlopokat kell tartalmaznia: <c>vendor</c>, <c>frequencyghz</c>, <c>rssi0</c>.
        /// </param>
        ///
        /// <param name="measurementsPath">
        /// A CSV fájl elérési útja, amely az AP-hely + mérésipont távolságokat tartalmazza.
        /// A fájlnak a következő oszlopokat kell tartalmaznia: <c>apid</c>, <c>pointid</c>, <c>truedistance</c>.
        /// </param>
        ///
        /// <param name="readingsPath">
        /// A CSV fájl elérési útja, amely az RSSI leolvasásokat tartalmazza.
        /// A fájlnak a következő oszlopokat kell tartalmaznia: <c>apid</c>, <c>pointid</c>,
        /// <c>vendor</c>, <c>frequencyghz</c>, <c>rssi</c>.
        /// </param>
        ///
        /// <param name="aggregation">
        /// A minták aggregálásának módja. Alapértelmezésben nincs aggregálás.
        /// </param>
        ///
        /// <param name="separator">
        /// A mezőket elválasztó karakter. Alapértelmezésben pontosvessző (<c>;</c>).
        /// </param>
        public CsvDataSource(
            string accessPointsPath,
            string measurementsPath,
            string readingsPath,
            SampleAggregation aggregation = SampleAggregation.None,
            char separator = ';')
        {
            this.accessPointsPath = accessPointsPath;
            this.measurementsPath = measurementsPath;
            this.readingsPath = readingsPath;
            this.aggregation = aggregation;
            this.separator = separator;
        }

        public CalibrationDataset Load()
        {
            List<AccessPoint> aps = LoadDevices();
            Dictionary<(string ApId, string PointId), double> distances = LoadDistances();
            List<Measurement> measurements = LoadReadings(distances);

            // Ha van kért aggregálás, végezzük el az aggregálást a méréseken
            if (aggregation != SampleAggregation.None)
            {
                measurements = Aggregate(measurements, aggregation);
            }

            // Visszaadjuk a létrehozott CalibrationDataset objektumot az eszközök és mérések listájával
            return new CalibrationDataset(aps, measurements);
        }

        /// <summary>
        /// Beolvassa az eszközkatalógust: gyártó + frekvencia páronként a referencia RSSI0 értéket.
        /// </summary>
        ///
        /// <returns>Az eszközök listája.</returns>
        ///
        /// <exception cref="InvalidDataException">
        /// Ha a fájl üres, vagy ugyanaz a gyártó + frekvencia páros többször szerepel benne.
        /// </exception>
        private List<AccessPoint> LoadDevices()
        {
            List<AccessPoint> aps =
                // Beolvassuk a CSV fájlból az értékeket
                CsvReader.Read(accessPointsPath, separator)
                // Minden sorból létrehozunk egy AccessPoint objektumot
                .Select(r => new AccessPoint(
                    vendor: CsvReader.Required(r, "vendor", accessPointsPath),
                    frequencyGHz: CsvReader.Double(r, "frequencyghz", accessPointsPath),
                    rssi0: CsvReader.Double(r, "rssi0", accessPointsPath)))
                // Az összes AccessPoint objektumot listává alakítjuk
                .ToList();

            if (aps.Count == 0)
            {
                throw new InvalidDataException("Nem tartalmaz eszközt az access-points fájl.");
            }

            string[] duplicates =
                // Csoportosítsuk az eszközöket a gyártó + frekvencia páros alapján
                aps.GroupBy(a => a.Key)
                // Szűrjük ki azokat a csoportokat, amelyek több mint egy elemet tartalmaznak (ismétlődő eszközök)
                .Where(g => g.Count() > 1)
                // Válasszuk ki az ismétlődő párosokat
                .Select(g => g.Key.ToString())
                .ToArray();

            // Ha vannak ismétlődő eszközök, dobjunk kivételt
            if (duplicates.Length > 0)
            {
                throw new InvalidDataException($"Ismétlődő gyártó + frekvencia páros: {string.Join(", ", duplicates)}");
            }

            return aps;
        }

        /// <summary>
        /// Beolvassa az (AP-hely, mérésipont) párokhoz tartozó valós távolságokat.
        /// </summary>
        ///
        /// <returns>
        /// Szótár, amely az (AP azonosító, pont azonosító) párhoz a valós távolságot rendeli.
        /// </returns>
        ///
        /// <exception cref="InvalidDataException">
        /// Ha a fájl üres, vagy ugyanaz az (AP, pont) páros többször szerepel benne.
        /// </exception>
        private Dictionary<(string, string), double> LoadDistances()
        {
            Dictionary<(string, string), double> distances =
                new Dictionary<(string, string), double>(KeyComparer.Instance);

            foreach (Dictionary<string, string> row in CsvReader.Read(measurementsPath, separator))
            {
                string apId = CsvReader.Required(row, "apid", measurementsPath);
                string pointId = CsvReader.Required(row, "pointid", measurementsPath);
                double distance = CsvReader.Double(row, "truedistance", measurementsPath);

                // Ugyanahhoz az (AP, pont) párhoz csak egy távolság tartozhat: az ismétlődés
                // szinte biztosan elgépelés, és csendben felülírva észrevétlen maradna.
                if (!distances.TryAdd((apId, pointId), distance))
                {
                    throw new InvalidDataException(
                        $"Ismétlődő (AP, pont) páros a measurements fájlban: {apId} / {pointId}");
                }
            }

            if (distances.Count == 0)
            {
                throw new InvalidDataException("Nem tartalmaz távolságot a measurements fájl.");
            }

            return distances;
        }

        /// <summary>
        /// Beolvassa az RSSI leolvasásokat, és mindegyikhez hozzáfűzi a valós távolságot.
        /// </summary>
        ///
        /// <param name="distances">
        /// Az (AP, pont) páronkénti valós távolságok, a <see cref="LoadDistances"/> eredménye.
        /// </param>
        ///
        /// <returns>A kész mérések listája.</returns>
        ///
        /// <exception cref="InvalidDataException">
        /// Ha egy leolvasás olyan (AP, pont) párosra hivatkozik, amelyhez nincs távolság,
        /// vagy ha egyetlen kitöltött leolvasás sincs.
        /// </exception>
        private List<Measurement> LoadReadings(Dictionary<(string, string), double> distances)
        {
            List<Measurement> measurements = new List<Measurement>();

            foreach (Dictionary<string, string> row in CsvReader.Read(readingsPath, separator))
            {
                // Az RSSI nélküli sorok a még ki nem töltött méréseket jelölik: a fájl előre
                // felveheti az összes (AP, pont, eszköz) kombinációt, és a kitöltés haladhat
                // ütemében - az üres sorokat egyszerűen átugorjuk.
                if (!row.TryGetValue("rssi", out string? rssiText) || rssiText.Length == 0)
                {
                    continue;
                }

                string apId = CsvReader.Required(row, "apid", readingsPath);
                string pointId = CsvReader.Required(row, "pointid", readingsPath);

                if (!distances.TryGetValue((apId, pointId), out double trueDistance))
                {
                    throw new InvalidDataException(
                        $"A readings fájlban olyan (AP, pont) páros szerepel, amelyhez nincs távolság " +
                        $"a measurements fájlban: {apId} / {pointId}");
                }

                measurements.Add(new Measurement(
                    apId: apId,
                    pointId: pointId,
                    vendor: CsvReader.Required(row, "vendor", readingsPath),
                    frequencyGHz: CsvReader.Double(row, "frequencyghz", readingsPath),
                    rssi: CsvReader.Double(row, "rssi", readingsPath),
                    trueDistance: trueDistance));
            }

            // Ha nincs mérés, dobjunk kivételt
            if (measurements.Count == 0)
            {
                throw new InvalidDataException(
                    "Nem tartalmaz kitöltött RSSI értéket a readings fájl: a kalibrációhoz " +
                    "legalább egy sorban ki kell tölteni az Rssi oszlopot.");
            }

            return measurements;
        }

        /// <summary>
        /// Aggregálja a méréseket az AP azonosító, a pont azonosító és az eszköz alapján,
        /// a megadott aggregálási mód szerint.
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
        /// Egy új listát, amely az aggregált méréseket tartalmazza, az AP azonosító, a pont
        /// azonosító és az eszköz szerint csoportosítva.
        /// </returns>
        private static List<Measurement> Aggregate(List<Measurement> raw, SampleAggregation mode)
        {
            // Egy fail-safe ellenőrzés, hogy ha nincs aggregálás, akkor egyszerűen visszaadjuk a nyers méréseket
            if (mode == SampleAggregation.None)
            {
                return raw;
            }

            return raw
                // Csoportosítsuk a méréseket az AP azonosító, a pont azonosító és az eszköz alapján.
                // Egy csoportba azok a sorok kerülnek, amelyek ugyanazt a mérést ismétlik:
                // ugyanaz az eszköz, ugyanabban az AP-helyben, ugyanarról a mérésipontról nézve.
                .GroupBy(m => (m.ApId, m.PointId, m.Device))
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

                    Measurement first = g.First();
                    return new Measurement(
                        first.ApId, first.PointId, first.Vendor, first.FrequencyGHz, rssi, first.TrueDistance);
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

        /// <summary>
        /// Az (AP azonosító, pont azonosító) párokat kis- és nagybetűtől függetlenül hasonlítja,
        /// hogy a két fájlban lévő eltérő írásmód (pl. <c>ap1</c> és <c>AP1</c>) is összetalálkozzon.
        /// </summary>
        private sealed class KeyComparer : IEqualityComparer<(string, string)>
        {
            public static readonly KeyComparer Instance = new KeyComparer();

            public bool Equals((string, string) x, (string, string) y) =>
                StringComparer.OrdinalIgnoreCase.Equals(x.Item1, y.Item1)
                && StringComparer.OrdinalIgnoreCase.Equals(x.Item2, y.Item2);

            public int GetHashCode((string, string) obj) =>
                HashCode.Combine(
                    StringComparer.OrdinalIgnoreCase.GetHashCode(obj.Item1),
                    StringComparer.OrdinalIgnoreCase.GetHashCode(obj.Item2));
        }
    }

}
