namespace RssiCalibration.Core.PathLoss
{
    /// <summary>
    /// Az útveszteség (path loss) modellek interfésze.
    /// A különböző modellek implementálják ezt az interfészt, hogy becsléseket adjanak a távolságra és az RSSI-re.
    /// </summary>
    public interface IPathLossModel
    {
        /// <summary>
        /// A modell neve, amelyet a felhasználó láthat (pl. "Log-térbeli modell", "Free-space path loss", stb.).
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Becsüli a távolságot az adott RSSI érték alapján, a referencia RSSI (rssi0) és a környezeti tényező (n) figyelembevételével.
        /// </summary>
        /// 
        /// <param name="rssi">
        /// Az aktuális RSSI érték, amelyből a távolságot szeretnénk becsülni.
        /// </param>
        /// 
        /// <param name="rssi0">
        /// A referencia RSSI érték, amelyet a modell használ a távolság becsléséhez.
        /// </param>
        /// 
        /// <param name="n">
        /// A környezeti tényező, amely a jel terjedésének csillapítását jellemzi.
        /// </param>
        /// 
        /// <returns>
        /// A becsült távolság az adott RSSI érték alapján.
        /// </returns>
        public double EstimateDistance(double rssi, double rssi0, double n);

        /// <summary>
        /// Becsüli az RSSI értéket az adott távolság alapján, a referencia RSSI (rssi0) és a környezeti tényező (n) figyelembevételével.
        /// </summary>
        /// 
        /// <param name="distance">
        /// A távolság, amelyből az RSSI értéket szeretnénk becsülni.
        /// </param>
        /// 
        /// <param name="rssi0">
        /// A referencia RSSI érték, amelyet a modell használ az RSSI becsléséhez.
        /// </param>
        /// 
        /// <param name="n">
        /// A környezeti tényező, amely a jel terjedésének csillapítását jellemzi.
        /// </param>
        /// 
        /// <returns>
        /// A becsült RSSI érték az adott távolság alapján.
        /// </returns>
        public double EstimateRssi(double distance, double rssi0, double n);
    }
}