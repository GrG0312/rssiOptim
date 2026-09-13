namespace RssiCalibration.Core.Models
{

    /// <summary>
    /// Egy Wi-Fi eszközt (gyártó + frekvencia páros) reprezentáló rekord, azzal a referencia
    /// RSSI értékkel, amit ez az eszköz 1 méterről ad.
    /// </summary>
    /// <remarks>
    /// Ez a rekord <b>nem</b> egy konkrét fizikai hozzáférési pontot ír le: az eszközök
    /// szabadon cserélhetők az AP-helyek között, ezért nincs saját azonosítójuk. A kulcs a
    /// gyártó és a frekvencia párosa - ezen a néven hivatkoznak rájuk a mérési leolvasások
    /// (<c>readings.csv</c>).
    /// </remarks>
    public sealed record AccessPoint
    {
        /// <summary>
        /// Az eszköz gyártója.
        /// </summary>
        public readonly string Vendor;

        /// <summary>
        /// Az eszköz frekvenciája GHz-ben (pl. 2.4 vagy 5).
        /// </summary>
        public readonly double FrequencyGHz;

        /// <summary>
        /// Az eszköz referencia RSSI értéke, amely a kibocsátott jel erősségét jelzi 1 méteres távolságban.
        /// </summary>
        public readonly double Rssi0;

        /// <summary>
        /// Sáv megnevezése csoportosításhoz. Lásd: <see cref="Bands.Of(double)"/>.
        /// </summary>
        public string Band => Bands.Of(FrequencyGHz);

        /// <summary>
        /// Inicializálja az AccessPoint rekordot a megadott értékekkel.
        /// </summary>
        ///
        /// <param name="vendor">Az eszköz gyártójának neve.</param>
        /// <param name="frequencyGHz">Az eszköz frekvenciája GHz-ben.</param>
        /// <param name="rssi0">A referencia RSSI érték 1 méteres távolságban.</param>
        public AccessPoint(string vendor, double frequencyGHz, double rssi0)
        {
            Vendor = vendor;
            FrequencyGHz = frequencyGHz;
            Rssi0 = rssi0;
        }

        /// <summary>
        /// Az eszköz azonosítására szolgáló kulcs: a gyártó és a frekvencia párosa.
        /// </summary>
        public DeviceKey Key => new DeviceKey(Vendor, FrequencyGHz);
    }

    /// <summary>
    /// Egy eszköz (gyártó + frekvencia) azonosítására szolgáló kulcs. A gyártó nevét
    /// kis- és nagybetűtől függetlenül, a frekvenciát három tizedesjegyre kerekítve hasonlítja,
    /// hogy a két CSV fájlban lévő írásmódok (pl. <c>2,4</c> és <c>2.40</c>) ugyanarra a kulcsra essenek.
    /// </summary>
    public readonly record struct DeviceKey
    {
        /// <summary>
        /// A gyártó neve, ahogy a CSV fájlban szerepel.
        /// </summary>
        public string Vendor { get; }

        /// <summary>
        /// A frekvencia GHz-ben.
        /// </summary>
        public double FrequencyGHz { get; }

        public DeviceKey(string vendor, double frequencyGHz)
        {
            Vendor = vendor;
            FrequencyGHz = frequencyGHz;
        }

        /// <summary>
        /// Az összehasonlításhoz használt normalizált alak.
        /// </summary>
        private (string, double) Normalized =>
            (Vendor.Trim().ToLowerInvariant(), Math.Round(FrequencyGHz, 3));

        public bool Equals(DeviceKey other) => Normalized == other.Normalized;

        public override int GetHashCode() => Normalized.GetHashCode();

        public override string ToString() => $"{Vendor} @ {FrequencyGHz:0.###} GHz";
    }

    /// <summary>
    /// A frekvenciasáv megnevezését adja meg a frekvenciából.
    /// </summary>
    public static class Bands
    {
        /// <summary>
        /// A 3 GHz alatti frekvenciákat 2.4 GHz-es, a felettieket 5 GHz-es sávba sorolja.
        /// Jelenleg csak ez a két sáv releváns a Wi-Fi-ben használt frekvenciatartományokhoz.
        /// </summary>
        ///
        /// <param name="frequencyGHz">A frekvencia GHz-ben.</param>
        ///
        /// <returns>A sáv megnevezése: <c>2.4GHz</c> vagy <c>5GHz</c>.</returns>
        public static string Of(double frequencyGHz) => frequencyGHz < 3.0 ? "2.4GHz" : "5GHz";
    }
}
