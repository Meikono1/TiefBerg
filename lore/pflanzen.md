# Pflanzen

## 1. Worum es geht

Was eine Gegend hergibt, entscheidet, was ihre Bewohner erfinden. Wo am Fluss Papyrus wächst, schreibt man auf Papyrus; wo es keinen gibt, auf Birkenrinde, Palmblättern oder Ton. Wo Flachs gedeiht, entsteht Leinen, in der Steppe dagegen Kleidung aus Wolle und Leder. Die Pflanzen sind deshalb keine Kulisse, sondern die Grundlage der Rezepte (Design-Referenz Abschnitt 9): Jede Landzelle außer Eis soll Pflanzen tragen, und Völker und Kulturen bauen auf dem auf, was dort wächst.

Eine Pflanze wächst, wo Klima, Boden und Wasser zu ihr passen. Klima kommt aus den Köppen-Klassen, Boden aus `materialien_und_boeden.md` (mit Fruchtbarkeit und pH), Wasser aus Bodenfeuchte, Flüssen und Küste. Wie daraus je Zelle eine Eignung berechnet wird, steht in `welt_konfiguration.md` Abschnitte 11 und 12. Die Liste wird später zu YAML (`data/nature/pflanzen.yaml`); bis dahin ist diese Datei die Arbeitsgrundlage.

## 2. Angaben je Pflanze

| Feld | Bedeutung |
|---|---|
| `id` | eindeutige ID |
| `name` | Anzeigename |
| `wuchsform` | `BAUM`, `STRAUCH`, `ZWERGSTRAUCH`, `STAUDE`, `GRAS`, `GETREIDE`, `KNOLLE`, `HUELSENFRUCHT`, `KLETTERPFLANZE`, `PALME`, `SUKKULENTE`, `MOOS`, `FLECHTE`, `WASSERPFLANZE`, `ALGE` |
| `klima` | Map Köppen-Code → Gewicht (Kernzone 1,0, Randzone 0,5) |
| `boden` | bevorzugt, toleriert, ausgeschlossen (Boden-IDs) |
| `ph` | Min und Max (`STARK_SAUER`, `SAUER`, `NEUTRAL`, `BASISCH`) |
| `wasser` | Liste aus Abschnitt 3 |
| `hoehe` | optional Min und Max in Metern |
| `salztolerant` | true oder false |
| `nutzung` | Liste aus Abschnitt 4 |
| `linie` | optional, siehe Abschnitt 5 |
| `fiktiv` | true für erfundene Pflanzen (Abschnitt 7) |

Die Tabellen in Abschnitt 6 enthalten die Kernangaben; Detailfelder wie pH oder Höhenbereich werden daraus abgeleitet. Wo eine Angabe unsicher ist, wird sie mit TODO markiert statt geraten.

Freitext in der Spalte **Standort** wird beim Überführen in YAML auf messbare Größen abgebildet (O - Vorschlag):

| Angabe | wird zu |
|---|---|
| fruchtbar, nährstoffreich | Boden mit Fruchtbarkeit ab 4 bevorzugt |
| arm | Boden mit Fruchtbarkeit 1–2 toleriert |
| sandig, Kalk, Fels, Moor | bevorzugter Boden (`sandboden`, `rendzina`/`terra_rossa`, `rohboden_fels`, `moor`) |
| sauer, basisch | pH-Bereich |
| Hang, gut drainiert | Mindest-Hangneigung bzw. kein `gley`/`moor` |
| Gebirge, Hochland | Höhenbereich |
| frostfrei | kältester Monat über 0 °C (`temperature_coldest_month`) |
| bevorzugt Inseln | Bonus auf kleinen Landmassen |
| Schatten, Unterholz | TODO: braucht das Konkurrenzmodell (`welt_konfiguration.md` Abschnitt 12) |
| Pionier auf Brandflächen | TODO: Brände gibt es in den Weltdaten nicht |
| Küstenwüste mit Nebel | TODO: Nebel gibt es in den Weltdaten nicht; bis dahin `KUESTE` in BWh |
| Brackwasser | `GEZEITEN` an Flussmündungen |

## 3. Wasserkategorien

Wie die Kategorien je Zelle bestimmt werden, steht in `welt_konfiguration.md` Abschnitt 11.

| Kategorie | Bedeutung |
|---|---|
| `TROCKEN` | kein Grundwasser, nur Niederschlag |
| `FRISCH` | normale Bodenfeuchte |
| `FEUCHT` | dauerhaft feucht, hohe Niederschläge |
| `NASS` | Sumpf, Moor, Feuchtgebietsanteil hoch |
| `UFER` | entlang ganzjähriger Flüsse und Seen |
| `OASE` | Grundwasser in Trockengebieten, entlang von Trockenflüssen |
| `KUESTE` | Küstensaum |
| `GEZEITEN` | Gezeitenzone an flachen Küsten |
| `STEHEND` | in Seen und Teichen |

