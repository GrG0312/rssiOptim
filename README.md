# RSSI path-loss kalibráció

Ez a program azt keresi meg, hogy **egy adott épületben / környezetben milyen erősen
gyengül a Wi-Fi jel a távolsággal** – és ebből megmondja, mennyire pontosan lehet
a jelerősségből távolságot becsülni.

Ha csak ki akarod próbálni, elég ennyi:

```bash
dotnet run --project RssiCalibration.Cli
```

majd a megjelenő `rssi>` promptba írd be:

```
run
```

A többit ez a dokumentum lépésről lépésre elmagyarázza.

---

## Tartalom

1. [Mit csinál a program?](#1-mit-csinál-a-program)
2. [Az alapfogalmak egyszerűen](#2-az-alapfogalmak-egyszerűen)
3. [Indítás](#3-indítás)
4. [Az első futtatás – végigvezetés](#4-az-első-futtatás--végigvezetés)
5. [A parancsok](#5-a-parancsok)
6. [A beállítható paraméterek](#6-a-beállítható-paraméterek)
7. [Tipikus munkamenetek (receptek)](#7-tipikus-munkamenetek-receptek)
8. [A saját mérési adataid használata](#8-a-saját-mérési-adataid-használata)
9. [A kimeneti fájlok](#9-a-kimeneti-fájlok)
10. [Gyakori hibaüzenetek](#10-gyakori-hibaüzenetek)
11. [Hogyan működik belül?](#11-hogyan-működik-belül)
12. [Bővítési pontok](#12-bővítési-pontok)
13. [Az Argus könyvtár](#13-az-argus-könyvtár)
14. [Mi van kész és mi hiányzik még](#14-mi-van-kész-és-mi-hiányzik-még)

---

## 1. Mit csinál a program?

### A probléma

Ha egy telefonnal vagy laptoppal "látsz" egy Wi-Fi routert, meg tudod mérni, milyen
erős a jele. Minél messzebb vagy, annál gyengébb a jel. Ebből elvileg vissza lehet
számolni a távolságot – ezen alapul sok beltéri helymeghatározó rendszer.

A visszaszámoláshoz ezt a képletet használjuk:

```
d = 10 ^ ((RSSI0 - RSSI) / (10 * n))
```

- `d` – a keresett távolság méterben
- `RSSI` – a most mért jelerősség
- `RSSI0` – mekkora a jelerősség pontosan 1 méterről (ez az adott AP-ra jellemző, ismert)
- `n` – **ez a bökkenő**: ez mondja meg, milyen gyorsan gyengül a jel

Az `n` értéke a környezettől függ. Szabad ég alatt kb. 2, egy vasbeton falakkal teli
irodaházban lehet 4 fölött is. **Nincs rá képlet – meg kell mérni.**

### A megoldás

Ez a program pont ezt csinálja. Adsz neki:

- egy listát az AP-król (azonosító, gyártó, frekvencia, `RSSI0`),
- és egy csomó mérést, ahol **tudod a valós távolságot is** (pl. lemérted centivel).

A program pedig végigpróbálja az `n` lehetséges értékeit, és megkeresi azt, amelyiknél
a **becsült távolság a lehető legközelebb van a valós távolsághoz**. Ezt hívjuk
kalibrációnak.

Ezen felül összeveti, hogy jobban jársz-e, ha nem egyetlen közös `n`-t használsz
mindenre, hanem külön értéket gyártónként, frekvenciasávonként vagy akár AP-nként.

### Mire jó az eredmény?

A kapott `n` értéket beírod a saját helymeghatározó alkalmazásodba, és onnantól
pontosabb távolságbecsléseket kapsz. A program azt is megmondja, **mennyire bízhatsz
benne**: mekkora hibával kell számolnod méterben.

---

## 2. Az alapfogalmak egyszerűen

Ezek a szavak végig előkerülnek a program kimenetében. Itt egyszer elmagyarázzuk őket.

| szó | mit jelent |
|---|---|
| **AP (Access Point)** | Wi-Fi hozzáférési pont, magyarul router / bázisállomás. |
| **RSSI** | A mért jelerősség. Negatív szám, pl. `-58.6`. Minél közelebb van a nullához, annál erősebb a jel. A `-40` erős, a `-90` nagyon gyenge. |
| **dBm** | Az RSSI mértékegysége (decibel-milliwatt). A lényeg: **10 dBm különbség tízszeres teljesítménykülönbséget jelent**, tehát ez egy "logaritmikus" skála – ezért látszik ilyen furán. |
| **RSSI0** | A referencia-jelerősség pontosan **1 méter** távolságból. Ez az adott AP tulajdonsága, ezt te adod meg a bemeneti fájlban. |
| **n (path-loss exponens)** | A "csillapítási kitevő": milyen gyorsan gyengül a jel a távolsággal. **Ezt keresi a program.** Kb. 2 = szabad tér, 3 = átlagos iroda, 4–5 = sok fal, vastag beton. |
| **path loss (útveszteség)** | Az a jelenség, hogy a jel a terjedés közben gyengül. A "log-distance modell" az a képlet, amivel ezt leírjuk. |
| **mérésipont (PointId)** | Egy hely, ahol mértél. Egy ponton több AP jelét is meg lehet mérni. |
| **hiba / reziduális** | `becsült távolság - valós távolság`, méterben. Ha `+5`, akkor a program 5 méterrel messzebbre saccolt, mint a valóság. |
| **célfüggvény** | Egy szám, ami megmondja, mennyire rossz az összes hiba **együtt**. A program ezt az egy számot próbálja a lehető legkisebbre szorítani. Többféle van, mert másképp lehet "rosszaságot" mérni – lásd lentebb. |
| **optimalizáló** | Az a keresési eljárás, amivel a program végigpróbálja az `n` értékeit. |
| **csoportosítási stratégia** | Melyik AP-k osztozzanak közös `n` értéken. |

### A hibastatisztikák

A program minden csoportra kiírja ezt az öt számot. Mind méterben van:

| rövidítés | mit mond meg | mikor nézd |
|---|---|---|
| **MAE** | Az átlagos hiba (abszolút értékben). *"Átlagosan ennyit tévedek."* | Általános képhez. |
| **medián** | A "középső" hiba: a mérések fele ennél pontosabb, fele ennél rosszabb. | Ha van pár nagyon rossz mérésed, ami elrontja az átlagot. Ez a szám nem hazudik. |
| **RMSE** | Négyzetes átlag. A nagy hibákat **erősen felnagyítja**. | Ha a nagy tévedések különösen fájnak. Mindig ≥ MAE. |
| **P90** | A 90. percentilis: a mérések 90%-a ennél pontosabb. | *"A legrosszabb esetek is beleférnek?"* |
| **max** | A legnagyobb tévedés az egész adathalmazban. | A "legrosszabb eset". |

> **Miért van ebből öt?** Mert egyetlen szám félrevezető tud lenni. Ha a MAE 3 méter,
> de a max 27 méter, akkor tipikusan jó vagy, de van néhány katasztrofális kilengés.
> Az öt szám együtt adja ki a valós képet.

---

## 3. Indítás

### Amire szükséged van

- **.NET 8 SDK** (a projekt `net8.0`-ra épül)
- Semmi más – nincs NuGet-függőség, nincs adatbázis, nincs internet.

### Fordítás és futtatás

```bash
dotnet build
```

```bash
dotnet run --project RssiCalibration.Cli
```

A program **interaktív**: elindul, és utána te adod ki neki a parancsokat.
Indítási kapcsolókat (parancssori argumentumokat) **nem** vesz át –
minden beállítást a `set` paranccsal állítasz, futás közben.

### Hol keresi az adatokat?

Alapból a `data/` könyvtárban, a projekthez mellékelt mintaadatokban. Ezek a
fordításkor a kimeneti könyvtárba is átmásolódnak, tehát a `run` **azonnal
működik**, mielőtt bármit beállítanál.

---

## 4. Az első futtatás – végigvezetés

Indítsd el a programot. Ez fogad:

```
RSSI path-loss kalibráció
Az optimális n (környezeti csillapítás) keresése a log-distance modellhez.

Így használd:
  show                    a jelenlegi beállítások megtekintése
  set <paraméter> <érték> egy beállítás módosítása
  run                     a kalibráció lefuttatása
  help                    minden parancs és paraméter
  exit                    kilépés

Az alapbeállítások a data/ könyvtár mintaadataira mutatnak:
írd be, hogy 'run', és máris látod az eredményt.

rssi>
```

Az `rssi>` a **prompt**: itt vár tőled parancsot. Írd be:

```
run
```

Most végignézzük, mit ír ki, blokkonként.

### 4.1 Az adathalmaz összefoglalója

```
=== ADATHALMAZ ===
Access Point-ok : 4
Mérésipontok    : 6
Minták          : 24
Gyártók         : Cisco, Ubiquiti, TPLink
Sávok           : 2.4GHz, 5GHz
Célfüggvény     : median
Optimalizáló    : hybrid(201)
n tartomány     : [1, 6]
n zárt képlettel: 3.001  (log-térbeli legkisebb négyzetek, referenciaérték)
```

**Ez egy ellenőrző blokk.** Az első dolgod mindig az legyen, hogy megnézed: tényleg
annyi AP-t és mérést olvasott-e be, amennyire számítottál. Ha itt 24 helyett 12 minta
van, akkor a bemeneti fájloddal van baj, és a többi számot már felesleges nézni.

- **Minták**: hány sor mérés van összesen.
- **Sávok**: a program a frekvenciából automatikusan eldönti, hogy 2.4 GHz-es vagy
  5 GHz-es-e az AP (a határ 3000 MHz).
- **n zárt képlettel**: ez egy **második, teljesen független becslés** az `n`-re, egy
  egyszerű képlettel (legkisebb négyzetek módszere), iteráció nélkül. Nem ez az
  eredmény – ez a **kontroll**. Ha a lentebb kapott `n` értékek nagyjából ekörül
  szóródnak, minden rendben. Ha valamelyik nagyon messze van tőle (pl. itt 3.0
  helyett 5.8), az gyanús: valószínűleg kevés vagy zajos az adat abban a csoportban.

### 4.2 Az öt csoportosítási stratégia

Ezután öt hasonló táblázat jön. Mindegyik ugyanazt az adatot dolgozza fel, csak
másképp osztja csoportokra az AP-kat.

**Első: mindenki egy közös `n`-en osztozik.**

```
=== GLOBAL - Egyetlen közös n minden AP-ra ===
Csoport                        n   db      MAE   medián     RMSE      P90       max
-----------------------------------------------------------------------------------
ALL                        3.108   24     6.89     3.15    10.28    18.79     27.41
-----------------------------------------------------------------------------------
ÖSSZESÍTETT                        24     6.89     3.15    10.28    18.79     27.41
```

Olvasd így: *"Ha egyetlen `n`-t használok mindenre, akkor a legjobb választás
`n = 3.108`, és ezzel átlagosan 6.89 métert tévedek, a legnagyobb tévedésem 27.41 méter."*

- **Csoport**: a csoport neve. Itt `ALL`, mert minden AP egy csoportban van.
- **n**: az adott csoportra megtalált legjobb csillapítási kitevő.
- **db**: hány mérésre támaszkodik ez az érték. **Ez fontos!** Egy 6 mintából
  számolt `n` sokkal bizonytalanabb, mint egy 24-ből számolt.
- Az **ÖSSZESÍTETT** sor az összes csoport hibáit egybeönti.

**Második: gyártónként külön `n`.**

```
=== VENDOR - Gyártónként külön n ===
Csoport                        n   db      MAE   medián     RMSE      P90       max
-----------------------------------------------------------------------------------
Cisco                      3.182   12     3.49     1.27     5.14     9.43     10.10
TPLink                     3.436    6     1.81     0.38     2.94     4.86      6.32
Ubiquiti                   2.623    6     3.91     0.91     6.62    10.41     15.11
-----------------------------------------------------------------------------------
ÖSSZESÍTETT                        24     3.17     0.77     5.13     9.17     15.11
```

Az összesített MAE 6.89-ről 3.17-re esett. Tehát **megérte** gyártónként külön
értéket használni – a különböző gyártók rádiói máshogy viselkednek.

Ugyanígy jön még a `BAND` (frekvenciasávonként), a `VENDOR-BAND` (gyártó + sáv
kombináció) és a `PER-AP` (minden AP-nak saját `n`).

### 4.3 Az összehasonlító táblázat

```
=== STRATÉGIÁK ÖSSZEHASONLÍTÁSA (összesített hibák) ===
Stratégia       csoport      MAE   medián     RMSE      P90       max
---------------------------------------------------------------------
global                1     6.89     3.15    10.28    18.79     27.41
vendor                3     3.17     0.77     5.13     9.17     15.11
band                  2     4.27     1.31     7.26    12.82     22.28
vendor-band           4     2.33     0.51     4.48     6.18     15.11
per-ap                4     2.33     0.51     4.48     6.18     15.11

Megjegyzés: a több csoportra bontás mindig jobb illeszkedést ad, de
kevesebb mintára támaszkodik. A 'per-ap' látszólagos fölénye lehet túlillesztés.
```

**Ez a legfontosabb táblázat.** Egy pillantással látod, melyik felosztás éri meg.

Fentről lefelé haladva a csoportok egyre kisebbek, és a hiba egyre kisebb. **Ez
mindig így lesz** – és pont ezért kell óvatosnak lenni.

> **Túlillesztés (overfitting) – mi ez?**
> Ha minden AP-nak saját `n`-t adsz, a program külön-külön "ráhajlíthatja" a görbét
> mindegyik AP néhány mérésére. A **meglévő** adatokon ez remek eredményt ad, de
> könnyen lehet, hogy csak a mérési zajt tanulta meg, nem a valódi fizikát – és **új**
> mérésen rosszabbul fog teljesíteni.
>
> **Ökölszabály:** válaszd a legegyszerűbb felosztást, amelyik még érdemi javulást hoz.
> A fenti példában `global → vendor` óriási ugrás (6.89 → 3.17), a
> `vendor-band → per-ap` viszont **semmit sem javít** (2.33 → 2.33), tehát felesleges.
> Itt a `vendor` vagy a `vendor-band` a józan választás.
>
> Az is árulkodó, ha egy csoportban kevés a **db**: 5-6 mintából számolt `n`-t ne vegyél
> készpénznek.

### 4.4 A legnagyobb hibák

```
=== 10 LEGNAGYOBB HIBA ===
Csoport              AP       Pont        RSSI    valós   becsült      hiba
---------------------------------------------------------------------------
AP3                  AP3      P6         -76.7    45.00     29.89    -15.11
AP1                  AP1      P6         -89.3    45.00     56.16    +11.16
AP4                  AP4      P4         -96.9    22.00     28.32     +6.32
```

Itt az egyes **konkrét mérések** vannak, a legrosszabbtól kezdve. A `hiba` oszlop
előjeles: a `-15.11` azt jelenti, hogy 15 méterrel **közelebbre** saccolt a program,
mint a valóság.

**Erre használd:** vadászd le a hibás méréseket. Ha egy adott pont (`P6`) minden
AP-nál a lista tetején van, akkor valószínűleg **elírtad a távolságot** annál a
pontnál, vagy volt ott valami zavaró tényező (fém szekrény, tömeg, ajtó).

Figyeld meg azt is, hogy a nagy hibák jellemzően a **nagy távolságoknál** jelennek meg.
Ez természetes: 45 méteren a jel már nagyon gyenge, és 1-2 dBm mérési zaj is több
méteres eltérést okoz a becsült távolságban.

### 4.5 A riportok

Végül:

```
Riportok kiírva ide: D:\Programming\rssiOptim\output
```

Három CSV fájl készült, ezekben minden részlet benne van – lásd a
[9. fejezetet](#9-a-kimeneti-fájlok).

### 4.6 Most próbálj ki valamit

A program a `run` után **nem lép ki**, és a beállításokat sem felejti el. Nyugodtan
kísérletezz:

```
rssi> set objective composite
rssi> run
```

Most más "rosszaság-mértéket" használ, és más `n` értékeket fog találni.
Ha kész vagy:

```
rssi> exit
```

---

## 5. A parancsok

| parancs | rövidítések | mit csinál |
|---|---|---|
| `help` | `?`, `h` | a parancsok listája |
| `help param` | | az összes állítható paraméter, magyarázattal |
| `help <név>` | | egy parancs **vagy** egy paraméter részletes leírása |
| `show` | `ls`, `list` | a jelenlegi beállítások; `*` jelöli, amit átállítottál |
| `show <param>` | | egyetlen paraméter értéke, típusa, alapértéke |
| `set <param> <érték>` | `s` | beállítás módosítása |
| `set <param>` | | érték nélkül: kiírja a jelenlegi értéket (kapcsolóknál bekapcsol) |
| `reset` | | **minden** beállítás vissza az alapértékre |
| `reset <param>` | | csak az adott paraméter vissza az alapértékre |
| `run` | `futtat`, `r` | a kalibráció lefuttatása és a riportok kiírása |
| `exit` | `quit`, `q` | kilépés (a **Ctrl+Z** majd Enter is ezt teszi) |

### Jó tudni

**Idézőjel a szóközös útvonalakhoz.** Ha az elérési útban szóköz van, tedd idézőjelbe:

```
set aps "C:\mérési adatok\ap.csv"
```

**Az igen/nem kapcsolók.** A `free-rssi0` és a `sweep` igen/nem típusú. Ha csak a
nevét írod be, az **bekapcsolja**:

```
set free-rssi0
```

Kikapcsolni viszont csak kifejezett értékkel lehet (a puszta név nem billegtet ide-oda):

```
set sweep no
```

Elfogadott igenek: `igen`, `i`, `yes`, `y`, `true`, `on`, `be`, `1`.
Elfogadott nemek: `nem`, `n`, `no`, `false`, `off`, `ki`, `0`.

**Tizedesvessző is jó.** A `set nmax 5,5` és a `set nmax 5.5` ugyanaz.

**Elgépelésnél nem áll le.** Ha rossz nevet vagy értéket írsz be, a program kiírja a
hibát, javaslatot tesz, és mehet a következő parancs. Nem kell újraindítani.

**Kommentek és szkriptelés.** A `#`-tel kezdődő sorokat a program figyelmen kívül
hagyja. Így akár fájlból is beetetheted a parancsokat:

```bash
dotnet run --project RssiCalibration.Cli < parancsok.txt
```

---

## 6. A beállítható paraméterek

A `show` parancs kiírja mindet az aktuális értékkel, a `help param` pedig
magyarázattal együtt. Négy kategóriába vannak sorolva.

### ADATFORRÁS – honnan jöjjön az adat

| paraméter | rövidítés | jelentés | alap |
|---|---|---|---|
| `aps` | `a` | Az AP-k CSV fájlja | `data/access-points.csv` |
| `measurements` | `m`, `mer` | A mérések CSV fájlja | `data/measurements.csv` |
| `separator` | `sep` | A CSV oszlopelválasztója. Írhatod névvel is: `tab`, `comma`, `semicolon`, `space` | `;` |
| `aggregate` | `agg` | Több RSSI minta összevonása: `none`, `mean`, `median` | `none` |

**Az `aggregate` magyarázata.** Ha egy helyen ugyanahhoz az AP-hoz **többször** is
mértél (ez ajánlott, mert az RSSI ingadozik), akkor egyszerűen írj több sort a
mérésfájlba ugyanazzal az `ApId` + `PointId` párral. Ezután:

- `none` – minden sor önálló minta marad (a zaj is bekerül a hibastatisztikákba)
- `mean` – a program átlagol soronként (egyszerű, de egy kilógó érték elhúzza)
- `median` – a középső értéket veszi (**ez az ajánlott**, mert a hibás mérést kidobja)

### MODELL ÉS CÉLFÜGGVÉNY – mit optimalizáljunk

| paraméter | rövidítés | jelentés | alap |
|---|---|---|---|
| `objective` | `obj` | A minimalizálandó hibametrika | `median` |
| `free-rssi0` | `rssi0` | Az `RSSI0`-t is hangolja csoportonként | `nem` |
| `strategy` | `s` | Melyik AP-k osztozzanak egy `n`-en | `mind` |

**A célfüggvények (`objective`).** Ez dönti el, mit jelent az, hogy "a lehető
legkisebb hiba":

| név | mit minimalizál | mikor válaszd |
|---|---|---|
| `mean` (`mae`) | Az átlagos abszolút hibát | Kiegyensúlyozott, de néhány kiugró mérés elhúzhatja. |
| `median` (`mdae`) | A középső hibát | **Robusztus**: a kiugró értékeket gyakorlatilag figyelmen kívül hagyja. Alapértelmezés. |
| `rmse` | A négyzetes átlagot | Ha a nagy tévedések kifejezetten fájnak. Erősen bünteti őket. |
| `huber` | Vegyes | 10 méter alatt négyzetes, felette lineáris. Kompromisszum az `rmse` és a `mean` között. |
| `composite` (`robust`) | `0.7 × medián + 0.3 × P90` | **"Legyen jó a tipikus eset, de a kiugrók se szálljanak el."** Ha nem tudsz dönteni, ez általában jó választás. |

> **Miért nem mindegy?** Mert más-más `n`-t adnak. A `median` azt mondja: "a mérések
> fele legyen minél pontosabb, a többi nem érdekel". Az `rmse` azt: "egyetlen nagy
> tévedés se legyen". Ez a két cél húz egymás ellen. Futtasd le mindkettővel, és nézd
> meg, mennyire tér el az eredmény – ha alig, akkor stabil az adatod.

**A `free-rssi0` magyarázata.** Alapból a program elhiszi a fájlban megadott `RSSI0`
értéket, és csak az `n`-t keresi. Ha bekapcsolod, akkor csoportonként az `RSSI0`-hoz
is hozzátehet egy **eltolást** (±10 dBm között), hátha úgy jobb illeszkedést kap.

Akkor kapcsold be, ha gyanús, hogy az 1 méteres referenciamérésed pontatlan volt.
Ilyenkor megjelenik egy `dRSSI0` oszlop a táblázatban, ami mutatja, mennyivel tolta el.
**Óvatosan:** ez egy második szabad paraméter, tehát növeli a túlillesztés kockázatát.
Ha a program konzisztensen ugyanazt az eltolást találja minden csoportnál, az arra
utal, hogy tényleg rossz volt a referenciaértéked.

**A stratégiák (`strategy`).** `mind` esetén (ez az alap) mind az öt lefut és
összehasonlítja őket. Ha egy konkrét kell:

| név | mit jelent | mikor |
|---|---|---|
| `global` | egyetlen `n` mindenre | homogén környezet, kevés adat |
| `vendor` | gyártónként külön | vegyes hardverpark |
| `band` | 2.4 GHz és 5 GHz külön | az 5 GHz-es jel jobban gyengül, ez gyakran indokolt |
| `vendor-band` | gyártó + sáv kombináció | ha van elég adatod hozzá |
| `per-ap` | AP-nként külön | csak sok mérésnél; egyébként túlillesztés |

### KERESÉS – hogyan keressük az optimumot

| paraméter | rövidítés | jelentés | alap |
|---|---|---|---|
| `optimizer` | `opt` | A keresési eljárás | `hybrid` |
| `nmin` | | Az `n` tartomány alsó határa | `1.0` |
| `nmax` | | Az `n` tartomány felső határa | `6.0` |

**Az optimalizálók.** Mindhárom ugyanazt csinálja – megkeresi a legkisebb hibát adó
`n`-t –, csak máshogy:

| név | hogyan | jó/rossz |
|---|---|---|
| `grid` | 5001 egyenletes pontban kipróbálja | Nem elegáns, de **biztosan** megtalálja a legjobbat a rács felbontásán belül. Lassabb, de nálunk ez is ezredmásodperc. |
| `golden` | Aranymetszéses szűkítés | Nagyon gyors és pontos, **de csak akkor jó, ha a görbének egyetlen völgye van**. A `median` célfüggvény görbéje lépcsős, ezért ott beragadhat egy rossz helyre. |
| `hybrid` | Először durva rács (201 pont), utána aranymetszéssel finomít | **Ez az alapértelmezés, és szinte mindig ez a jó választás**: biztonságos is, pontos is. |

**Az `n` tartománya.** Alapból 1 és 6 között keres. Ez a fizikailag értelmes
tartomány. Ha az eredményed pont a szélére esik (`1.000` vagy `6.000`), az figyelmeztető
jel: vagy hibás az adat, vagy szélesíteni kell a tartományt.

### KIMENET – mit írjon ki

| paraméter | rövidítés | jelentés | alap |
|---|---|---|---|
| `out` | `o` | A riportok könyvtára (ha nincs, létrehozza) | `output` |
| `worst` | | Hány legnagyobb hibát listázzon a végén | `10` |
| `sweep` | | Exportálja-e az `n` → hiba görbét | `igen` |

---

## 7. Tipikus munkamenetek (receptek)

### 7.1 "Csak nézzük meg, mi van"

```
rssi> run
```

Lefut mind az öt stratégia, és látod az összehasonlítást. **Mindig ezzel kezdj.**

### 7.2 Saját adatok betöltése

```
rssi> set aps "C:\meresek\sajat-ap.csv"
rssi> set measurements "C:\meresek\sajat-meresek.csv"
rssi> set separator comma
rssi> show
rssi> run
```

A `show` előtte azért jó, mert a program **jelzi, ha egy megadott fájl nem létezik**
(`(még nem létezik)` felirattal) – így nem a futtatás felénél derül ki.

### 7.3 Több minta pontonként

Ha ugyanarra a (AP, pont) párra több sort írtál a mérésfájlba:

```
rssi> set aggregate median
rssi> run
```

### 7.4 Célfüggvények összehasonlítása

```
rssi> set objective median
rssi> run
rssi> set objective composite
rssi> run
rssi> set objective rmse
rssi> run
```

Ha a három futtatás nagyjából ugyanazt az `n`-t adja, az **jó jel**: stabil az adatod.
Ha nagyon eltérnek, akkor néhány kiugró mérés dominálja az eredményt – érdemes
megnézni a `worst` listát.

### 7.5 Egyetlen stratégia, tiszta kimenettel

```
rssi> set strategy vendor
rssi> set worst 25
rssi> run
```

### 7.6 Gyanús a referenciaértékem

```
rssi> set free-rssi0
rssi> run
```

Nézd meg a `dRSSI0` oszlopot: ha pl. minden csoportnál `-3.5` körüli, akkor a
mérőeszközöd konzisztensen 3.5 dBm-mel másképp mér, mint amit a fájlba írtál.

### 7.7 Vissza az alaphelyzetbe

```
rssi> reset
```

vagy csak egy paramétert:

```
rssi> reset objective
```

---

## 8. A saját mérési adataid használata

Két CSV fájl kell. **Az első nem üres, nem `#`-kezdetű sor a fejléc.**

### `access-points.csv` – az AP-k

| oszlop | jelentés | példa |
|---|---|---|
| `ApId` | az AP azonosítója (bármilyen szöveg, de egyedi legyen) | `AP1` |
| `Vendor` | gyártó – ezt használja a `vendor` csoportosítás | `Cisco` |
| `FrequencyMHz` | frekvencia MHz-ben; ebből jön a 2.4/5 GHz-es sáv (határ: 3000) | `2412` |
| `Rssi0` | referencia RSSI 1 méteren, dBm | `-40.0` |

```csv
ApId;Vendor;FrequencyMHz;Rssi0
AP1;Cisco;2412;-40.0
AP2;Cisco;5180;-45.0
AP3;Ubiquiti;2437;-38.0
AP4;TPLink;5240;-47.0
```

### `measurements.csv` – a mérések

| oszlop | jelentés | példa |
|---|---|---|
| `ApId` | melyik AP-t mérted (**szerepelnie kell** az AP-fájlban) | `AP1` |
| `PointId` | melyik mérésiponton álltál | `P1` |
| `Rssi` | a mért jelerősség, dBm | `-58.6` |
| `TrueDistance` | a valós, lemért távolság, méter | `4.5` |

```csv
ApId;PointId;Rssi;TrueDistance
AP1;P1;-58.6;4.5
AP1;P2;-67.1;9.0
AP1;P3;-72.3;14.0
```

### A beolvasás szabályai

- Az **oszlopnevek sorrendje nem számít**, és a kis/nagybetű sem (`apid` = `ApId`).
- A mezők körüli szóközöket levágja.
- Az **üres sorokat** és a `#`-kel kezdődő **kommentsorokat** átugorja.
- A számoknál **tizedesvessző is jó** (`-58,6`).
- Az elválasztó karaktert a `separator` beállítás adja (alap: `;`, ez a magyar Excel
  alapértelmezése is).
- Ha egy sorban nem annyi mező van, mint a fejlécben, hibát kapsz a **sorszámmal együtt**.

### Amire figyel a program

- Ha ugyanaz az `ApId` kétszer szerepel az AP-fájlban → hiba.
- Ha egy mérés olyan `ApId`-re hivatkozik, ami nincs az AP-fájlban → hiba.
- Ha bármelyik fájl üres vagy csak fejléc van benne → hiba.

### Tippek jó méréshez

- **Legalább 5-6 mérésipont** AP-nként, változatos távolságokon (közel, közép, távol).
- Mindig **le is mérd** a valós távolságot – ha ez pontatlan, a kalibráció is az lesz.
- Egy ponton **több RSSI mintát** végy, és használd a `set aggregate median` opciót.
  Az RSSI másodpercről másodpercre több dBm-et ingadozhat.
- Ne csak egy folyosó mentén mérj: a program azt a környezetet fogja megtanulni,
  amit megmutatsz neki.

---

## 9. A kimeneti fájlok

Minden `run` felülírja őket az `out` beállítás szerinti könyvtárban (alap: `output/`).

| fájl | mi van benne |
|---|---|
| `summary.csv` | Stratégiánként és csoportonként az optimális `n`, az `RSSI0` eltolás és az összes hibastatisztika. |
| `residuals.csv` | **Minden egyes mérés** külön sorban: mit becsült a program, mennyi a valóság, mekkora a hiba. |
| `sweep.csv` | Az `n` → hiba görbe: 501 pontban kiszámolva, mennyi lenne a célfüggvény értéke minden lehetséges `n`-nél. |

### Mire jó a `sweep.csv`?

Ez a legérdekesebb fájl. Az első oszlop az `n` értéke, a többi oszlop egy-egy
csoporté. Ha Excelben kijelölöd és vonaldiagramot rajzolsz belőle, egy **völgyet**
látsz – a völgy alja az optimális `n`.

**A völgy alakja mond el valamit:**

- **Éles, keskeny völgy** → az adatod egyértelműen meghatározza az `n`-t, megbízható.
- **Lapos, széles teknő** → sok `n` érték majdnem ugyanolyan jó. Az "optimális"
  érték itt félig véletlen; ne vedd három tizedesjegyre komolyan.
- **Két völgy** → valami zavar van, valószínűleg kétféle környezetből kevertél adatot.

### Fontos: a kimenet formátuma

A kimeneti CSV-k **vesszővel** vannak elválasztva, és **pontot** használnak
tizedesjelnek (angolszász formátum) – függetlenül attól, mit állítottál be a
`separator`-ban (az csak a **bemenetre** vonatkozik).

Magyar Excelben ezért ne dupla kattintással nyisd meg, hanem:
**Adatok → Szövegből/CSV-ből**, és ott állítsd be a vesszőt elválasztónak, a
területi beállítást pedig angolra.

---

## 10. Gyakori hibaüzenetek

A program a felhasználói hibáktól **nem áll le** – kiírja, mi a baj, gyakran
javaslattal együtt, és jöhet a következő parancs.

| üzenet | mi történt | mit tegyél |
|---|---|---|
| `Nincs meg a fájl: ...` | A megadott CSV nem létezik | `set aps <helyes útvonal>`; a `show` kiírja, létezik-e |
| `No parameter named 'X'` | Elgépelted a paraméter nevét | A program felajánl hasonlókat; `help param` a teljes lista |
| `Invalid value for parameter ...` | Rossz típusú vagy nem megengedett érték | A program felsorolja az elfogadottakat |
| `Unknown command: X` | Nincs ilyen parancs | `help` |
| `Az nmax (...) nem lehet kisebb...` | `nmax` ≤ `nmin` | Állítsd át az egyiket |
| `Ismétlődő AP azonosító: ...` | Kétszer szerepel ugyanaz az `ApId` az AP-fájlban | Javítsd a fájlt |
| `A mérésekben ismeretlen AP azonosító(k)...` | Olyan AP-ra hivatkozik egy mérés, ami nincs az AP-fájlban | Elírás, vagy hiányzó AP-sor |
| `...:12 - 4 oszlop várt, 3 érkezett` | A 12. sorban rossz a mezők száma | Nézd meg azt a sort; gyakran rossz `separator` az ok |
| `'X' nem szám: ...` | Szám helyett szöveg van egy számoszlopban | Javítsd a fájlt |
| `Üres vagy fejléc nélküli fájl` | Nincs fejlécsor | Írj fejlécet a fájl elejére |

---

## 11. Hogyan működik belül?

### A projektek

| projekt | mi van benne | függ ettől |
|---|---|---|
| `Argus` | Általános célú paraméter- és parancskezelő könyvtár. **Semmit nem tud az RSSI-ről.** | – |
| `RssiCalibration.Core` | Modellek, célfüggvények, optimalizálók, csoportosítás. A tényleges számítás. | – |
| `RssiCalibration.Data` | CSV beolvasás és mintaösszevonás. | Core |
| `RssiCalibration.Cli` | Az interaktív felület, a konzolriportok és a CSV kiírás. | mind |

### Mi történik egy `run` alatt?

1. **Ellenőrzés.** A `RunCommand` meghívja a `CalibrationSettings.Validate()`-et:
   `nmax > nmin`? Léteznek a fájlok? Ha nem, hibaüzenet és vége – még mielőtt bármi
   számítás indulna.
2. **Betöltés.** A `CsvDataSource` beolvassa a két CSV-t, ellenőrzi az ismétlődő és
   az árva azonosítókat, és ha kérted, összevonja a mintákat. **Minden `run` újraolvassa
   a fájlokat** – ez szándékos: menet közben átírhatod az adatot, és a következő
   futtatás már a frisset látja.
3. **Összeállítás.** A nevekből példányok lesznek: az `ObjectiveFactory` legyártja a
   célfüggvényt, az `OptimizerFactory` az optimalizálót.
4. **Kontrollérték.** A `LeastSquaresInitializer` egy zárt képlettel is megbecsüli
   az `n`-t – ez a "n zárt képlettel" sor a kimenetben.
5. **Kalibráció.** A `CalibrationEngine` a választott stratégia szerint csoportokba
   osztja a méréseket, és **csoportonként** megkeresi a legjobb `n`-t:
   - végigpróbál sok `n` értéket,
   - mindegyiknél kiszámolja az összes mérés becsült távolságát a log-distance
     képlettel, ebből a hibákat,
   - a hibavektorból a célfüggvény ad egy számot,
   - a legkisebb szám nyer.
6. **Statisztika.** Az `ErrorStatistics` a végleges `n` melletti hibákból számol MAE-t,
   mediánt, RMSE-t, P90-et, maxot.
7. **Riportok.** A `ConsoleReporter` a képernyőre, a `CsvReportWriter` a fájlokba ír.

### A számítás magja

Ez a néhány sor a program szíve (`CalibrationEngine.CalibrateGroup`):

```csharp
double Cost(double n, double offset)
{
    for (int i = 0; i < samples.Length; i++)
    {
        double estimated = _model.EstimateDistance(samples[i].Rssi, rssi0[i] + offset, n);
        buffer[i] = estimated - samples[i].TrueDistance;   // előjeles hiba méterben
    }
    return objective.Evaluate(buffer);                     // egyetlen "rosszaság" szám
}
```

Az optimalizáló ezt a `Cost` függvényt hívogatja különböző `n` értékekkel, amíg meg nem
találja a minimumot. **Minden más ezt szolgálja ki.**

Ha a `free-rssi0` be van kapcsolva, kétszintű a keresés: a külső ciklus végigmegy 81
`RSSI0`-eltolás értéken (−10-től +10 dBm-ig), és mindegyikhez a belső optimalizáló
megkeresi a hozzá tartozó legjobb `n`-t.

---

## 12. Bővítési pontok

A program szándékosan interfészek köré épül. Ha bővíteni akarod, ezeket kell
implementálni – a többi kód változatlanul marad.

| interfész | mit cserélsz vele | hol |
|---|---|---|
| `IPathLossModel` | Maga a fizikai modell (pl. ITU beltéri modell, falszám-korrekció) | `Core/PathLoss` |
| `IErrorObjective` | Tetszőleges hibametrika | `Core/Objectives` |
| `IOptimizer1D` | Más keresési eljárás | `Core/Optimization` |
| `IGroupingStrategy` | Mely AP-k osztozzanak egy `n`-en | `Core/Grouping` |
| `IValueParser` | Új paramétertípus az Argus-ban | `Argus/Parsing` |
| `IShellCommand<T>` | Új parancs az interaktív felületen | `Argus/Shell` |

**Új célfüggvény felvétele** például: írj egy osztályt `IErrorObjective`-vel, vedd fel
az `ObjectiveFactory.Create` switchébe és az `AvailableNames` listájába. Ennyi – a
súgó, a `set` validációja és az elfogadott értékek listája **automatikusan** frissül,
mert az `ObjectiveParser` ebből a listából dolgozik.

**Új paraméter felvétele:** elég egy property a `CalibrationSettings`-be `[Option]`
attribútummal. Sem a shellhez, sem a súgóhoz nem kell hozzányúlni.

---

## 13. Az Argus könyvtár

Az `Argus` projekt önálló, függőségmentes könyvtár: a paraméterek leírását, a típus
szerinti értelmezésüket és az interaktív parancsértelmezőt adja. Nem hivatkozik a
projekt többi részére, így más solutionbe is átemelhető.

### Az alapötlet

A paraméterek **egyetlen helyen**, egy sima beállítás-osztályban élnek. A név, a
súgószöveg, az alapérték és a típus egymás mellett van – nincs külön argumentum-,
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

Ebből az `OptionModel` **reflexióval** felderíti a paramétereket (reflexió = a program
futás közben megvizsgálja a saját osztályait), az alapértékeket pedig egy friss
példányból olvassa ki – ezért maradhatnak ott, ahol a legolvashatóbbak: a property
inicializálójában.

### Típus szerinti parserek

Minden paraméterértéket egy `IValueParser` olvas be. A választás típus alapján történik
a `ParserRegistry`-ből; enumokhoz a registry menet közben gyárt parsert, így azokat nem
kell regisztrálni.

```csharp
var registry = ParserRegistry.CreateDefault()   // szöveg, egész, szám, igen/nem, karakter
    .Register(new TimeSpanParser())             // saját típus
    .Register("MAC-cím", MacAddress.Parse);     // vagy egyetlen függvénnyel
```

Saját parser a `ValueParser<T>`-ből származik. A hibás bemenet **nem kivétel, hanem
visszaadott eredmény** – a felhasználó elgépelése normális esemény, nem programhiba:

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
`[Option(Parser = typeof(...))]` megadásával – így lehet két `string` paraméternek két
különböző értelmezése (pl. útvonal és rögzített névlista).

A véges értékkészletű paraméterekhez van kész `ChoiceParser`, ami már a beírás
pillanatában visszautasítja a rossz nevet, és fel is sorolja az elfogadottakat:

```csharp
public sealed class OptimizerParser() : ChoiceParser(["grid", "golden", "hybrid"]);
```

### A shell

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

A felhasználói hibák (ismeretlen parancs, rossz érték, hiányzó fájl) nem szakítják meg
a munkamenetet: a shell kiírja őket, és jöhet a következő parancs.

---

## 14. Mi van kész és mi hiányzik még

### Kész

- Interaktív shell: `help` / `show` / `set` / `reset` / `run` / `exit`, aliasokkal,
  idézőjeles értékekkel, kommentekkel, barátságos hibakezeléssel.
- Attribútum-vezérelt paraméterkezelés (Argus), automatikus súgóval.
- CSV beolvasás validációval, tizedesvessző-tűréssel, mintaösszevonással
  (`none` / `mean` / `median`).
- Log-distance path-loss modell.
- 5 célfüggvény: `mean`, `median`, `rmse`, `huber`, `composite`.
- 3 optimalizáló: `grid`, `golden`, `hybrid`.
- 5 csoportosítási stratégia + automatikus összehasonlítás.
- Opcionális `RSSI0`-eltolás keresés (`free-rssi0`).
- Zárt képletű kontrollbecslés az `n`-re (legkisebb négyzetek).
- Teljes hibastatisztika: MAE, medián, RMSE, P90, max.
- Konzolriportok + 3 CSV export (`summary`, `residuals`, `sweep`).

### Hiányzik / jövőbeli ötletek

**Validáció és megbízhatóság**

- Nincs **keresztvalidáció** (train/test szétválasztás). A program figyelmeztet a
  túlillesztésre, de nem méri meg – pedig ez adná meg az igazi választ arra, melyik
  stratégiát érdemes választani.
- Nincs **bizonytalanságbecslés** az `n`-re (pl. bootstrap konfidencia-intervallum).
- Nincs **automatikus stratégia-ajánlás** a végén.
- Nincs **egységteszt** egyetlen projektben sem.

**Modell**

- Csak egy path-loss modell van (`LogDistanceModel`), és **nem lehet beállításból
  cserélni**. Az `IPathLossModel` interfész kész, hiányzik pl. az ITU beltéri modell
  vagy a falszám-korrekció.
- Az `IPathLossModel.EstimateRssi` implementálva van, de sehol nem használjuk
  (jelenleg holt kód).
- Nincs **relatív hiba** célfüggvény (%-ban). Jelenleg minden hibát méterben mérünk,
  ezért a nagy távolságok automatikusan túlsúlyt kapnak.
- Nincs mérési **súlyozás** (pl. közeli pontok fontosabbak).

**Beállíthatóság**

- A célfüggvények belső paraméterei **be vannak drótozva**: a Huber-küszöb (10 m), a
  `composite` súlya (0.3) és kvantilise (0.90). Nem lehet a shellből állítani.
- Az optimalizálók paraméterei szintén: rácsfelbontás (5001 / 201), tolerancia.
- Az `RSSI0`-eltolás tartománya (±10 dBm) és felbontása (81 lépés) sem állítható.
- Nincs **config mentés/betöltés**: a beállítások nem élik túl a kilépést.
- A `Program.Main` megkapja a parancssori argumentumokat, de **nem használja**;
  nincs egylövetű mód (pl. `dotnet run -- run`).

**Kimenet**

- A `sweep.csv` mindig csak **egy** stratégia görbéit tartalmazza (az utoljára
  futtatottét), nem választható.
- A sweep-görbe **nem veszi figyelembe az `RSSI0`-eltolást**, ezért `free-rssi0`
  bekapcsolt állapotában a görbe minimuma nem esik egybe a táblázatban szereplő `n`-nel.
- A CSV kiírás **nem escape-eli** a vesszőt és az idézőjelet – ha egy gyártónév vesszőt
  tartalmaz, elcsúszik a kimenet.
- A kimeneti CSV mindig vesszős/pontos, nem követi a `separator` beállítást.
- Nincs futásidő-mérés, nincs naplózás.
- Nincs grafikus kimenet (a sweep-görbét kézzel kell Excelben ábrázolni).

**Használhatóság**

- Az Argus beépített parancsainak súgószövege **angol**, míg a projekt többi része
  magyar – vegyes a felület nyelve.
- Nincs parancstörténet és tab-kiegészítés a promptban.
- Nincs `version` / `about` parancs.
- A `CommandLexer` idézőjel-escape-elése `""` formájú; a `\"` nincs támogatva
  (kódban jelölt TODO).
