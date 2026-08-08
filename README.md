# RSSI path-loss kalibráció

Az optimális **n** (környezeti csillapítás) megkeresése a log-distance modellhez:

```
d = 10 ^ ((RSSI0 - RSSI) / (10 * n))
```

A program csoportonként megkeresi azt az `n` értéket, amelynél a becsült és a valós
távolság közti hiba a választott célfüggvény szerint minimális, és összehasonlítja
a csoportosítási stratégiákat.

## Futtatás

A program **interaktív**: elindítod, és utána adod ki neki az utasításokat.
Indítási kapcsolókat nem vesz át.

```bash
dotnet run --project RssiCalibration.Cli
```

Egy tipikus munkamenet:

```
rssi> run                        # futtatás az alapbeállításokkal
rssi> set objective composite    # célfüggvény cseréje
rssi> set nmax 5.5               # szűkebb keresési tartomány
rssi> show                       # mit állítottam át eddig?
rssi> run                        # újrafuttatás
rssi> exit
```

### Parancsok

| parancs | mit csinál |
|---|---|
| `help` | a parancsok listája |
| `help param` | az összes állítható paraméter, magyarázattal |
| `help <név>` | egy parancs vagy egy paraméter részletes leírása |
| `show` | a jelenlegi beállítások; `*` jelöli, amit átállítottál |
| `show <param>` | egyetlen paraméter értéke és típusa |
| `set <param> <érték>` | beállítás módosítása |
| `reset [param]` | visszaállítás alapértékre (név nélkül: mindent) |
| `run` | a kalibráció lefuttatása és a riportok kiírása |
| `exit` | kilépés (a Ctrl+Z is ezt teszi) |

Az igen/nem kapcsolókat érték nélkül is be lehet kapcsolni (`set free-rssi0`), a
szóközös útvonalakat pedig idézőjelbe kell tenni:
`set aps "C:\mérési adatok\ap.csv"`.

### Paraméterek

| paraméter | jelentés | alap |
|---|---|---|
| `aps` | AP-k CSV fájlja | `data/access-points.csv` |
| `measurements` | mérések CSV fájlja | `data/measurements.csv` |
| `separator` | CSV elválasztó (`tab`, `comma` névvel is) | `;` |
| `aggregate` | több RSSI minta összevonása: `none`, `mean`, `median` | `none` |
| `objective` | célfüggvény | `median` |
| `free-rssi0` | az RSSI0-t is hangolja csoportonként | `nem` |
| `strategy` | `mind`, `global`, `vendor`, `band`, `vendor-band`, `per-ap` | `mind` |
| `optimizer` | `grid`, `golden`, `hybrid` | `hybrid` |
| `nmin`, `nmax` | az `n` keresési tartománya | `1.0`, `6.0` |
| `out` | riportok könyvtára | `output` |
| `worst` | hány legnagyobb hibát listázzon | `10` |
| `sweep` | exportálja-e az `n` -> hiba görbét | `igen` |

## Bemeneti formátum

`data/access-points.csv`

| oszlop | jelentés |
|---|---|
| ApId | az AP azonosítója |
| Vendor | gyártó (csoportosításhoz) |
| FrequencyMHz | frekvencia MHz-ben; ebből származik a 2.4/5 GHz sáv |
| Rssi0 | referencia RSSI 1 méteren, dBm |

`data/measurements.csv`

| oszlop | jelentés |
|---|---|
| ApId | melyik AP |
| PointId | melyik mérésipont |
| Rssi | mért jelerősség, dBm |
| TrueDistance | valós távolság, méter |

Ha egy (ApId, PointId) párhoz több RSSI mintád van, egyszerűen írj több sort, és
használd a `set aggregate median` beállítást. Tizedesvessző is elfogadott.

## Kimenet

| fájl | tartalom |
|---|---|
| `output/summary.csv` | stratégiánként és csoportonként az optimális n és a hibastatisztikák |
| `output/residuals.csv` | minden egyes (AP, pont) pár becsült távolsága és hibája |
| `output/sweep.csv` | az n -> célfüggvény görbe; Excelben ábrázolva látszik, mennyire éles az optimum |

## Célfüggvények

| név | mikor |
|---|---|
| `mean` | átlagos abszolút hiba - kiegyensúlyozott, de a kiugrók elhúzzák |
| `median` | medián abszolút hiba - robusztus, a kiugrókat figyelmen kívül hagyja |
| `rmse` | erősen bünteti a nagy hibákat |
| `huber` | 10 m alatt négyzetes, felette lineáris - jó kompromisszum |
| `composite` | 0.7 * medián + 0.3 * P90 - "legyen jó a tipikus hiba, de a kiugrók se szálljanak el" |

