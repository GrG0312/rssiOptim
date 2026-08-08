using RssiCalibration.Core.Grouping;
using RssiCalibration.Core.Objectives;
using VisiLib.Args.Parsing;
using VisiLib.Args.Parsing.Builtin;

namespace RssiCalibration.Cli.Settings
{
    /// <summary>
    /// A célfüggvény neve. A választékot a <see cref="ObjectiveFactory"/> adja, így
    /// egy új célfüggvény felvétele után itt nincs teendő.
    /// </summary>
    public sealed class ObjectiveParser : ChoiceParser
    {
        /// <summary>
        /// A gyár által szintén elfogadott, de a súgóban nem hirdetett rövidítések.
        /// </summary>
        private static readonly Dictionary<string, string> Aliases = new()
        {
            ["mae"] = "mean",
            ["mdae"] = "median",
            ["robust"] = "composite"
        };

        /// <summary>
        /// Inicializálja a célfüggvény-parsert az elérhető nevek és aliasok alapján.
        /// </summary>
        public ObjectiveParser() : base(ObjectiveFactory.AvailableNames, Aliases) { }
    }

    /// <summary>
    /// Az optimalizáló eljárás neve.
    /// </summary>
    public sealed class OptimizerParser : ChoiceParser
    {
        /// <summary>
        /// Inicializálja az optimalizáló-parsert a három elérhető eljárás nevével.
        /// </summary>
        public OptimizerParser() : base(["grid", "golden", "hybrid"]) { }
    }

    /// <summary>
    /// A futtatandó csoportosítási stratégia neve.
    /// </summary>
    /// <remarks>
    /// A "mind" kulcsszó <c>null</c>-ra képződik le, ez jelenti azt, hogy mind az öt
    /// stratégia lefut és összehasonlításra kerül.
    /// </remarks>
    public sealed class StrategyParser : ChoiceParser
    {
        /// <summary>
        /// Inicializálja a stratégia-parsert az összes elérhető stratégia nevével,
        /// a "mind" kulcsszóval jelölve a "mindegyik futtatása" opciót.
        /// </summary>
        public StrategyParser() : base(
            GroupingStrategies.All.Select(s => s.Name).ToArray(),
            noneKeyword: "mind")
        { }
    }

    /// <summary>
    /// Fájl- vagy könyvtárútvonal.
    /// </summary>
    /// <remarks>
    /// A létezést szándékosan nem itt ellenőrizzük: a felhasználó beállíthat olyan
    /// útvonalat is, ami majd csak a futtatás idejére jön létre. A hiányzó fájlról
    /// a betöltés ad hibát.
    /// </remarks>
    public sealed class PathParser : ValueParser<string>
    {
        /// <inheritdoc />
        public override string TypeName => "útvonal";

        /// <inheritdoc />
        protected override ParseResult ParseCore(string text)
        {
            string path = text.Trim();

            if (path.Length == 0)
            {
                return ParseResult.Fail("az útvonal nem lehet üres.");
            }

            // Ellenőrizzük, hogy az útvonal nem tartalmaz-e érvénytelen karaktereket
            if (path.IndexOfAny(Path.GetInvalidPathChars()) >= 0)
            {
                return ParseResult.Fail($"'{text}' érvénytelen karaktert tartalmaz.");
            }

            return ParseResult.Ok(path);
        }

        /// <summary>
        /// A megjelenítésnél jelezzük, ha a megadott fájl vagy könyvtár nincs meg -
        /// így a <c>show</c> kimenetén azonnal látszik a leggyakoribb hiba oka.
        /// </summary>
        public override string Format(object? value)
        {
            if (value is not string path || path.Length == 0)
            {
                return "(nincs)";
            }

            bool exists = File.Exists(path) || Directory.Exists(path);
            return exists ? path : $"{path}  (még nem létezik)";
        }
    }
}
