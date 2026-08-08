namespace RssiCalibration.Data
{
    /// <summary>
    /// A minták aggregálásának módja. Alapértelmezésben nincs aggregálás.
    /// </summary>
    public enum SampleAggregation
    {
        /// <summary>
        /// Minden sor önálló minta.
        /// </summary>
        None,

        /// <summary>
        /// (AP, pont) páronként átlagos RSSI.
        /// </summary>
        Mean,

        /// <summary>
        /// (AP, pont) páronként medián RSSI.
        /// </summary>
        Median
    }
}
