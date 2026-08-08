namespace RssiCalibration.Core.Models
{

    /// <summary>
    /// Egy minta kiértékelése a megtalált n mellett.
    /// </summary>
    public sealed record ResidualRow
    {
        public readonly string ApId; 
        public readonly string PointId;
        public readonly double Rssi;
        public readonly double TrueDistance;
        public readonly double EstimatedDistance;
        public double Error => EstimatedDistance - TrueDistance;

        public ResidualRow(string apId, string pointId, double rssi, double trueDistance, double estimatedDistance)
        {
            ApId = apId;
            PointId = pointId;
            Rssi = rssi;
            TrueDistance = trueDistance;
            EstimatedDistance = estimatedDistance;
        }
    }

    /// <summary>
    /// A kalibrációs folyamat eredménye.
    /// </summary>
    public sealed record CalibrationResult
    {
        /// <summary>
        /// A csoport kulcsa, amelyhez a kalibrációs eredmény tartozik.
        /// </summary>
        public readonly GroupKey Group;

        /// <summary>
        /// A kalibráció során használt stratégia neve.
        /// </summary>
        public readonly string StrategyName;

        /// <summary>
        /// Az optimális n érték.
        /// </summary>
        public readonly double OptimalN;

        /// <summary>
        /// Az Rssi0 eltolás értéke.
        /// </summary>
        public readonly double Rssi0Offset;

        /// <summary>
        /// Az objektív érték, amely a kalibrációs folyamat célfüggvényének értékét jelzi.
        /// </summary>
        public readonly double ObjectiveValue;

        /// <summary>
        /// A hibastatisztikák, amelyek a kalibrációs folyamat során keletkezett hibák összegzését tartalmazzák.
        /// </summary>
        public readonly ErrorStatistics Statistics;

        /// <summary>
        /// A maradék sorok listája, amelyek a kalibrációs folyamat során keletkezett hibákat tartalmazzák.
        /// </summary>
        public readonly IReadOnlyList<ResidualRow> Residuals;

        /// <summary>
        /// Az AP azonosítók listája, amelyek a kalibrációs folyamat során használt hozzáférési pontokat jelzik.
        /// </summary>
        public readonly IReadOnlyList<string> ApIds;

        public CalibrationResult(
            GroupKey group,
            string strategyName,
            double optimalN,
            double rssi0Offset,
            double objectiveValue,
            ErrorStatistics statistics,
            IReadOnlyList<ResidualRow> residuals,
            IReadOnlyList<string> apIds)
        {
            Group = group;
            StrategyName = strategyName;
            OptimalN = optimalN;
            Rssi0Offset = rssi0Offset;
            ObjectiveValue = objectiveValue;
            Statistics = statistics;
            Residuals = residuals;
            ApIds = apIds;
        }
    }
}