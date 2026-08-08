using RssiCalibration.Core.Grouping;
using RssiCalibration.Core.Models;
using RssiCalibration.Core.Objectives;
using RssiCalibration.Core.Optimization;
using RssiCalibration.Core.PathLoss;

namespace RssiCalibration.Core.Services
{
    /// <summary>
    /// A kalibrációs folyamatot végrehajtó osztály. A megadott mérések alapján meghatározza a legjobb 'n' és opcionálisan az RSSI0 eltolást.
    /// </summary>
    public sealed class CalibrationEngine
    {
        /// <summary>
        /// A kalibrációhoz használt útvesztés-modell. Ez határozza meg, hogyan becsüljük meg a távolságot az RSSI értékekből.
        /// </summary>
        private readonly IPathLossModel _model;

        /// <summary>
        /// Az optimalizáló, amely a célfüggvény minimumát keresi az 'n' és opcionálisan az RSSI0 eltolás tekintetében.
        /// </summary>
        private readonly IOptimizer1D _optimizer;

        public CalibrationEngine(IPathLossModel model, IOptimizer1D optimizer)
        {
            _model = model;
            _optimizer = optimizer;
        }

        /// <summary>
        /// Végrehajtja a kalibrációt a megadott adatok, csoportosítási stratégia és célfüggvény alapján.
        /// A méréseket a csoportosítási stratégia szerint csoportosítja, 
        /// majd minden csoporthoz meghatározza a legjobb 'n' és opcionálisan az RSSI0 eltolást.
        /// </summary>
        /// 
        /// <param name="data">
        /// A kalibrációhoz használt adatkészlet, amely tartalmazza az Access Pointokat és a méréseket.
        /// </param>
        /// 
        /// <param name="grouping">
        /// A csoportosítási stratégia, amely meghatározza, hogyan kell a méréseket csoportosítani az Access Pointok alapján.
        /// </param>
        /// 
        /// <param name="objective">
        /// A célfüggvény, amely meghatározza, hogyan értékeljük a becsült távolságok és a valós távolságok közötti hibát.
        /// </param>
        /// 
        /// <param name="options">
        /// Opcionális kalibrációs beállítások, például az 'n' és az RSSI0 eltolás optimalizálásának határai és lépésközei.
        /// </param>
        /// 
        /// <returns>
        /// Egy listát a kalibrációs eredményekről, ahol minden elem egy csoporthoz tartozó optimális 'n', RSSI0 eltolás, célfüggvény érték és statisztikák.
        /// </returns>
        public IReadOnlyList<CalibrationResult> Calibrate(
            CalibrationDataset data,
            IGroupingStrategy grouping,
            IErrorObjective objective,
            CalibrationOptions? options = null)
        {
            // Ha a felhasználó nem adott meg beállításokat, használjuk az alapértelmezett értékeket.
            options ??= new CalibrationOptions();

            return data.Measurements
                // Csoportosítjuk a méréseket az Access Pointok alapján a megadott csoportosítási stratégia szerint.
                .GroupBy(m => grouping.GetKey(data.ApOf(m)))
                // A csoportokat a kulcsuk (GroupKey) értéke szerint rendezzük, figyelmen kívül hagyva a kis- és nagybetűk közötti különbséget.
                .OrderBy(g => g.Key.Value, StringComparer.OrdinalIgnoreCase)
                // Minden csoporthoz meghívjuk a CalibrateGroup metódust,
                // amely kiszámítja az optimális 'n' és RSSI0 eltolást, valamint a célfüggvény értékét és statisztikákat.
                .Select(g => CalibrateGroup(data, grouping, objective, options, g.Key, g.ToArray()))
                .ToList();
        }

