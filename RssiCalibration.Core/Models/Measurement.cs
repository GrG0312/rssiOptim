namespace RssiCalibration.Core.Models;

/// <summary>
/// Az AP-khoz rendelt mérések adatait reprezentáló rekord.
/// A rekord tartalmazza az AP azonosítóját, a mérési pont azonosítóját, a mért RSSI értéket és a tényleges távolságot.
/// </summary>
public sealed record Measurement
{
    /// <summary>
    /// Az AP azonosítója, amelyhez a mérés tartozik.
    /// </summary>
    public readonly string ApId;

    /// <summary>
    /// A mérési pont azonosítója, ahol a mérés történt.
    /// </summary>
    public readonly string PointId;

    /// <summary>
    /// A mért RSSI érték.
    /// </summary>
    public readonly double Rssi;

    /// <summary>
    /// A tényleges távolság az AP-tól a mérési pontig.
    /// </summary>
    public readonly double TrueDistance;

    public Measurement(string apId, string pointId, double rssi, double trueDistance)
    {
        ApId = apId;
        PointId = pointId;
        Rssi = rssi;
        TrueDistance = trueDistance;
    }
}
