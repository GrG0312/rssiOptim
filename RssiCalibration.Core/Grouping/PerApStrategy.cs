using RssiCalibration.Core.Models;

namespace RssiCalibration.Core.Grouping
{
    /// <summary>
    /// Csoportosítási stratégia, amely minden hozzáférési ponthoz (AP) külön csoportot hoz létre.
    /// </summary>
    public sealed class PerApStrategy : IGroupingStrategy
    {
        /// <inheritdoc />
        public string Name => "per-ap";

        /// <inheritdoc />
        public string Description => "AP-nként külön n";

        /// <inheritdoc />
        public GroupKey GetKey(AccessPoint ap) => new GroupKey(ap.Id);
    }
}