## 4. Nutzungscodes

| Code | Bedeutung |
|---|---|
| N | Nahrung (inkl. Zucker, Getränke) |
| Fu | Futter für Tiere |
| F | Faser (Seile, Stoffe, Matten, Füllung) |
| P | Papier oder Schreibmaterial |
| H | Holz, Bau- und Werkholz |
| B | Brennmaterial |
| O | Öl oder Fett |
| W | Harz, Latex, Wachs, Lack, Gummi, Gerbstoff |
| G | Gewürz oder Aroma |
| Fa | Farbstoff |
| S | Stimulans oder Rauschmittel |
| M | Heilmittel |
| T | Gift |
| I | Industriell (z. B. Soda aus Pflanzenasche) |

## 5. Linien je Landmasse

Auf getrennten Landmassen bringen gleiche Klimate ähnliche Pflanzenformen hervor, die aus ganz verschiedenen Familien stammen. Ein Säulenkaktus auf dem einen Kontinent und eine Kandelaber-Wolfsmilch auf dem anderen sehen fast gleich aus, sind aber nicht verwandt, und die Wolfsmilch führt giftigen Milchsaft. Auf einer abgelegenen Landmasse kann eine einzige Baumfamilie fast alle Waldnischen besetzen, und eine Inselgruppe hat statt Flachs ihre eigene Faserlilie. Wer von einem Kontinent auf den anderen reist, findet also Vertrautes, das sich anders verhält.

Pflanzen mit `linie: true` bekommen deshalb je Landmasse eine eigene Variante, z. B. `saeulenkaktus_west` und `saeulenkaktus_ost`. Die Varianten unterscheiden sich leicht (essbar oder giftig, Faserqualität, Farbe) und können eigene Namen bekommen. Für diese Welt bieten sich an: Westkontinent, Ostkontinent, Nordwestland, Eisinsel, Inselketten im Osten, Südland.

## 6. Pflanzenliste

### 6.1 Tropischer Regenwald (Af, Am)

| ID | Name | Wuchsform | Klima | Standort | Nutzung |
|---|---|---|---|---|---|
| `banane` | Banane / Kochbanane | STAUDE | Af, Am | FRISCH bis FEUCHT, fruchtbar | N, F |
| `taro` | Taro | KNOLLE | Af, Am, Cfa | NASS, UFER | N |
| `yams` | Yams | KNOLLE | Af, Am, Aw | FRISCH | N, M |
| `maniok` | Maniok | KNOLLE | Am, Aw | FRISCH, auch arme Böden (Laterit) | N, T (roh giftig) |
| `brotfrucht` | Brotfruchtbaum | BAUM | Af, Am | FEUCHT | N, H, F (Bast) |
| `kokospalme` | Kokospalme | PALME | Af, Am, Aw | KUESTE, sandig, salztolerant | N, O, F, H |
| `sagopalme` | Sagopalme | PALME | Af | NASS | N |
| `oelpalme` | Ölpalme | PALME | Af, Am | FEUCHT | O |
| `kakao` | Kakao | BAUM | Af | FEUCHT, Schatten | N, S |
| `pfeffer` | Pfeffer | KLETTERPFLANZE | Af, Am | FEUCHT | G |
| `nelke` | Gewürznelke | BAUM | Af | FEUCHT, Vulkanboden bevorzugt | G, M |
| `muskat` | Muskatnuss | BAUM | Af | FEUCHT, Vulkanboden, bevorzugt Inseln | G |
| `zimt` | Zimt | BAUM | Af, Am | FEUCHT | G |
| `kardamom` | Kardamom | STAUDE | Af, Am | FEUCHT, Schatten, Hang | G |
| `ingwer` | Ingwer | STAUDE | Af, Am, Aw | FRISCH | G, M |
| `kurkuma` | Kurkuma | STAUDE | Am, Aw | FRISCH | G, Fa, M |
| `vanille` | Vanille | KLETTERPFLANZE | Af | FEUCHT, Schatten | G |
| `zuckerrohr` | Zuckerrohr | GRAS | Af, Am, Aw, Cwa | FEUCHT oder UFER | N |
| `kautschuk` | Kautschukbaum | BAUM | Af | FEUCHT | W |
| `kapok` | Kapokbaum | BAUM | Af, Am, Aw | FRISCH | F (Füllung), O |
| `abaka` | Abaka (Manilahanf) | STAUDE | Af | FEUCHT, Vulkanboden | F |
| `rattan` | Rattan | PALME | Af | FEUCHT, Unterholz | F, H (Möbel) |
| `bambus` | Bambus | GRAS | Af, Am, Cfa, Cwa | FEUCHT | H, F, P, N (Sprossen) |
| `teak` | Teak | BAUM | Am, Aw | FRISCH, gut drainiert | H |
| `mahagoni` | Mahagoni | BAUM | Af, Am | FEUCHT | H |
| `ebenholz` | Ebenholz | BAUM | Af, Am | FEUCHT | H |
| `durian` | Durian | BAUM | Af | FEUCHT | N |
| `mangostan` | Mangostan | BAUM | Af | FEUCHT | N |
| `jackfrucht` | Jackfrucht | BAUM | Af, Am | FEUCHT | N, H |
| `ananas` | Ananas | STAUDE | Am, Aw | sandig, sauer toleriert | N, F |
| `papaya` | Papaya | BAUM | Af, Am, Aw | FRISCH | N, M |
| `kolanuss` | Kolanuss | BAUM | Af | FEUCHT | S |
| `guarana` | Guaraná | KLETTERPFLANZE | Af | FEUCHT | S |
| `betel` | Betelnusspalme und Betelpfeffer | PALME | Af, Am | FEUCHT | S |
| `kava` | Kava | STRAUCH | Af | FEUCHT, bevorzugt Inseln | S, M |
| `raffia` | Raffiapalme | PALME | Af, Am | NASS, UFER | F |
| `annatto` | Annatto (Orleansstrauch) | STRAUCH | Am, Aw | FRISCH | Fa, G |
| `pfeilgiftliane` | Pfeilgiftliane (Strychnos-Typ) | KLETTERPFLANZE | Af, Am | FEUCHT | T, M |

