using RssiCalibration.Core.Models;

namespace RssiCalibration.Core.Grouping
{
    /// <summary>
    /// A csoportosítási stratégia, amely az összes AccessPoint-ot egyetlen csoportba sorolja, így minden AP ugyanazon n értéken osztozik.
    /// </summary>
    public sealed class GlobalStrategy : IGroupingStrategy
    {
        /// <inheritdoc />
        public string Name => "global";

        /// <inheritdoc />
        public string Description => "Egyetlen közös n minden AP-ra";

        /// <inheritdoc />
        public GroupKey GetKey(AccessPoint ap) => new GroupKey("ALL");
    }
}
