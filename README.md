# RSSI path-loss kalibráció

*Magyar leírás alább. [English description below](#english).*

---

## Projektek

| projekt | mi van benne |
|---|---|
| `RssiCalibration.Core` | A matematika: modellek, path-loss képlet, csoportosítás, célfüggvények, optimalizálók, a motor. I/O nincs benne |
| `RssiCalibration.Data` | CSV beolvasás, a három fájl összefűzése, mintaösszevonás |
| `VLib` (`VLib.Args`) | Általános célú interaktív shell és paraméterkezelő könyvtár. Semmit nem tud az RSSI-ről |
| `RssiCalibration.Cli` | A beállítások, a `run` parancs, a konzoltáblák és a CSV riportok |

## A megoldandó egyenlet

```
d = 10 ^ ((RSSI0 - RSSI) / (10 * n))
```

Az `RSSI0` eszközönként adott, az `n` az egyetlen ismeretlen. Az egész program nem más, mint annak az egydimenziós minimalizálása `n` szerint, hogy „mennyire tévednek a távolságbecsléseim”. A szélsőséges kitevőket a kód levágja, így egy elszálló `n` sem tud végtelenbe fordulni.

## Bemenet: három CSV, betöltéskor összefűzve

Azért három táblázat, hogy mindegyik kézzel is kényelmesen tölthető legyen. Alapértelmezésben pontosvesszővel elválasztva, tizedesvesszőt is elfogad.

| fájl | oszlopok | sorok a mintaadatban | szerep |
|---|---|---|---|
| `access-points.csv` | `Vendor; FrequencyGHz; Rssi0` | 6 | **Eszközkatalógus** – Unifi/Ruckus/Cambium × 2.4/5 GHz |
| `measurements.csv` | `ApId; PointId; TrueDistance` | 24 | **Geometria** – lemért távolságok, AP1–AP4 × P1–P6 |
| `readings.csv` | `ApId; PointId; Vendor; FrequencyGHz; Rssi` | 144 | **A leolvasások** – 6 eszköz × 24 páros |

A lényegi modellezési döntés: **az AP-hely egy hely, nem egy doboz.** Az eszközök szabadon cserélhetők a helyek között, ezért nincs saját azonosítójuk – a kulcs a `(Vendor, FrequencyGHz)` páros, és az `RSSI0` az eszközhöz tartozik, nem a helyhez. A gyártóneveket kis- és nagybetűtől függetlenül, a frekvenciákat három tizedesre kerekítve illeszti, így az egyik fájlban lévő `2,4` és a másikban lévő `2.40` ugyanarra az eszközre mutat. A sávbesorolás egyetlen küszöb: 3 GHz alatt `2.4GHz`, felette `5GHz`.

A betöltés a hármat egyetlen listává fűzi össze, amelyben minden leolvasás az RSSI-jét *és* a valós távolságát is magával hozza. Megállítja a futást: üres katalógus, ismétlődő eszköz, ismétlődő `(AP, pont)` távolság, olyan leolvasás, amelynek `(AP, pont)` párosához nincs távolság, és olyan leolvasás, amely a katalógusban nem szereplő eszközre hivatkozik. Az ismétlődést nem csendben felülírja, hanem elutasítja – egy duplikátum szinte biztosan elgépelés.

Az **üres `Rssi` mezőjű sorokat átugorja**, és ez szándékos munkafolyamat-támogatás: a `readings.csv` előre legenerálható az összes kombinációval, és a felmérés ütemében tölthető ki. Az `agg` beállítással az ugyanahhoz az `(AP, pont, eszköz)` hármashoz tartozó ismételt leolvasások átlagra vagy mediánra vonhatók össze.

A CSV-olvasó szándékosan kicsi: üres sorok és `#` kommentek kihagyása, kis- és nagybetűre érzéketlen fejléc, pontos oszlopszám-ellenőrzés `fájl:sor` hibaüzenettel, `,` → `.` csere a számmá alakítás előtt. Idézőjeles mezőket nem kezel.

## A kalibrációs ciklus

A méréseket a választott stratégia csoportokba osztja, és minden csoportot külön old meg:

- Költségfüggvény: egy adott `n`-re megbecsüli minden minta távolságát, kivonja a valós távolságot, és a kapott hibavektort a célfüggvény egyetlen számmá redukálja.
- Ezt kapja meg az optimalizáló, amely az `n`-t az `[nmin, nmax]` tartományon belül keresi.
- A győztes `n` mellett minden mintát újraszámol – ebből lesznek a reziduálisok és a statisztikák.

Bekapcsolt `free-rssi0` mellett ez beágyazott kereséssé válik: egy külső, 81 lépéses rács a −10…+10 dBm-es `RSSI0`-korrekción, és minden lépésében egy teljes belső `n`-keresés. Az aszimmetria szándékos – az `n` valódi optimalizálót kap, mert a költséggörbéje éles, míg az eltolás csak rácsot, mert a hatása sima. Nyolcvanegyszeres munka, és valódi túlillesztési kockázat, ezért alapból ki van kapcsolva.

Ettől függetlenül az `n`-t **zárt képlettel** is kiszámolja egy log-térbeli legkisebb négyzetes illesztés – egy menetben, iteráció nélkül –, és referenciaértékként kiírja. Ha az optimalizáló ettől messze áll meg, valami baj van. Kiindulási értéknek nem használja.

### Hibastatisztikák

Csoportonként számolja, és mindig teljes egészében kiírja, bármelyik célfüggvény vezette is a keresést:

| mutató | mit jelent |
|---|---|
| `MeanError` | Előjeles átlag – egyedül ez mutatja a **torzítást** (rendszeres alá- vagy fölébecslést) |
| `MAE` | Átlagos abszolút hiba |
| `MedianAE` | Az abszolút hibák mediánja |
| `RMSE` | Négyzetes középérték – mindig ≥ MAE, és a különbség a kiugró értékekkel nő |
| `P90` | A 90. percentilis abszolút hiba |
| `MaxAE` | A legrosszabb egyedi minta |

## Csoportosítási stratégiák – `set strategy <név>` (alapértelmezés: nincs beállítva = mind lefut)

Azt dönti el, mely mérések osztoznak egy `n` értéken.

| érték | egy `n` … szerint | csoport | minta/csoport | illesztett `n` |
|---|---|---|---|---|
| `global` | minden (egyetlen közös) | 1 | 144 | 2.352 |
| `vendor` | gyártó | 3 | 48 | 2.33 – 2.42 |
| `band` | frekvenciasáv | 2 | 72 | 2.20 / 2.38 |
| `vendor-band` | gyártó × sáv | 6 | 24 | 2.25 – 2.51 |
| `per-ap` | AP-hely | 4 | 36 | 2.22 – 2.44 |
| `mind` | – | *(visszaáll a „mind lefut” állapotra)* | – | – |

Ha a `strategy` nincs beállítva, mindegyik stratégia lefut, és a végén összehasonlító táblázat készül; a `set s mind` ide állítja vissza.

A finomabb bontás mindig jobban illeszkedik a saját adataira, és mindig kevesebb mintára támaszkodik – ezért zárul az összehasonlító táblázat azzal a figyelmeztetéssel, hogy a `per-ap` látszólagos fölénye lehet túlillesztés. A mintaadatok ezt alá is támasztják: minden csoport 2.20 és 2.51 közé esik, vagyis itt az őszinte válasz egyetlen közös `n` ≈ 2.35.

## Célfüggvények – `set objective <név>` (alapértelmezés: `median`)

A hibavektort egyetlen minimalizálandó számmá redukálja.

| érték | alias | képlet | kiugró értékek | mikor használd |
|---|---|---|---|---|
| `mean` | `mae` | `avg(\|eᵢ\|)` | Érzékeny | Megbízol az adatban, és sima átlagos hibát akarsz |
| `median` | `mdae` | `median(\|eᵢ\|)` | **Érzéketlen** 50% rossz mintáig | **Alapértelmezés.** Terepi adat, gyanús sorokkal |
| `rmse` | – | `√(avg(eᵢ²))` | Nagyon érzékeny | A nagy tévedések aránytalanul sokba kerülnek |
| `huber` | – | `0.5·eᵢ²` ha `\|eᵢ\| ≤ δ`, egyébként `δ·(\|eᵢ\| − δ/2)`, δ = 10 m | Köztes | RMSE simasága kell, de a kiugró értékek uralma nélkül |
| `composite` | `robust` | `0.7·median(\|e\|) + 0.3·P90(\|e\|)` | Többnyire robusztus, de a farok is számít | Tipikusan jó illeszkedés, ami a legrosszabb eseteket sem hagyja figyelmen kívül |

A `huber` δ-ja, valamint a `composite` súlya és kvantilise **be van drótozva** – az osztályok paraméterként megkapnák őket, de a `set` nem teszi elérhetővé. A 10 méteres δ csak több tíz méteres helyszínen harap; kis területen a `huber` egyszerű RMSE-vé fajul.

## Optimalizálók – `set optimizer <név>` (alapértelmezés: `hybrid`)

Egydimenziós minimumkeresők az `n ∈ [nmin, nmax]` tartományon.

| érték | módszer | kiértékelés csoportonként | pontosság | több minimum esetén jó? |
|---|---|---|---|---|
| `grid` | Nyers erő 5001 egyenletes ponton | 5001, fixen | A lépésköz – `[1,6]`-on kb. 0.001 | **Igen** |
| `golden` | Aranymetszéses keresés: a szakaszt lépésenként φ ≈ 0.618 arányban szűkíti | ~30–35 | 1e-6 | **Nem** – egyetlen völgyet feltételez |
| `hybrid` | 201 pontos rács → aranymetszés a győztes ±1 lépésnyi környezetében | ~235 | 1e-7 | **Igen** |

A `hybrid` a kézenfekvő okból alapértelmezett: nagyjából 20-szor olcsóbb a `grid`-nél, három nagyságrenddel pontosabb, és ugyanúgy robusztus – ráadásul megtartja a durva rács eredményét tartaléknak, ha a finomítás valamiért rosszabbat adna.

A választás ritkán változtat az eredményen, mert a költséggörbe általában egyetlen sima völgy. Ott számít, ahol a `median` vagy a `composite` szakaszonként lapos görbét ad apró lépcsőkkel, illetve bekapcsolt `free-rssi0` mellett, ahol a belső keresés csoportonként 81-szer fut le: ott a `grid` csoportonként 405 000 kiértékelést jelent a `hybrid` ~19 000-ével szemben.

## A beállítások

| kategória | név | aliasok | típus | alapértelmezés |
|---|---|---|---|---|
| Adatforrás | `aps` | `a` | útvonal | `data/access-points.csv` |
| Adatforrás | `measurements` | `m`, `mer` | útvonal | `data/measurements.csv` |
| Adatforrás | `readings` | `rd`, `leolvasas` | útvonal | `data/readings.csv` |
| Adatforrás | `separator` | `sep` | karakter (`tab`/`comma` is írható) | `;` |
| Adatforrás | `aggregate` | `agg` | `None` \| `Mean` \| `Median` | `None` |
| Modell | `objective` | `obj` | lásd a fenti táblát | `median` |
| Modell | `free-rssi0` | `rssi0` | kapcsoló | ki |
| Modell | `strategy` | `s` | lásd a fenti táblát | *(mind)* |
| Keresés | `optimizer` | `opt` | lásd a fenti táblát | `hybrid` |
| Keresés | `nmin` | – | double | `1.0` |
| Keresés | `nmax` | – | double | `6.0` |
| Kimenet | `out` | `o` | útvonal | `output` |
| Kimenet | `worst` | – | egész | `10` |
| Kimenet | `sweep` | – | kapcsoló | be |

Minden `run` előtt ellenőrzés fut – `nmax > nmin`, `worst ≥ 0`, mindhárom bemeneti fájl megvan –, és minden hiba mellé jár egy tipp, például `set aps <útvonal>`. Az, hogy itt álljon meg, és ne a számítás közepén, szándékos.

## Kimenet

**Konzol:** adathalmaz-összefoglaló (darabszámok, gyártók, sávok, a választott célfüggvény és optimalizáló, az `n` tartománya, a zárt képletű `n`), majd stratégiánként egy tábla csoportonként egy sorral és egy összesített sorral, utána a stratégiák összehasonlítása, végül a `worst` darab legnagyobb hibájú minta AP-val, ponttal, eszközzel, RSSI-vel, valós és becsült távolsággal. Ez az utolsó tábla a gyakorlati hibakereső eszköz – egy elgépelt távolság azonnal ott jelenik meg.

**Fájlok** az `out` könyvtárban:

| fájl | tartalom |
|---|---|
| `summary.csv` | Stratégia–csoport páronként egy sor: `n`, eltolás, célfüggvényérték, mind a hat statisztika, az érintett AP-azonosítók |
| `residuals.csv` | Minden minta: stratégia, csoport, `n`, AP, pont, gyártó, frekvencia, RSSI, valós, becsült, hiba |
| `sweep.csv` | Széles formátum – `N` oszlop, utána csoportonként egy oszlop, 501 sor |

Mindhárom **ugyanazt az elválasztót használja, mint a bemenet**, így a magyar Excelhez beállított futtatás olyan fájlokat ad, amelyeket ugyanaz az Excel vissza is olvas. A `sweep.csv` a legfontosabb diagnosztika: az éles völgy azt jelenti, hogy az `n` jól meghatározott, a lapos medence pedig azt, hogy az adat nem szorítja le – ilyenkor a kiírt pontosság hamis biztonságérzet.

## A shell

**Parancssori argumentumot egyáltalán nem vesz át** – a program egy `rssi>` promptot nyit, és minden beállítás onnan történik. Hat parancs van: `help`, `show`, `set`, `reset`, `exit` (mind a VLib-ből) és `run` (az egyetlen szakterületi parancs). Az üres sorokat és a `#` kommenteket kihagyja, az idézőjeles értékeket kezeli, a hibákat üzenet + tipp formában írja ki anélkül, hogy a munkamenet megszakadna – egy rossz CSV vagy elgépelés soha nem dobja ki a felhasználót.

Mivel a beállítások futtatások között megmaradnak, az összehasonlító munkamenet a természetes:

```
set objective median
run
set objective composite
run
```

A VLib a teljes beállítási felületet **reflexióval** építi fel az `[Option]` attribútummal jelölt propertykből: egy friss példányból kiolvassa az alapértékeket, propertynként feloldja a megfelelő értelmezőt, és ebből a modellből generálja a `show`, `set`, `reset` és `help` parancsokat. Egy új beállítás felvétele egyetlen annotált property – semmi más. A névütközés, az írhatatlan property és a nem értelmezhető típus mind indításkor bukik ki, nem az első használatkor; a `bool` propertyket pedig kapcsolóként ismeri fel, így a `set free-rssi0` átbillenti az értéket, nem kér hozzá paramétert.

## Amire érdemes figyelni

**A sweep export figyelmen kívül hagyja az RSSI0 eltolást.** Bekapcsolt `free-rssi0` mellett a `sweep.csv`-ben lévő görbe nem az a görbe, amit az optimalizáló ténylegesen minimalizált, és a minimuma nem esik egybe a kiírt `n`-nel. Ha sosem használod a kapcsolót, ez nem számít.

**A `worst` és a `sweep.csv` az utoljára lefuttatott stratégiából származik** – a kód ezt „a legfinomabb” stratégiaként írja le. Ha mind az öt lefut, ez a `per-ap`, ami a szándék szerint való; ha egyetlen stratégiát rögzítesz, mindkettő abból jön. Ez tehát sorrendfüggő, nem finomság szerint rendezett.

**A csoportok minimális mintaszámát semmi nem ellenőrzi.** Egy kétleolvasásos csoport ugyanolyan magabiztosan kapja meg az `n`-jét, mint egy 144 mintás, és `median` mellett két mintán a célfüggvény egyszerűen a két érték átlaga. Erre semmi nem figyelmeztet.

---

<a name="english"></a>

# RSSI path-loss calibration

*English description. [Magyar leírás fentebb](#rssi-path-loss-kalibráció).*

## Solution layout

| Project | Contains |
|---|---|
| `RssiCalibration.Core` | The maths: models, path-loss formula, grouping, objectives, optimizers, engine. No I/O |
| `RssiCalibration.Data` | CSV reading, the three-file join, sample aggregation |
| `VLib` (`VLib.Args`) | Generic interactive-shell + option-binding library. Knows nothing about RSSI |
| `RssiCalibration.Cli` | Settings, the `run` command, console tables, CSV reports |

## The equation being solved

```
d = 10 ^ ((RSSI0 - RSSI) / (10 * n))
```

`RSSI0` is given per device; `n` is the single unknown. The whole program is a 1-D minimisation of "how wrong are my distance estimates" as a function of `n`. Extreme exponents are clamped so a wild `n` can't overflow into infinity.

## Input: three CSVs joined at load time

Split into three tables so each is comfortable to fill by hand. Semicolon-separated by default, decimal commas accepted.

| File | Columns | Sample rows | Role |
|---|---|---|---|
| `access-points.csv` | `Vendor; FrequencyGHz; Rssi0` | 6 | **Device catalogue** — Unifi/Ruckus/Cambium × 2.4/5 GHz |
| `measurements.csv` | `ApId; PointId; TrueDistance` | 24 | **Geometry** — tape-measured distances, AP1–AP4 × P1–P6 |
| `readings.csv` | `ApId; PointId; Vendor; FrequencyGHz; Rssi` | 144 | **The readings** — 6 devices × 24 pairs |

The crucial modelling decision: **an AP location is a place, not a box.** Devices are swappable between locations, so they have no identity of their own — the key is `(Vendor, FrequencyGHz)`, and `RSSI0` belongs to the device, not the location. Vendor names are matched case-insensitively and frequencies rounded to 3 decimals, so `2,4` in one file and `2.40` in the other resolve to the same device. Band assignment is a single threshold: below 3 GHz is `2.4GHz`, above is `5GHz`.

Loading joins the three into one flat list where each reading carries its RSSI *and* its ground-truth distance. It refuses to continue on: an empty catalogue, duplicate devices, a duplicate `(AP, point)` distance, a reading whose `(AP, point)` has no distance, or a reading naming a device not in the catalogue. Duplicates are rejected rather than silently overwritten — a repeat is almost certainly a typo.

Rows with an **empty `Rssi` are skipped**, which is deliberate workflow support: `readings.csv` can be pre-generated with every combination and filled in as you survey. Optionally, `agg` collapses repeated readings of the same `(AP, point, device)` into their mean or median.

The CSV reader is deliberately small: blank lines and `#` comments skipped, case-insensitive headers, exact column count enforced with `file:line` in the error, `,` converted to `.` before parsing. No quoted-field support.

## The calibration loop

Measurements are grouped by the chosen strategy, and each group is solved independently:

- Define a cost function: for a candidate `n`, estimate every sample's distance, subtract the true distance, and reduce the resulting error vector to one number via the objective.
- Hand that to the optimizer, which searches `n` within `[nmin, nmax]`.
- Recompute every sample at the winning `n` to produce residuals and statistics.

With `free-rssi0` on, it becomes a nested search: an outer 81-step grid over an `RSSI0` correction of −10…+10 dBm, with a full inner `n` search at each step. The asymmetry is deliberate — `n` gets a real optimizer because its cost curve is sharp, while the offset gets a plain grid because its effect is smooth. It costs 81× the work and carries a real overfitting risk, so it's off by default.

Separately, `n` is also computed in **closed form** by a log-space least-squares fit — one pass, no iteration — and printed as a reference. If the optimizer lands far from it, something is wrong. It is not used as a starting point.

### Error statistics

Computed per group and always reported in full, whichever objective drove the search:

| Stat | Meaning |
|---|---|
| `MeanError` | Signed mean — the only one that reveals **bias** (systematic over/under-estimation) |
| `MAE` | Mean absolute error |
| `MedianAE` | Median absolute error |
| `RMSE` | Root mean square — always ≥ MAE, gap widens with outliers |
| `P90` | 90th percentile absolute error |
| `MaxAE` | Worst single sample |

## Grouping strategies — `set strategy <name>` (default: unset = run all)

Decides which measurements share one `n`.

| Value | One `n` per… | Groups here | Samples each | Fitted `n` |
|---|---|---|---|---|
| `global` | everything | 1 | 144 | 2.352 |
| `vendor` | manufacturer | 3 | 48 | 2.33 – 2.42 |
| `band` | frequency band | 2 | 72 | 2.20 / 2.38 |
| `vendor-band` | vendor × band | 6 | 24 | 2.25 – 2.51 |
| `per-ap` | AP location | 4 | 36 | 2.22 – 2.44 |
| `mind` | — | *(resets to "run all five")* | — | — |

Leaving `strategy` unset runs every strategy and prints a comparison; `set s mind` returns to that state.

Finer grouping always fits better in-sample and always rests on fewer samples, which is why the comparison table ends with an explicit warning that `per-ap`'s apparent lead may be overfitting. The sample data bears this out: every group lands between 2.20 and 2.51, suggesting one global `n` ≈ 2.35 is the honest answer here.

## Objectives — `set objective <name>` (default `median`)

Reduces the error vector to the single number being minimised.

| Value | Aliases | Formula | Outliers | Use when |
|---|---|---|---|---|
| `mean` | `mae` | `avg(\|eᵢ\|)` | Sensitive | You trust the data and want plain average error |
| `median` | `mdae` | `median(\|eᵢ\|)` | **Immune** up to 50% bad samples | **Default.** Field data with suspected bad rows |
| `rmse` | — | `√(avg(eᵢ²))` | Very sensitive | Large misses are disproportionately costly |
| `huber` | — | `0.5·eᵢ²` if `\|eᵢ\| ≤ δ`, else `δ·(\|eᵢ\| − δ/2)`, δ = 10 m | Middle ground | You want RMSE's smoothness without outlier domination |
| `composite` | `robust` | `0.7·median(\|e\|) + 0.3·P90(\|e\|)` | Mostly robust, tail still counts | Typically-good fit that doesn't ignore worst cases |

`huber`'s δ and `composite`'s weight and quantile are **hard-coded** — the classes accept them as parameters, but nothing exposes them to `set`. δ = 10 m only bites on sites tens of metres across; on a small site `huber` degenerates into plain RMSE.

## Optimizers — `set optimizer <name>` (default `hybrid`)

1-D minimisers over `n ∈ [nmin, nmax]`.

| Value | Method | Evaluations per group | Precision | Multi-minimum safe? |
|---|---|---|---|---|
| `grid` | Brute force over 5001 evenly spaced points | 5001 fixed | Step size — ~0.001 on `[1,6]` | **Yes** |
| `golden` | Golden-section: shrink the bracket by φ ≈ 0.618 each iteration | ~30–35 | 1e-6 | **No** — assumes one valley |
| `hybrid` | 201-point grid → golden-section within ±1 step of the winner | ~235 | 1e-7 | **Yes** |

`hybrid` is default for the obvious reason: ~20× cheaper than `grid`, three orders of magnitude more precise, still robust — and it keeps the coarse result as a fallback if refinement somehow scores worse.

The choice rarely changes the answer, since the cost curve is normally one smooth valley. It matters with `median`/`composite`, which are piecewise-flat and can have small steps, and with `free-rssi0` on, where the inner search runs 81× per group — `grid` there means 405,000 evaluations per group versus ~19,000.

## All settings

| Category | Name | Aliases | Type | Default |
|---|---|---|---|---|
| Data source | `aps` | `a` | path | `data/access-points.csv` |
| Data source | `measurements` | `m`, `mer` | path | `data/measurements.csv` |
| Data source | `readings` | `rd`, `leolvasas` | path | `data/readings.csv` |
| Data source | `separator` | `sep` | char (`tab`/`comma` spellable) | `;` |
| Data source | `aggregate` | `agg` | `None` \| `Mean` \| `Median` | `None` |
| Model | `objective` | `obj` | see table above | `median` |
| Model | `free-rssi0` | `rssi0` | flag | off |
| Model | `strategy` | `s` | see table above | *(all)* |
| Search | `optimizer` | `opt` | see table above | `hybrid` |
| Search | `nmin` | — | double | `1.0` |
| Search | `nmax` | — | double | `6.0` |
| Output | `out` | `o` | path | `output` |
| Output | `worst` | — | int | `10` |
| Output | `sweep` | — | flag | on |

Validation runs before every `run` — `nmax > nmin`, `worst ≥ 0`, all three input files present — each failure carrying a hint like `set aps <path>`. Failing here rather than mid-calculation is intentional.

## Output

**Console:** a dataset summary (counts, vendors, bands, chosen objective and optimizer, `n` range, closed-form `n`), then per strategy a table of one row per group plus a pooled total row, then the cross-strategy comparison, then the `worst` largest-error samples with AP, point, device, RSSI, true vs estimated distance. That last table is the practical debugging tool — a mistyped distance shows up there immediately.

**Files** in `out/`:

| File | Shape |
|---|---|
| `summary.csv` | One row per (strategy, group): `n`, offset, objective value, all six stats, contributing AP IDs |
| `residuals.csv` | Every sample: strategy, group, `n`, AP, point, vendor, frequency, RSSI, true, estimated, error |
| `sweep.csv` | Wide format — column `N`, then one column per group, 501 rows |

All three use the **same separator as the input**, so a run configured for Hungarian Excel produces files that same Excel reads back. `sweep.csv` is the diagnostic that matters most: a sharp valley means `n` is well determined; a flat basin means the data doesn't pin it down and the reported precision is false comfort.

## The shell

**No command-line arguments are accepted at all** — the program starts an `rssi>` prompt and everything is set from inside. Six commands: `help`, `show`, `set`, `reset`, `exit` (all from VLib) and `run` (the only domain command). Blank lines and `#` comments are ignored, quoted values are honoured, and errors print as message plus hint without killing the session — a bad CSV or typo never ends the run.

Because settings persist between runs, the comparative workflow is the natural one:

```
set objective median
run
set objective composite
run
```

VLib builds the entire settings UI by **reflection** over `[Option]`-annotated properties: it captures defaults from a fresh instance, resolves a parser per property, and generates `show`, `set`, `reset` and `help` from that model. Adding a setting means adding one annotated property and nothing else. Clashing names, unwritable properties and unparseable types all fail at startup rather than at first use, and `bool` properties are detected as flags, so `set free-rssi0` toggles rather than requiring a value.

## Sharp edges worth knowing

**The sweep export ignores the RSSI0 offset.** With `free-rssi0` on, the curve in `sweep.csv` is not the curve the optimizer actually minimised, and its minimum won't line up with the reported `n`. Harmless if you never use the flag.

**`worst` and `sweep.csv` come from the last strategy executed** — described in the code as "the finest". Running all five that's `per-ap`, which is the intent; pin a single strategy and both come from that one. It's ordering-dependent rather than sorted by granularity.

**Groups are never checked for a minimum sample count.** A group with two readings gets an `n` reported with the same authority as one with 144, and with `median` on two samples the objective is just their average. Nothing warns about this.