### 6.2 Savanne (Aw, As)

| ID | Name | Wuchsform | Klima | Standort | Nutzung |
|---|---|---|---|---|---|
| `sorghum` | Sorghum | GETREIDE | Aw, As, BSh | TROCKEN toleriert | N, Fu |
| `perlhirse` | Perlhirse | GETREIDE | Aw, BSh | sandig, TROCKEN | N |
| `fingerhirse` | Fingerhirse | GETREIDE | Aw, Cwb | FRISCH | N |
| `afrikanischer_reis` | Afrikanischer Reis | GETREIDE | Aw | UFER, Überschwemmungsflächen | N |
| `baumwolle` | Baumwolle | STRAUCH | Aw, Cfa, BSh (nur OASE/UFER) | gut drainiert | F, O |
| `kenaf` | Kenaf | STAUDE | Aw | FRISCH | F, P |
| `baobab` | Baobab | BAUM | Aw, BSh | TROCKEN, speichert Wasser im Stamm | N, F (Bast), M |
| `akazie` | Gummi-Akazie | BAUM | Aw, BSh, BWh (OASE) | TROCKEN | W (Gummi), Fu, H |
| `shea` | Sheabutterbaum | BAUM | Aw | FRISCH | O |
| `erdnuss` | Erdnuss | HUELSENFRUCHT | Aw, Cfa | sandig | N, O |
| `augenbohne` | Augenbohne | HUELSENFRUCHT | Aw, BSh | TROCKEN | N, Fu |
| `sesam` | Sesam | STAUDE | Aw, Cwa, BSh | FRISCH, warm | O, N |
| `rizinus` | Rizinus | STRAUCH | Aw, BSh | TROCKEN toleriert | O, T, M |
| `tamarinde` | Tamarinde | BAUM | Aw | FRISCH | N, G |
| `palmyra` | Palmyrapalme | PALME | Aw, As | TROCKEN toleriert | N, P (Palmblattmanuskripte), F |
| `cashew` | Cashew | BAUM | Aw | sandig, arm | N, O |
| `mango` | Mango | BAUM | Aw, Am | FRISCH | N, H |
| `indigo` | Indigostrauch | STRAUCH | Aw, Cwa | FRISCH | Fa (blau) |
| `sisal` | Sisal | SUKKULENTE | As, BSh | TROCKEN | F |
| `mais` | Mais | GETREIDE | Aw, Cfa, Cwa, Dfa, BSh (nur UFER) | fruchtbar | N, Fu |
| `elefantengras` | Elefantengras | GRAS | Aw, Am | FRISCH | Fu, H (Dach) |

### 6.3 Heiße Trockengebiete (BWh, BSh)

