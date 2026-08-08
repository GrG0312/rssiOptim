using RssiCalibration.Core.Optimization;

namespace RssiCalibration.Core.Services
{
    /// <summary>
    /// A kalibrációs algoritmus paraméterei.
    /// </summary>
    public sealed record CalibrationOptions
    {
        /// <summary>
        /// Az "n" keresési tartománya.
        /// </summary>
        public Interval NBounds { get; init; } = Interval.Of(1.0, 6.0);

        /// <summary>
        /// Ha true, az RSSI0-t is finomhangolja csoportonként egy eltolással.
        /// Gyakran ez okozza a nagy kiugró hibákat, ha a referenciamérés pontatlan volt.
        /// </summary>
        public bool OptimizeRssi0Offset { get; init; }

        /// <summary>
        /// Az RSSI0 eltolás keresési tartománya dBm-ben (dBm = decibel-milliwatt, a rádiófrekvenciás teljesítmény mértékegysége).
        /// Csak akkor érvényes, ha OptimizeRssi0Offset = true.
        /// </summary>
        public Interval Rssi0OffsetBounds { get; init; } = Interval.Of(-10, 10);

        /// <summary>
        /// Az RSSI0 eltolás rácsfelbontása (csak ha OptimizeRssi0Offset = true).
        /// </summary>
        public int Rssi0OffsetSteps { get; init; } = 81;
    }
}