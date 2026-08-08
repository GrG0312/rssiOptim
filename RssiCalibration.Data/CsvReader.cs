using System.Globalization;

namespace RssiCalibration.Data
{

    /// <summary>
    /// Egyszerű CSV olvasó, amely a fájl első sorát fejlécnek tekinti, és minden további sort egy <see cref="Dictionary{,}"/> objektummá alakít.
    /// A további publikus metódusok segítenek a típuskonverzióban és a kötelező mezők ellenőrzésében.
    /// </summary>
    internal static class CsvReader
    {
        /// <summary>
        /// Beolvassa a CSV fájlt, és minden sort egy <see cref="Dictionary{,}"/> objektummá alakít, ahol a kulcsok a fejléc oszlopnevei.
        /// </summary>
        /// 
        /// <param name="path">
        /// A beolvasandó CSV fájl elérési útja. A fájlnak léteznie kell, és a fejlécnek tartalmaznia kell az oszlopneveket.
        /// </param>
        /// 
        /// <param name="separator">
        /// A mezőket elválasztó karakter. Alapértelmezett érték: ';'.
        /// </param>
        /// 
        /// <returns>
        /// Egy <see cref="IEnumerable{T}"/> típusú objektum, amely minden sorhoz egy <see cref="Dictionary{,}"/> objektumot tartalmaz, ahol a kulcsok a fejléc oszlopnevei.
        /// </returns>
        /// 
        /// <exception cref="FileNotFoundException"></exception>
        /// <exception cref="InvalidDataException"></exception>
        public static IEnumerable<Dictionary<string, string>> Read(string path, char separator = ';')
        {
            // Verify if the file exists
            if (!File.Exists(path))
            {
                throw new FileNotFoundException($"Nem található a fájl: {path}", path);
            }

            // Read the file line by line, skipping empty lines and comments
            string[]? header = null;
            int lineNumber = 0;

            foreach (string raw in File.ReadLines(path))
            {
                lineNumber++;
                string line = raw.Trim();

                if (line.Length == 0 || line.StartsWith('#'))
                {
                    continue;
                }

                // Split the line into fields, trimming each of them
                string[] fields = line.Split(separator).Select(f => f.Trim()).ToArray();

                // If this is the first non-empty, non-comment line, treat it as the header
                if (header is null)
                {
                    header = fields.Select(f => f.ToLowerInvariant()).ToArray();
                    continue;
                }

                // Check if the number of fields matches the number of header columns
                // E.g.: header has 3 columns, but the current line has 4 fields
                if (fields.Length != header.Length)
                {
                    throw new InvalidDataException($"{Path.GetFileName(path)}:{lineNumber} - {header.Length} oszlop várt, {fields.Length} érkezett.");
                }


                Dictionary<string, string> row = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                for (int i = 0; i < header.Length; i++)
                {
                    row[header[i]] = fields[i];
                }

                // Return the rows sequentially as they are read, without loading the entire file into memory
                yield return row;
            }

            if (header is null)
            {
                throw new InvalidDataException($"Üres vagy fejléc nélküli fájl: {path}");
            }
        }

        #region Konverziók

        /// <summary>
        /// Konvertálja a megadott oszlop értékét <see cref="double"/> típusra. Ha az oszlop hiányzik vagy az érték nem konvertálható, kivételt dob.
        /// </summary>
        /// 
        /// <param name="row">
        /// A beolvasott sor, amely egy <see cref="Dictionary{,}"/> objektum, ahol a kulcsok a fejléc oszlopnevei.
        /// </param>
        /// 
        /// <param name="column">
        /// A konvertálandó oszlop neve. A keresés nem érzékeny a kis- és nagybetűkre.
        /// </param>
        /// 
        /// <param name="path">
        /// A CSV fájl elérési útja, amelyből a sor származik.
        /// </param>
        /// 
        /// <returns>
        /// A megadott oszlop értéke <see cref="double"/> típusra konvertálva.
        /// </returns>
        /// 
        /// <exception cref="InvalidDataException"></exception>
        public static double Double(Dictionary<string, string> row, string column, string path)
        {
            string text = Required(row, column, path);

            // Tizedes vessző szűrése ponttá, hogy a double.TryParse helyesen értelmezze
            text = text.Replace(',', '.');

            if (double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out double value))
            {
                return value;
            }

            throw new InvalidDataException($"{Path.GetFileName(path)}: '{column}' nem szám: '{text}'");
        }

        /// <summary>
        /// Konvertálja a megadott oszlop értékét <see cref="int"/> típusra. Ha az oszlop hiányzik vagy az érték nem konvertálható, kivételt dob.
        /// </summary>
        /// 
        /// <param name="row">
        /// A beolvasott sor, amely egy <see cref="Dictionary{,}"/> objektum, ahol a kulcsok a fejléc oszlopnevei.
        /// </param>
        /// 
        /// <param name="column">
        /// A konvertálandó oszlop neve. A keresés nem érzékeny a kis- és nagybetűkre.
        /// </param>
        /// 
        /// <param name="path">
        /// A CSV fájl elérési útja, amelyből a sor származik.
        /// </param>
        /// 
        /// <returns>
        /// A megadott oszlop értéke <see cref="int"/> típusra konvertálva.
        /// </returns>
        /// 
        /// <exception cref="InvalidDataException"></exception>
        public static int Int(Dictionary<string, string> row, string column, string path)
        {
            string text = Required(row, column, path);

            if (int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out int value))
            {
                return value;
            }

            throw new InvalidDataException($"{Path.GetFileName(path)}: '{column}' nem egész szám: '{text}'");
        }

        #endregion

        /// <summary>
        /// Ellenőrzi, hogy a megadott oszlop létezik-e a sorban, és nem üres-e az értéke. Ha hiányzik vagy üres, kivételt dob.
        /// </summary>
        /// 
        /// <param name="row">
        /// A beolvasott sor, amely egy <see cref="Dictionary{,}"/> objektum, ahol a kulcsok a fejléc oszlopnevei.
        /// </param>
        /// 
        /// <param name="column">
        /// A kötelezően ellenőrizendő oszlop neve. A keresés nem érzékeny a kis- és nagybetűkre.
        /// </param>
        /// 
        /// <param name="path">
        /// A CSV fájl elérési útja, amelyből a sor származik.
        /// </param>
        /// 
        /// <returns>
        /// A megadott oszlop értéke, ha az létezik és nem üres.
        /// </returns>
        /// 
        /// <exception cref="InvalidDataException"></exception>
        public static string Required(Dictionary<string, string> row, string column, string path)
        {
            if (!row.TryGetValue(column, out string? value) || value.Length == 0)
            {
                throw new InvalidDataException($"{Path.GetFileName(path)}: hiányzó '{column}' oszlop vagy üres érték.");
            }
            return value;
        }
    }
}