| ID | Name | Wuchsform | Klima | Standort | Nutzung |
|---|---|---|---|---|---|
| `dattelpalme` | Dattelpalme | PALME | BWh, BSh | nur OASE, salztolerant | N, F, H |
| `agave` | Agave | SUKKULENTE | BSh, BWh, Csa | TROCKEN | F, N (Saft), S (vergoren) |
| `opuntie` | Opuntie (Feigenkaktus) | SUKKULENTE | BSh, BWh, Csa | TROCKEN | N, Fu; Wirt der Cochenille-Laus → Fa (rot) |
| `saeulenkaktus` | Säulenkaktus / Kandelaber-Euphorbie (`linie: true`) | SUKKULENTE | BWh, BSh | TROCKEN | N oder T je nach Linie |
| `jojoba` | Jojoba | STRAUCH | BWh, BSh | TROCKEN | W (Wachs), O |
| `kreosotbusch` | Kreosotbusch | STRAUCH | BWh | TROCKEN | M |
| `weihrauch` | Weihrauchbaum | BAUM | BWh, BSh | TROCKEN, Fels, Kalk | W, M |
| `myrrhe` | Myrrhe | STRAUCH | BWh, BSh | TROCKEN | W, M |
| `halfagras` | Halfagras / Esparto | GRAS | BSh, BSk, Csa | TROCKEN, Kalk | F, P |
| `henna` | Hennastrauch | STRAUCH | BWh, BSh, Aw | TROCKEN bis OASE | Fa |
| `mesquite` | Mesquite | BAUM | BSh, BWh | tiefe Wurzeln, OASE | N (Hülsen), H, Fu |
| `aloe` | Aloe | SUKKULENTE | BSh, BWh, Aw | TROCKEN | M |
| `tamariske` | Tamariske | STRAUCH | BWh, BSh, BWk | OASE, salztolerant | H, B, Fu |
| `gerste` | Gerste | GETREIDE | BSh, BSk, Cs, Dfb, Cwb | TROCKEN toleriert, salztolerant | N, Fu, S (Bier) |
| `kichererbse` | Kichererbse | HUELSENFRUCHT | Csa, BSh | TROCKEN toleriert | N |
| `oleander` | Oleander | STRAUCH | Cs, BSh | OASE, Trockenflüsse | T |
| `welwitschia` | Welwitschia | SUKKULENTE | BWh (Küstenwüste mit Nebel) | KUESTE, TROCKEN | – (Kuriosität) |

### 6.4 Kalte Trockengebiete (BWk, BSk)

| ID | Name | Wuchsform | Klima | Standort | Nutzung |
|---|---|---|---|---|---|
| `hanf` | Hanf | STAUDE | BSk, Dfa, Dfb, Cfa, Cs | fruchtbar | F, P, O, S, M |
| `kendyr` | Kendyr (Apocynum) | STAUDE | BSk, BWk | UFER, salztolerant | F, M |
| `federgras` | Federgras | GRAS | BSk | Schwarzerde, Kastanienerde | Fu |
| `beifuss` | Wermut / Beifuß | ZWERGSTRAUCH | BSk, BWk | TROCKEN | M, G, S |
| `saxaul` | Saxaul | STRAUCH | BWk | sandig, salztolerant | B, H |
| `ephedra` | Meerträubel (Ephedra) | STRAUCH | BWk, BSk, ET (Gebirge) | TROCKEN | M, S |
| `sanddorn` | Sanddorn | STRAUCH | BSk, Dfb, Gebirge | sandig | N, M |
| `wildapfel` | Wildapfel | BAUM | BSk (Gebirgsränder), Dfa | FRISCH | N |
| `pistazie` | Pistazie | BAUM | BSk, Csa | TROCKEN | N, W (Harz) |
| `mandel` | Mandel | BAUM | BSk, Cs | TROCKEN toleriert | N, O |
| `luzerne` | Luzerne | HUELSENFRUCHT | BSk, Cs, Dfa | FRISCH | Fu |
| `rhabarber` | Medizinalrhabarber | STAUDE | BSk (Gebirge), Dwb | FRISCH | M |
| `weizen` | Weizen | GETREIDE | BSk, Cs, Cfb, Dfa | Schwarzerde, Löss bevorzugt | N |
| `safran` | Safran | KNOLLE | BSk, Csa | TROCKEN, gut drainiert | G, Fa, M |
| `melone` | Zuckermelone | KLETTERPFLANZE | BSk, BSh | UFER oder OASE | N |

### 6.5 Mediterran (Csa, Csb, Csc)

