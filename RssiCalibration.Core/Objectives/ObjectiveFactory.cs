using RssiCalibration.Core.Objectives.Basics;

namespace RssiCalibration.Core.Objectives
{
    /// <summary>
    /// A célfüggvények gyára, amely a nevet leképezi a megfelelő hibafüggvény implementációjára.
    /// </summary>
    public static class ObjectiveFactory
    {
        /// <summary>
        /// Létrehoz egy hibafüggvény objektumot a megadott név alapján.
        /// </summary>
        /// 
        /// <param name="name">
        /// A célfüggvény neve.
        /// </param>
        /// 
        /// <returns>
        /// A megfelelő hibafüggvény objektum.
        /// </returns>
        /// 
        /// <exception cref="ArgumentException"></exception>
        public static IErrorObjective Create(string name) => name.ToLowerInvariant() switch
        {
            "mean" or "mae" => new MeanAbsoluteError(),
            "median" or "mdae" => new MedianAbsoluteError(),
            "rmse" => new RootMeanSquareError(),
            "huber" => new HuberLoss(),
            "composite" or "robust" => new RobustComposite(),
            _ => throw new ArgumentException(
                $"Ismeretlen célfüggvény: '{name}'. Elérhető: mean, median, rmse, huber, composite.")
        };

        /// <summary>
        /// A gyár által elérhető célfüggvények neveinek listája.
        /// </summary>
        public static IReadOnlyList<string> AvailableNames { get; } =
            ["mean", "median", "rmse", "huber", "composite"];
    }
}