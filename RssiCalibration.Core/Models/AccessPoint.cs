namespace RssiCalibration.Core.Models
{

    /// <summary>
    /// A Wi-Fi hozzáférési pontot (AP) reprezentáló rekord. A rekord tartalmazza az AP azonosítóját, gyártóját, frekvenciáját és a referencia RSSI értékét.
    /// </summary>
    public sealed record AccessPoint
    {
        /// <summary>
        /// Az AP azonosítója, amely egyedi módon azonosítja a hozzáférési pontot.
        /// </summary>
        public readonly string Id;

        /// <summary>
        /// Az AP gyártója, amely a hozzáférési pontot gyártó cég nevét tartalmazza.
        /// </summary>
        public readonly string Vendor;

        /// <summary>
        /// Az AP frekvenciája MHz-ben, amely a hozzáférési pont által használt rádiófrekvenciát jelzi.
        /// </summary>
        public readonly int FrequencyMHz;

        /// <summary>
        /// Az AP referencia RSSI értéke, amely a hozzáférési pont által kibocsátott jel erősségét jelzi 1 méteres távolságban.
        /// </summary>
        public readonly double Rssi0;

        /// <summary>
        /// Sáv megnevezése csoportosításhoz. A 3 GHz alatti frekvenciákat 2.4 GHz-es,
        /// a felettieket 5 GHz-es sávba sorolja. Jelenleg csak ez a két sáv releváns
        /// a Wi-Fi-ben használt frekvenciatartományokhoz.
        /// </summary>
        public string Band => FrequencyMHz < 3000 ? "2.4GHz" : "5GHz";

        /// <summary>
        /// Inicializálja az AccessPoint rekordot a megadott értékekkel.
        /// </summary>
        ///
        /// <param name="id">Az AP egyedi azonosítója.</param>
        /// <param name="vendor">Az AP gyártójának neve.</param>
        /// <param name="frequencyMHz">Az AP frekvenciája MHz-ben.</param>
        /// <param name="rssi0">A referencia RSSI érték 1 méteres távolságban.</param>
        public AccessPoint(string id, string vendor, int frequencyMHz, double rssi0)
        {
            Id = id;
            Vendor = vendor;
            FrequencyMHz = frequencyMHz;
            Rssi0 = rssi0;
        }
    }
}
