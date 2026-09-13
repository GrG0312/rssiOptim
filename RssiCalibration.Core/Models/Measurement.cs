namespace RssiCalibration.Core.Models;

/// <summary>
/// Egy mérési leolvasás: egy adott AP-helyen lévő, adott gyártó + frekvencia párosú eszköz
/// jelerőssége egy adott mérésiponton, a hozzá tartozó valós távolsággal.
/// </summary>
/// <remarks>
/// A rekord két bemeneti táblázat összefűzéséből áll elő: a mérésipontok és az AP-helyek
/// közötti távolságokból (<c>measurements.csv</c>) és az eszközönkénti RSSI leolvasásokból
/// (<c>readings.csv</c>). Ugyanahhoz az (AP, mérésipont) párhoz annyi rekord tartozik,
/// ahány eszközzel ott mértek.
/// </remarks>
public sealed record Measurement
{
    /// <summary>
    /// Az AP-hely azonosítója, ahol a mérés történt.
    /// </summary>
    public readonly string ApId;

    /// <summary>
    /// A mérési pont azonosítója, ahol a mérés történt.
    /// </summary>
    public readonly string PointId;

    /// <summary>
    /// Az AP-helyre bedugott eszköz gyártója.
    /// </summary>
    public readonly string Vendor;

    /// <summary>
    /// Az AP-helyre bedugott eszköz frekvenciája GHz-ben.
    /// </summary>
    public readonly double FrequencyGHz;

    /// <summary>
    /// A mért RSSI érték.
    /// </summary>
    public readonly double Rssi;

    /// <summary>
    /// A tényleges távolság az AP-helytől a mérési pontig.
    /// </summary>
    public readonly double TrueDistance;

    /// <summary>
    /// Az eszközt azonosító gyártó + frekvencia páros. Ezzel kereshető ki a referencia RSSI0.
    /// </summary>
    public DeviceKey Device => new DeviceKey(Vendor, FrequencyGHz);

    /// <summary>
    /// A mérésnél használt eszköz frekvenciasávja. Lásd: <see cref="Bands.Of(double)"/>.
    /// </summary>
    public string Band => Bands.Of(FrequencyGHz);

    public Measurement(
        string apId,
        string pointId,
        string vendor,
        double frequencyGHz,
        double rssi,
        double trueDistance)
    {
        ApId = apId;
        PointId = pointId;
        Vendor = vendor;
        FrequencyGHz = frequencyGHz;
        Rssi = rssi;
        TrueDistance = trueDistance;
    }
}