        private CalibrationResult CalibrateGroup(
            CalibrationDataset data,
            IGroupingStrategy grouping,
            IErrorObjective objective,
            CalibrationOptions options,
            GroupKey key,
            Measurement[] samples)
        {
            // Az RSSI0 értékeket egyszer kiolvassuk, hogy a belső ciklus szótárkeresés-mentes legyen.
            double[] rssi0 = new double[samples.Length];
            for (int i = 0; i < samples.Length; i++)
            {
                rssi0[i] = data.ApOf(samples[i]).Rssi0;
            }

            // Buffer a becsült távolságok és a valós távolságok közötti hibák tárolására.
            double[] buffer = new double[samples.Length];

            // Segédmetódus a célfüggvény kiszámítására adott 'n' és RSSI0 eltolás mellett.
            double Cost(double n, double offset)
            {
                // Minden mintára kiszámítjuk a becsült távolságot az adott 'n' és RSSI0 eltolás mellett,
                for (int i = 0; i < samples.Length; i++)
                {
                    double estimated = _model.EstimateDistance(samples[i].Rssi, rssi0[i] + offset, n);
                    buffer[i] = estimated - samples[i].TrueDistance;
                }
                return objective.Evaluate(buffer);
            }

            double bestN;
            double bestOffset;
            double bestValue;

            // Ha az RSSI0 eltolást nem optimalizáljuk, egyszerűen meghívjuk az optimalizálót az 'n' dimenzióban.
            if (!options.OptimizeRssi0Offset)
            {
                Optimum1D optimum = _optimizer.Minimize(n => Cost(n, 0), options.NBounds);
                (bestN, bestOffset, bestValue) = (optimum.X, 0, optimum.Value);
            }
            else
            {
                // Beágyazott keresés:
                // minden RSSI0-eltoláshoz a belső optimalizáló
                // megkeresi a hozzá tartozó legjobb n-t. A külső dimenzió durva rács,
                // mert az eltolás hatása sima és lassan változó.
                (bestN, bestOffset, bestValue) = (0, 0, double.PositiveInfinity);
                double stepSize = options.Rssi0OffsetBounds.Length / (options.Rssi0OffsetSteps - 1);

                for (int i = 0; i < options.Rssi0OffsetSteps; i++)
                {
                    double offset = options.Rssi0OffsetBounds.Min + i * stepSize;
                    Optimum1D optimum = _optimizer.Minimize(n => Cost(n, offset), options.NBounds);
                    if (optimum.Value < bestValue)
                    {
                        (bestN, bestOffset, bestValue) = (optimum.X, offset, optimum.Value);
                    }
                }
            }

            // A legjobb 'n' és RSSI0 eltolás mellett kiszámítjuk a becsült távolságokat és a hibákat minden mintára.
            List<ResidualRow> residuals = samples.Select((Measurement s, int i) => new ResidualRow(
                    s.ApId,
                    s.PointId,
                    s.Rssi,
                    s.TrueDistance,
                    _model.EstimateDistance(s.Rssi, rssi0[i] + bestOffset, bestN)))
                .ToList();

            // A hibák statisztikáinak kiszámítása a residuals listából.
            ErrorStatistics stats = ErrorStatistics.From(residuals.Select(r => r.Error).ToList());

            // A csoporthoz tartozó Access Point azonosítók listájának előállítása, rendezve és duplikátumok nélkül.
            List<string> apIds = samples.Select(s => s.ApId)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
                .ToList();

            return new CalibrationResult(
                key, grouping.Name, bestN, bestOffset, bestValue, stats, residuals, apIds);
        }

        /// <summary>
        /// Végrehajt egy 'n' sweep-et a megadott mérések és célfüggvény alapján, a megadott határok között.
        /// </summary>
        /// <remarks>
        /// 'n' sweep azt jelenti, hogy a megadott határok között egyenletesen elosztott 'n' értékekre kiszámítjuk a célfüggvény értékét.
        /// </remarks>
        /// 
        /// <param name="data">
        /// A kalibrációhoz használt adatkészlet, amely tartalmazza az Access Pointokat és a méréseket.
        /// </param>
        /// 
        /// <param name="samples">
        /// A mérések listája, amelyeken a sweep-et végrehajtjuk. Ezek a mérések tartalmazzák az RSSI értékeket és a valós távolságokat.
        /// </param>
        /// 
        /// <param name="objective">
        /// A célfüggvény, amely meghatározza, hogyan értékeljük a becsült távolságok és a valós távolságok közötti hibát.
        /// </param>
        /// 
        /// <param name="bounds">
        /// A sweep során vizsgált 'n' értékek határai. Ez egy intervallum, amely meghatározza a minimum és maximum 'n' értékeket.
        /// </param>
        /// 
        /// <param name="steps">
        /// A sweep során használt lépések száma. Ez határozza meg, hogy hány egyenletesen elosztott 'n' értéket vizsgálunk a megadott határok között.
        /// </param>
        /// 
        /// <returns>
        /// Egy listát, amely minden elem egy tuple-t tartalmaz, ahol az első elem az 'n' érték, a második elem pedig a célfüggvény értéke az adott 'n' mellett.
        /// </returns>
        public IReadOnlyList<(double N, double Value)> Sweep(
            CalibrationDataset data,
            IEnumerable<Measurement> samples,
            IErrorObjective objective,
            Interval bounds,
            int steps = 201)
        {
            // A méréseket tömbbé alakítjuk, hogy gyorsabb legyen a hozzáférés a belső ciklusban.
            Measurement[] arr = samples.ToArray();

            // Buffer a becsült távolságok és a valós távolságok közötti hibák tárolására.
            double[] buffer = new double[arr.Length];

            // A sweep során használt lépésköz kiszámítása az intervallum hosszából és a lépések számából.
            double stepSize = bounds.Length / (steps - 1);

            // A sweep eredményeit tároló lista, ahol minden elem egy tuple az 'n' értékkel és a célfüggvény értékével.
            List<(double, double)> curve = new List<(double, double)>(steps);

            // Végigmegyünk az 'n' értékeken a megadott határok között, és kiszámítjuk a célfüggvény értékét minden 'n' mellett.
            for (int i = 0; i < steps; i++)
            {
                double n = bounds.Min + i * stepSize;
                for (int j = 0; j < arr.Length; j++)
                {
                    AccessPoint ap = data.ApOf(arr[j]);
                    buffer[j] = _model.EstimateDistance(arr[j].Rssi, ap.Rssi0, n) - arr[j].TrueDistance;
                }
                curve.Add((n, objective.Evaluate(buffer)));
            }

            return curve;
        }
    }
}