| ID | Name | Wuchsform | Klima | Standort | Nutzung |
|---|---|---|---|---|---|
| `oelbaum` | Ölbaum | BAUM | Csa, Csb | TROCKEN, Kalk | O, N, H |
| `weinrebe` | Weinrebe | KLETTERPFLANZE | Csa, Csb, Cfa, Cfb (warm) | Terra rossa, Hang | N, S |
| `feige` | Feige | BAUM | Csa, BSh | TROCKEN toleriert | N |
| `granatapfel` | Granatapfel | STRAUCH | Csa, BSk | TROCKEN toleriert | N, Fa |
| `johannisbrot` | Johannisbrotbaum | BAUM | Csa | Kalk | N, Fu |
| `korkeiche` | Korkeiche | BAUM | Csa, Csb | sauer, sandig | H, Kork |
| `lorbeer` | Lorbeer | BAUM | Csa, Cfa | FRISCH | G |
| `rosmarin_thymian` | Rosmarin / Thymian | ZWERGSTRAUCH | Csa, Csb | Kalk, Fels | G, M |
| `lavendel` | Lavendel | ZWERGSTRAUCH | Csa, Csb | Kalk | M, O (ätherisch) |
| `flachs` | Flachs (Lein) | STAUDE | Cs, Cfb, Dfb | FRISCH, fruchtbar | F, P, O |
| `zwergpalme` | Zwergpalme | PALME | Csa | TROCKEN | F |
| `linse` | Linse | HUELSENFRUCHT | Cs, BSk | TROCKEN toleriert | N |
| `zypresse` | Zypresse | BAUM | Csa, Csb | TROCKEN toleriert | H |
| `pinie` | Pinie | BAUM | Csa | sandig | N, H, W |
| `kaper` | Kaper | STRAUCH | Csa, BSh | Fels | G |
| `mastix` | Mastixstrauch | STRAUCH | Csa | TROCKEN | W |
| `krapp` | Krapp | STAUDE | Cs, Cfb, BSk | FRISCH | Fa (rot) |
| `kermeseiche` | Kermeseiche | STRAUCH | Csa | TROCKEN | Fa (rot, über Schildlaus) |

### 6.6 Feuchte und wintertrockene Subtropen (Cfa, Cwa, Cwb)

| ID | Name | Wuchsform | Klima | Standort | Nutzung |
|---|---|---|---|---|---|
| `reis` | Reis | GETREIDE | Cwa, Cfa, Am, Aw | NASS oder UFER, Auenboden | N, P (Stroh), S (Reiswein) |
| `tee` | Teestrauch | STRAUCH | Cfa, Cwa, Cwb | sauer, Hang | S, M |
| `maulbeere` | Weißer Maulbeerbaum | BAUM | Cfa, Cwa, Csa, Dfa | FRISCH | Fu (Seidenraupe → F), N |
| `papiermaulbeere` | Papiermaulbeere (Kozo) | STRAUCH | Cfa, Cwa, Af, Am | FRISCH | P, F (Rindenbaststoff) |
| `mitsumata` | Mitsumata | STRAUCH | Cfa, Cwb | Schatten | P |
| `ramie` | Ramie | STAUDE | Cfa, Cwa | FEUCHT | F |
| `jute` | Jute | STAUDE | Cwa, Am | UFER, Überschwemmungsflächen | F |
| `zitrus` | Zitrusfrüchte | BAUM | Cfa, Cwa, Csa | frostfrei | N, M |
| `pfirsich` | Pfirsich / Aprikose | BAUM | Cfa, Cwa, Dfa, BSk | FRISCH | N |
| `litschi` | Litschi | BAUM | Cwa | FRISCH | N |
| `soja` | Sojabohne | HUELSENFRUCHT | Cfa, Cwa, Dfa, Dwa | FRISCH | N, O |
| `kampfer` | Kampferbaum | BAUM | Cfa | FEUCHT | M, W, H |
| `lackbaum` | Lackbaum | BAUM | Cfa, Cwa, Dwa | FRISCH | W (Lack), T |
| `tungbaum` | Tungbaum | BAUM | Cfa, Cwa | FRISCH | O (Firnis, Holzschutz) |
| `ginkgo` | Ginkgo | BAUM | Cfa, Dfa | FRISCH | N, M |
| `kudzu` | Kudzu | KLETTERPFLANZE | Cfa | FRISCH | F, N, M |
| `suesskartoffel` | Süßkartoffel | KNOLLE | Cfa, Cwa, Aw, Am | sandig | N |
| `tabak` | Tabak | STAUDE | Cfa, Cwa, Aw | fruchtbar | S, T |
| `sumpfzypresse` | Sumpfzypresse | BAUM | Cfa | NASS | H |
| `pekannuss` | Pekannuss | BAUM | Cfa, Dfa | UFER, Auenboden | N, H |
| `papierrinde` | Papierrindenbaum (Melaleuca) | BAUM | Cfa, Aw, Am | NASS, UFER | P (Rindenbögen), M, O |
| `eukalyptus` | Eukalyptus (`linie: true`) | BAUM | Cfa, Cfb, Aw, BSh | arm, TROCKEN toleriert | H, M, O |

