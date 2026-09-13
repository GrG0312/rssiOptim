using RssiCalibration.Core.Models;

namespace RssiCalibration.Core.Grouping
{
    /// <summary>
    /// Csoportosítási stratégia, amely a gyártó és a frekvenciasáv kombinációját használja kulcsként.
    /// </summary>
    public sealed class VendorBandStrategy : IGroupingStrategy
    {
        /// <inheritdoc />
        public string Name => "vendor-band";

        /// <inheritdoc />
        public string Description => "Gyártó + frekvenciasáv kombinációnként külön n";

        /// <inheritdoc />
        public GroupKey GetKey(Measurement m) => new GroupKey($"{m.Vendor} @ {m.Band}");
    }
}