A dokumentumban leírt cél (kis átlag/medián **és** kordában tartott kiugrók)
pontosan a `composite`-ra illik.

## Projektek

| projekt | mi van benne |
|---|---|
| `Argus` | általános célú paraméter- és parancskezelő könyvtár (lásd lent) |
| `RssiCalibration.Core` | modellek, célfüggvények, optimalizálók, csoportosítás |
| `RssiCalibration.Data` | CSV beolvasás és mintaaggregálás |
| `RssiCalibration.Cli` | az interaktív felület és a riportok |

## Bővítési pontok

| interfész | mit cserélsz vele |
|---|---|
| `IPathLossModel` | pl. ITU beltéri modell, floor-attenuation factor |
| `IErrorObjective` | tetszőleges hibametrika |
| `IOptimizer1D` | más keresési eljárás |
| `IGroupingStrategy` | mely AP-k osztoznak egy n-en |
| `IValueParser` | új paramétertípus az Argus-ban |
| `IShellCommand<T>` | új parancs az interaktív felületen |

---

# Argus

Az `Argus` projekt önálló, függőségmentes könyvtár: a paraméterek leírását, a
típus szerinti értelmezésüket és az interaktív parancsértelmezőt adja. Nem
hivatkozik a projekt többi részére, így más solutionbe is átemelhető.

## Az alapötlet

A paraméterek egyetlen helyen, egy sima beállítás-osztályban élnek. A név, a
súgószöveg, az alapérték és a típus egymás mellett van - nincs külön argumentum-,
súgó- és validációs kód, amit szinkronban kellene tartani.

```csharp
public sealed class CalibrationSettings
{
    [Option("objective",
        Aliases = new[] { "obj" },
        Category = "MODELL",
        Parser = typeof(ObjectiveParser),
        Help = "A minimalizálandó hibametrika.")]
    public string Objective { get; set; } = "median";

    [Option("nmax", Category = "KERESÉS", Help = "Az n tartomány felső határa.")]
    public double NMax { get; set; } = 6.0;
}
```

Ebből az `OptionModel` reflexióval felderíti a paramétereket, az alapértékeket
pedig egy friss példányból olvassa ki - ezért maradhatnak ott, ahol a
legolvashatóbbak: a property inicializálójában.

## Típus szerinti parserek

Minden paraméterértéket egy `IValueParser` olvas be. A választás típus alapján
történik a `ParserRegistry`-ből; enumokhoz a registry menet közben gyárt parsert,
így azokat nem kell regisztrálni.

```csharp
var registry = ParserRegistry.CreateDefault()   // szöveg, egész, szám, igen/nem, karakter
    .Register(new TimeSpanParser())             // saját típus
    .Register("MAC-cím", MacAddress.Parse);     // vagy egyetlen függvénnyel
```

Saját parser a `ValueParser<T>`-ből származik. A hibás bemenet nem kivétel, hanem
visszaadott eredmény - a felhasználó elgépelése normális esemény:

```csharp
public sealed class PathParser : ValueParser<string>
{
    public override string TypeName => "útvonal";

    protected override ParseResult ParseCore(string text) =>
        text.Trim().Length > 0
            ? ParseResult.Ok(text.Trim())
            : ParseResult.Fail("az útvonal nem lehet üres.");
}
```

Egy property felül is írhatja a típus szerinti alapértelmezést az
`[Option(Parser = typeof(...))]` megadásával - így lehet két `string` paraméternek
két különböző értelmezése (pl. útvonal és rögzített névlista).

A véges értékkészletű paraméterekhez van kész `ChoiceParser`, ami már a beírás
pillanatában visszautasítja a rossz nevet, és fel is sorolja az elfogadottakat:

```csharp
public sealed class OptimizerParser() : ChoiceParser(["grid", "golden", "hybrid"]);
```

## A shell

A `CommandShell<TSettings>` ismeri a `help`, `show`, `set`, `reset` és `exit`
parancsokat; az alkalmazás ehhez adja a sajátjait.

```csharp
CommandShell<CalibrationSettings>
    .Create(ParserRegistry.CreateDefault(), new CalibrationSettings())
    .WithPrompt("rssi")
    .WithBanner(PrintBanner)
    .Register(new RunCommand())
    .Run();
```

A felhasználói hibák (ismeretlen parancs, rossz érték, hiányzó fájl) nem
szakítják meg a munkamenetet: a shell kiírja őket, és jöhet a következő parancs.
