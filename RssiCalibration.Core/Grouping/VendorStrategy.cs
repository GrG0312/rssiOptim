using RssiCalibration.Core.Models;

namespace RssiCalibration.Core.Grouping
{
    /// <summary>
    /// Csoportosítási stratégia, amely az AccessPoint objektumok gyártója (Vendor) alapján csoportosítja őket.
    /// </summary>
    public sealed class VendorStrategy : IGroupingStrategy
    {
        /// <inheritdoc />
        public string Name => "vendor";

        /// <inheritdoc />
        public string Description => "Gyártónként külön n";

        /// <inheritdoc />
        public GroupKey GetKey(AccessPoint ap) => new GroupKey(ap.Vendor);
    }
}
