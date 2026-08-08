namespace RssiCalibration.Core.Objectives
{
    /// <summary>
    /// Skalár célfüggvény a hibavektor felett. A kalibráció ezt minimalizálja.
    /// </summary>
    public interface IErrorObjective
    {
        /// <summary>
        /// A célfüggvény neve, amelyet a kalibrációs folyamat során a logban és a fájlnevekben használnak.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// A célfüggvény kiértékelése a bemeneti hibák alapján.
        /// </summary>
        /// 
        /// <param name="signedErrors">
        /// A bemeneti hibák előjeles vektora (becsült - valós) méterben.
        /// </param>
        /// 
        /// <returns>
        /// A célfüggvény értéke, amelyet a kalibráció minimalizálni próbál.
        /// </returns>
        public double Evaluate(double[] signedErrors);
    }
}