namespace RssiCalibration.Core.PathLoss
{

    /// <summary>
    /// Egy logaritmikus távolságmodell, amely az RSSI értékek és egy 'n' alapján becsüli meg a távolságot.
    /// </summary>
    /// 
    /// <remarks>
    /// Az alábbi képleten alapul:
    /// <code>
    /// d = 10 ^ ((RSSI0 - RSSI) / (10 * n))
    /// </code>
    /// </remarks>
    public sealed class LogDistanceModel : IPathLossModel
    {
        public string Name => "log-distance";

        public double EstimateDistance(double rssi, double rssi0, double n)
        {
            // Ellenőrizzük, hogy az n pozitív-e.
            if (n <= 0)
            {
                return double.PositiveInfinity;
            }

            // A 10 ^ ((RSSI0 - RSSI) / (10 * n)) képletének implementációja:
            double exponent = (rssi0 - rssi) / (10.0 * n);

            // Numerikus védelem: extrém RSSI/n kombinációnál a Pow túlcsordulna.
            if (exponent > 12) return 1e12;
            if (exponent < -12) return 0;

            return Math.Pow(10.0, exponent);
        }

        public double EstimateRssi(double distance, double rssi0, double n)
        {
            return rssi0 - 10.0 * n * Math.Log10(Math.Max(distance, 1e-9));
        }
    }
}