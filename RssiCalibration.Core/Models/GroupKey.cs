namespace RssiCalibration.Core.Models
{
    /// <summary>
    /// Egy kalibrációs csoport azonosítója - ehhez tartozik egy közös "n" érték.
    /// </summary>
    public readonly record struct GroupKey
    {
        /// <summary>
        /// A csoport szöveges azonosítója (pl. gyártó neve, sáv, AP azonosító).
        /// </summary>
        public readonly string Value;

        /// <summary>
        /// Inicializálja a csoportkulcsot a megadott szöveges azonosítóval.
        /// </summary>
        ///
        /// <param name="value">A csoport szöveges azonosítója.</param>
        public GroupKey(string value)
        {
            Value = value;
        }

        /// <inheritdoc />
        public override string ToString() => Value;
    }
}