### 6.7 Ozeanisch gemäßigt (Cfb, Cfc)

| ID | Name | Wuchsform | Klima | Standort | Nutzung |
|---|---|---|---|---|---|
| `eiche` | Eiche | BAUM | Cfb, Cfa, Dfa, Dfb, Cs | Braunerde | H, Fu (Eicheln), W (Gerbstoff) |
| `buche` | Buche | BAUM | Cfb, Dfb | Kalk, Braunerde | H, Fu |
| `esche` | Esche | BAUM | Cfb, Dfb | FRISCH | H (Werkzeugstiele, Speere) |
| `ulme` | Ulme | BAUM | Cfb, Dfa | FRISCH | H, F (Bast) |
| `eibe` | Eibe | BAUM | Cfb, Csb | Kalk | H (Bögen), T, M |
| `hasel` | Hasel | STRAUCH | Cfb, Dfb | FRISCH | N, H (Flechtwerk) |
| `brennnessel` | Brennnessel | STAUDE | Cfb, Dfb, Cfa | Auenboden, nährstoffreich | F, N, Fa, M |
| `faerberwaid` | Färberwaid | STAUDE | Cfb, Dfb | FRISCH | Fa (blau) |
| `faerberwau` | Färberwau | STAUDE | Cfb, Cs | Kalk | Fa (gelb) |
| `hafer` | Hafer | GETREIDE | Cfb, Dfb | sauer toleriert | N, Fu |
| `roggen` | Roggen | GETREIDE | Cfb, Dfb, Dfc (Rand) | sandig, sauer | N |
| `kohl_ruebe` | Kohl / Rübe | STAUDE | Cfb, Dfb | FRISCH | N, Fu |
| `erbse_ackerbohne` | Erbse / Ackerbohne | HUELSENFRUCHT | Cfb, Dfb, Cs | FRISCH | N, Fu |
| `apfel_birne` | Apfel / Birne | BAUM | Cfb, Dfb, Dfa | FRISCH | N |
| `hopfen` | Hopfen | KLETTERPFLANZE | Cfb, Dfb | UFER | M, S (Bier) |
| `heidekraut` | Heidekraut | ZWERGSTRAUCH | Cfb, Cfc | Podsol, sauer | Fu, Fa, B |
| `ginster` | Ginster / Stechginster | STRAUCH | Cfb, Csb | sauer, sandig | Fa (gelb), Fu, B |
| `fingerhut` | Fingerhut | STAUDE | Cfb | sauer | M, T |
| `riesenlebensbaum` | Riesenlebensbaum | BAUM | Cfb, Dfb (küstennah) | FEUCHT | H, F (Rinde) |
| `faserlilie` | Faserlilie (Phormium-Typ, `linie: true`) | STAUDE | Cfb, Cfc | FEUCHT bis NASS | F |

### 6.8 Kontinental gemäßigt (Dfa, Dfb, Dwa, Dwb, Dsa, Dsb)

| ID | Name | Wuchsform | Klima | Standort | Nutzung |
|---|---|---|---|---|---|
| `kiefer` | Kiefer | BAUM | Dfb, Dfc, Cfb, Csa | sandig, Podsol | H, W (Harz, Teer, Pech) |
| `fichte` | Fichte | BAUM | Dfb, Dfc, Dwb | sauer | H, W |
| `birke` | Birke | BAUM | Dfb, Dfc, Cfb, Dwb | sauer, Pionierpflanze | P (Rinde), H, N (Saft), W (Birkenteer) |
| `linde` | Linde | BAUM | Dfb, Cfb, Dfa | Braunerde | F (Bast), H (Schnitzholz), M |
| `zuckerahorn` | Zuckerahorn | BAUM | Dfa, Dfb | FRISCH | N (Sirup), H |
| `walnuss` | Walnuss | BAUM | Dfa, Cfb, Csa, BSk (Gebirge) | fruchtbar | N, O, Fa, H |
| `buchweizen` | Buchweizen | STAUDE | Dfb, Dwb | arm, sauer | N |
| `rispenhirse` | Rispenhirse | GETREIDE | Dfa, Dwa, BSk | TROCKEN toleriert | N |
| `sonnenblume` | Sonnenblume | STAUDE | Dfa, BSk, Cfa | Schwarzerde | O, N |
| `heidelbeere` | Heidel- und Preiselbeere | ZWERGSTRAUCH | Dfb, Dfc | Podsol | N, Fa |
| `wildreis` | Wildreis | GRAS | Dfb, Dfa | STEHEND, UFER | N |
| `ginseng` | Ginseng | STAUDE | Dwa, Dwb | Schatten | M |
| `schwarzerle` | Schwarzerle | BAUM | Dfb, Cfb | NASS, UFER | H (wasserfest) |

