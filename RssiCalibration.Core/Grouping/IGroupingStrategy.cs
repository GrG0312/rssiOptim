using RssiCalibration.Core.Models;

namespace RssiCalibration.Core.Grouping
{
    /// <summary>
    /// Meghatározza, hogy az AccessPoint-ok milyen módon legyenek csoportosítva.
    /// Az általánosabb csoportosítások esetében a csoportok nagyobbak, ezáltal több AP fog egy n értéken osztozni, így a kalibrációs értékek is általánosabbak lesznek.
    /// A specifikusabb csoportosítások esetében a csoportok kisebbek, így az AP-khez tartozó kalibrációs értékek is specifikusabbak lesznek.
    /// </summary>
    public interface IGroupingStrategy
    {
        /// <summary>
        /// A csoportosítás neve, amelyet a felhasználó is lát.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// A csoportosítás leírása, amelyet a felhasználó is lát.
        /// </summary>
        public string Description { get; }

        /// <summary>
        /// A csoportosítási stratégia alapján visszaadja a méréshez tartozó csoport kulcsot.
        /// </summary>
        ///
        /// <remarks>
        /// A kulcsot a mérésből képezzük, nem az eszközkatalógusból: egy AP-helyen több
        /// gyártó + frekvencia párosú eszköz is megfordulhat, így a besorolást csak a konkrét
        /// leolvasás dönti el.
        /// </remarks>
        ///
        /// <param name="m">
        /// A mérés, amelyhez a csoport kulcsot szeretnénk lekérdezni.
        /// </param>
        ///
        /// <returns>
        /// A csoport kulcs, amely alapján a mérést a megfelelő csoportba soroljuk.
        /// </returns>
        public GroupKey GetKey(Measurement m);
    }
}
