using RssiCalibration.Core.Models;

namespace RssiCalibration.Core.Grouping
{
    /// <summary>
    /// A csoportosítási stratégia, amely az AccessPoint-okat frekvenciasávonként külön n értéken tartja.
    /// </summary>
    public sealed class BandStrategy : IGroupingStrategy
    {
        /// <inheritdoc />
        public string Name => "band";

        /// <inheritdoc />
        public string Description => "Frekvenciasávonként külön n";

        /// <inheritdoc />
        public GroupKey GetKey(AccessPoint ap) => new GroupKey(ap.Band);
    }
}