### 6.9 Subarktisch (Dfc, Dfd, Dwc, Dwd, Dsc, Dsd)

| ID | Name | Wuchsform | Klima | Standort | Nutzung |
|---|---|---|---|---|---|
| `laerche` | Lärche | BAUM | Dfc, Dfd, Dwc, Dwd | Permafrost toleriert | H |
| `tanne` | Tanne | BAUM | Dfc, Dfb | sauer | H, W |
| `zwergbirke` | Zwergbirke | STRAUCH | Dfc, ET | sauer | B |
| `weide` | Weide | STRAUCH | Dfc, Dfb, Cfb, ET (Zwergweide) | UFER | F (Flechtruten), M (Rinde) |
| `moltebeere` | Moltebeere | STAUDE | Dfc, ET | Moor | N |
| `kraehenbeere` | Krähenbeere | ZWERGSTRAUCH | Dfc, ET | sauer | N |
| `weidenroeschen` | Schmalblättriges Weidenröschen | STAUDE | Dfc, Dfb | Pionier auf Brandflächen | N, M, F (Samenwolle) |
| `torfmoos` | Torfmoos | MOOS | Dfc, Dfb, Cfb, Cfc | NASS, Moor | M (Wundverband), Isolierung, Torfbildung |
| `rentierflechte` | Rentierflechte | FLECHTE | Dfc, ET | TROCKEN bis FRISCH | Fu, N (Notnahrung) |

### 6.10 Tundra und Hochgebirge (ET)

| ID | Name | Wuchsform | Klima | Standort | Nutzung |
|---|---|---|---|---|---|
| `wollgras` | Wollgras | GRAS | ET, Dfc | NASS, Moor | F (Füllung, Dochte) |
| `polsterpflanzen` | Polsterpflanzen | STAUDE | ET | Fels | Isolierung |
| `steinbrech` | Steinbrech | STAUDE | ET | Fels | M |
| `silberwurz` | Silberwurz | ZWERGSTRAUCH | ET | Kalk | Fu |
| `rosenwurz` | Rosenwurz (Rhodiola) | STAUDE | ET, Dfc | Fels | M |
| `islaendisch_moos` | Isländisches Moos | FLECHTE | ET, Dfc | sauer | N, M |
| `edelweiss` | Edelweiß | STAUDE | ET (Hochgebirge) | Kalk | M |

### 6.11 Tropisches und subtropisches Hochland (Cwb, Cwc, Hanglagen in Af/Am)

| ID | Name | Wuchsform | Klima | Standort | Nutzung |
|---|---|---|---|---|---|
| `kartoffel` | Kartoffel | KNOLLE | Cwb, Cwc, Cfb, Dfb | kühl, sauer toleriert | N, T (grüne Teile) |
| `oca` | Oca | KNOLLE | Cwb, Cwc | FRISCH | N |
| `ulluco` | Ulluco | KNOLLE | Cwb, Cwc | FRISCH | N |
| `mashua` | Mashua | KNOLLE | Cwb, Cwc | FRISCH | N, M |
| `quinoa` | Quinoa | GETREIDE | Cwb, Cwc, BSk (Hochland) | salztolerant | N |
| `amarant` | Amarant | GETREIDE | Cwb, Cwa, BSh | FRISCH | N, Fa |
| `koka` | Koka | STRAUCH | Cwb, Am (Hang) | Hang | S, M |
| `chinarinde` | Chinarindenbaum | BAUM | Cwb, Am (Hang) | FEUCHT | M (Fiebermittel) |
| `kaffee` | Kaffee | STRAUCH | Cwb, Am (Hang) | Vulkanboden, Schatten | S |
| `teff` | Teff | GETREIDE | Cwb | Vertisol toleriert | N |
| `ensete` | Ensete (Falsche Banane) | STAUDE | Cwb | FRISCH | N, F |
| `kath` | Kath | STRAUCH | Cwb | FRISCH | S |
| `seidelbast` | Seidelbast (Lokta) | STRAUCH | Cwb, Dsb, Dwb (Gebirge) | Hang, Schatten | P |
| `himalaya_nessel` | Himalaya-Nessel | STAUDE | Cwb, Dwb | FEUCHT | F |
| `rhododendron` | Rhododendron | STRAUCH | Cwb, Cfb, Dwb | sauer | T (giftiger Honig) |

### 6.12 Feuchtgebiete und Ufer (azonal)

| ID | Name | Wuchsform | Klima | Standort | Nutzung |
|---|---|---|---|---|---|
| `papyrus` | Papyrus | GRAS | Aw, Af, BWh, BSh | NASS, UFER, warm | P, F, H (Boote) |
| `schilf` | Schilfrohr | GRAS | alle außer EF und ET | UFER, NASS | H (Dach), F, P |
| `rohrkolben` | Rohrkolben | STAUDE | A, C, D | NASS | N (Wurzel), F (Matten, Füllung) |
| `binse` | Binse / Totora | STAUDE | A, C, D, Hochland | UFER | F (Matten, Boote) |
| `lotus` | Lotus | WASSERPFLANZE | Af, Am, Aw, Cfa, Cwa | STEHEND | N, M |
| `wassernuss` | Wassernuss | WASSERPFLANZE | Cfa, Cfb, Dfa | STEHEND | N |
| `mangrove` | Mangrove | BAUM | Af, Am, Aw | GEZEITEN, salztolerant | H, W (Gerbstoff), Fu |
| `nipapalme` | Nipapalme | PALME | Af, Am | Brackwasser, GEZEITEN | F (Dach), N (Zucker) |
| `pappel` | Pappel / Euphrat-Pappel | BAUM | BSk, BWk, Dfa, Cs | UFER, OASE | H |

### 6.13 Salzböden und Küste (azonal)

| ID | Name | Wuchsform | Klima | Standort | Nutzung |
|---|---|---|---|---|---|
| `queller` | Queller | STAUDE | Küsten in B, C, D | Salzboden, GEZEITEN | N, I (Asche liefert Soda für Glas und Seife) |
| `salzkraut` | Salzkraut (Salsola) | STAUDE | BWk, BSk, BWh | Salzboden | Fu, I (Soda) |
| `strandhafer` | Strandhafer | GRAS | Cfb, Dfb (Küste) | KUESTE, Dünen | F, Dünenbefestigung |
| `seetang` | Seetang | ALGE | kühle Küsten (C, D) | KUESTE | N, Dünger, I (Soda, Jod) |

### 6.14 Trockenflüsse und Oasen

Keine eigenen Pflanzen, sondern eine Standortkombination. Entlang episodischer Flüsse und an Oasen in BW/BS erhalten folgende Pflanzen einen Bonus: `dattelpalme`, `tamariske`, `akazie`, `mesquite`, `pappel`, `oleander`, `henna`, `melone`, `baumwolle`, `gerste`.

## 7. Fiktive, plausible Ergänzungen (optional)

Diese Pflanzen existieren auf der Erde nicht, treiben aber reale Eigenschaften weiter. Sie sind mit `fiktiv: true` markiert und können einzeln aktiviert werden.

| ID | Name | Wuchsform | Klima | Standort | Nutzung | Reale Grundlage |
|---|---|---|---|---|---|---|
| `kieselgras` | Kieselgras | GRAS | BSh, BWh | TROCKEN | F (extrem haltbare Seile) | Schachtelhalme lagern Kieselsäure ein |
| `filzpolster` | Filzpolster | STAUDE | ET | Fels | F (filzartige Matten), Isolierung | Polsterpflanzen, Wollgras |
| `bogenrinde` | Bogenrindenbaum | BAUM | Cfb, Dfb | FRISCH | P (großformatige Rindenbögen) | Birke, Papierrindenbaum |
| `frostzucker` | Frostzuckerstrauch | STRAUCH | Dfc, ET | TROCKEN bis FRISCH | N (süßer Saft mit Frostschutz) | Frostschutzzucker in Pflanzen, Ahornsaft |
| `latexrinde` | Plattenlatexbaum | BAUM | Af | FEUCHT | W, P (Latex härtet zu Platten aus) | Kautschukbaum |

## 8. Offen

- O - Abweichende Eigenschaften und Namen der Linien-Varianten (Abschnitt 5).
- O - Welche fiktiven Pflanzen aktiv sind (Abschnitt 7).
- O - Namen mit Bezug auf Orte der Erde brauchen eigene Namen: Afrikanischer Reis, Abaka (Manilahanf), Himalaya-Nessel, Isländisches Moos, Euphrat-Pappel.
- O - Abbildung der Standort-Angaben (Abschnitt 2) bestätigen.
- O - Die Liste ist noch nicht bestätigt (T-050); Tiere folgen in `Fauna.md`.
