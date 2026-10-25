# Materialien und Böden

## 1. Worum es geht

Was eine Siedlung bauen, schmieden und anbauen kann, hängt an dem, was unter ihr liegt. Wer auf altem Vulkanland siedelt, findet Basalt für Mühlsteine, Obsidian für Klingen und einen lockeren, fruchtbaren Boden, aber kaum Zinn. Wer am Rand eines Beckens lebt, hat Ton, Kalk und Salz und vielleicht Kohle, muss Erz aber eintauschen. So entsteht der Unterschied zwischen den Gegenden nicht aus einer Liste von Zufallsfunden, sondern aus der Geschichte des Bodens: Gestein, Lagerstätten und Böden hängen zusammen, und die Pflanzen (`pflanzen.md`) wachsen auf dem, was die Böden hergeben.

Diese Datei ist die Inhaltsliste: welche Gesteinsräume es gibt, welche Gesteine sie liefern, welche Materialien wo entstehen und welche Böden sich daraus und aus dem Klima bilden. Wie daraus die Karten berechnet werden, steht in `welt_konfiguration.md` Abschnitte 8–11. Jede Landzelle des Weltrasters (2160 × 1080) bekommt für jedes Material und jeden Boden einen Wert; das gewählte Spielfeld übernimmt seine Werte aus den umliegenden Zellen (Design-Referenz Abschnitt 4). Einzelne Erzadern oder Bodenflecken liegen nicht auf der Weltkarte, sie setzt die Spielwelt beim Betreten.

Die Listen werden später zu YAML unter `data/nature/`; bis dahin ist diese Datei die Arbeitsgrundlage.

## 2. Gesteinsräume

Ein Gesteinsraum sagt, woher der Untergrund einer Gegend stammt. Er ist kein Gebiet im Spiel, sondern bestimmt nur, welche Gesteine und Lagerstätten dort zusammen auftreten.

| ID | Bedeutung | Typische Landschaft |
|---|---|---|
| `KUESTENBOGEN` | Meeresboden taucht unter einen Kontinent | lange, hohe Kette parallel zur Küste mit jungen Vulkanen, Meer auf einer Seite |
| `INSELBOGEN` | Meeresboden taucht unter Meeresboden | langgestreckte Inseln oder Inselketten mit vulkanischen Rücken |
| `KOLLISION` | zwei Kontinente stoßen zusammen | lange, sehr hohe Kette mit Land auf beiden Seiten |
| `OPHIOLITH` | aufgeschobener Meeresboden in einer Kollisionszone | schmale Streifen dunklen, grünlichen Gesteins an den Flanken von `KOLLISION` |
| `ALTGEBIRGE` | abgetragenes altes Gebirge | lange, aber niedrige und breite Rücken |
| `KRATON` | alter, stabiler Kontinentkern | weites, flaches Land fern aller Gebirge |
| `BECKEN` | Sedimentbecken vor Gebirgen und junge Küstenebenen | Tiefland neben Gebirgen, um Binnenmeere, in Senken, flache Küstenebenen an ruhigen Küsten |
| `GRABEN` | ein Kontinent reißt auf | langgestreckte Senke oder Seenkette zwischen zwei Rücken |
| `HOTSPOT` | Vulkanland über einer Aufschmelzung im Erdmantel | rundliche, einzeln stehende Erhebung oder Vulkaninsel |
| `EISSCHILD` | dauerhaft vergletschert | die Eisinsel im hohen Norden |

### 2.1 Zuordnung der Ketten und Inseln

Die Ketten und Inselgruppen stammen aus `welt_konfiguration.md` Abschnitte 3 und 4 (bestätigt 2026-10-24, T-080). Die Welt bekommt so ein zusammenhängendes Bild: Im Westen taucht Meeresboden unter den Kontinent und türmt die Westkordillere mit ihren Vulkanen auf, im Osten stoßen zwei Landmassen zusammen und heben die Zentralkette, die höchste der Welt. Mitten im Ostkontinent wölbt heißer Mantel das Hochplateau auf und reißt südlich davon den Graben auf. Um die Nordbucht des Westkontinents liegt ein uralter, flacher Kontinentkern, abgeschliffen und voller Seen.

| Kette | Lage, Höhe | Gesteinsraum |
|---|---|---|
| Westkordillere | Westküste des Westkontinents, 6400 m | `KUESTENBOGEN` |
| Ostbergland | Osten des Westkontinents, 2100 m | `ALTGEBIRGE` |
| Nordschild | um die Nordbucht des Westkontinents, 1100 m | `KRATON` (alter Kontinentkern: Bandeisen, Gold, Diamantschlote) |
| Zentralkette | Ost-West durch den Ostkontinent, 8849 m | `KOLLISION`, mit `OPHIOLITH` an den Flanken |
| Nordostkamm | nördlich der Zentralkette, 3200 m | `KOLLISION` |
| Südkette NWL | am Mittelmeer des Nordwestlands, 2600 m | `KOLLISION` |
| Westhügel Nord | Westrand des Nordwestlands, 1300 m | `ALTGEBIRGE` |
| Hochplateau | Mitte des Ostkontinents, 2600 m, sehr breit | `HOTSPOT` |
| Grabenschultern und Grabenbruch | Nord-Süd mit Grabenseen | `GRABEN` |
| Südanden | Westküste des Ostkontinents im Süden, 4300 m | `KUESTENBOGEN` |
| Südkamm | Südküste des Ostkontinents, 3000 m | `ALTGEBIRGE` |
| Ostküstenkette | Ostküste des Ostkontinents, 2300 m | `ALTGEBIRGE` |
| Inselbogen, Borneo, Neuguinea, Doppelinsel | Inselketten im Osten und Süden | `INSELBOGEN` |
| Großinsel, Südland-Osten, Arktis-West | Rücken auf Großinsel, Südland und arktischen Inseln | `ALTGEBIRGE` |
| Vulkaninsel, Hotspot-Inseln, Mittelozean-Inseln | einzelne Inseln im Meer | `HOTSPOT` |
| Eisschild | Eisinsel im hohen Norden | `EISSCHILD` über `KRATON` |

**Inselgruppen ohne eigene Kette** (Blobs aus `welt_konfiguration.md` Abschnitt 3):

| Gruppe | Gesteinsraum |
|---|---|
| Karibik-Inselbogen | `INSELBOGEN` |
| Tropen-Archipel ohne Borneo und Neuguinea: (153, 11), (156, −2), (150, −9), (160, −10), (170, 14), (173, 9) | `INSELBOGEN` |
| Inselreihe im Nordozean: (158, 46), (162, 49,5), (167, 52), (174, 53), (−171, 54), (−166, 55) | `INSELBOGEN` |
| Inseln im offenen Meer: (−17, 28), (−13,5, 29,5), (−30, 16), (−27,5, 14), (−48, 32), (−40, 2), (−155, −2) | `HOTSPOT` |
| Westinseln, Polar-Archipel, arktische Inseln Ost, subpolare Inseln, (−88, −51) | `KRATON` (Stücke des Kontinents auf dem Schelf) |

**Regeln für den Rest:**

- Erhebungen, die zu keiner Kette gehören (alte Massive aus dem Höhenmodell, etwa der diagonale Rücken im Inneren des Westkontinents), werden ab etwa 1000 m `ALTGEBIRGE`: Es sind abgetragene alte Gebirge.
- Eine Insel, die keiner Gruppe zugeordnet ist, wird `HOTSPOT`, wenn sie weit vom Festland liegt (über 500 km), sonst `KRATON`.
- Alles übrige Land, das nicht Becken wird, ist `KRATON`.

## 3. Grundgestein

Jeder Gesteinsraum liefert Gesteinsanteile in Prozent.

| Gesteinsraum | Gesteinsanteile |
|---|---|
| `KRATON` | Granit 30, Gneis 25, Sandstein 15, Kalkstein 10, Quarzit 10, Basalt 5, Tonschiefer 5 (Kalk und Sandstein als dünne Decke auf dem alten Kern) |
| `ALTGEBIRGE` | Granit 30, Tonschiefer 20, Gneis 15, Sandstein 15, Quarzit 10, Kalkstein 10 |
| `KOLLISION` | Gneis 25, Granit 20, Kalkstein 20, Tonschiefer 15, Marmor 10, Quarzit 10 |
| `OPHIOLITH` | Serpentinit 50, Basalt 35, Kalkstein 15 |
| `KUESTENBOGEN` | Andesit 35, Granit 20, Basalt 15, Tuff 15, Sandstein 10, Porphyr 5 |
| `INSELBOGEN` | Andesit 35, Basalt 30, Tuff 20, Kalkstein 15 |
| `HOTSPOT` | Basalt 70, Tuff 25, Andesit 5 |
| `GRABEN` | Basalt 40, Sandstein 20, Tonstein 25, Tuff 15 (Travertin als Vorkommen, Abschnitt 5.1) |
| `BECKEN` | Sandstein 30, Kalkstein 25, Tonstein 25, Kreide 10, Gips 10 |
| `EISSCHILD` | Gneis 50, Granit 50 (Zugänglichkeit 0) |

Klimabedingungen für Gesteine:

- Gips in `BECKEN` nur in Köppen-Klassen BW/BS oder angrenzend an Binnenmeere. Sonst wird der Anteil Kalkstein zugeschlagen.
- Kreide in allen `BECKEN` (ehemaliges Flachmeer), unabhängig vom heutigen Klima: Das Gestein ist viel älter als das Klima darüber. Kreide ist Voraussetzung für Feuerstein.
- Marmor entsteht zusätzlich in `ALTGEBIRGE` dort, wo Kalkstein an Granit grenzt (Kontaktmetamorphose), als schmaler Saum.

## 4. Lesart der Materialtabelle

Spalten: **Entstehung** nennt Gesteinsraum und Bedingung (Köppen-Klassen, Gestein, Wasser). **Verteilung** sagt, ob ein Material flächig, in Flecken, an einzelnen Fundorten, als Seife flussabwärts oder abgeleitet aus einem anderen auftritt (Verfahren in `welt_konfiguration.md` Abschnitt 10). **Seltenheit** ist die Zielklasse (Zielwerte, Design-Referenz Abschnitt 4). **Abhängig von** nennt die Layer, aus denen ein Material abgeleitet wird.

Kategorien: `GESTEIN`, `ERZ`, `BRENNSTOFF`, `SALZ`, `INDUSTRIE`, `EDELSTEIN` (je Unterabschnitt eine).

## 5. Materialtabelle

### 5.1 Gesteine

| ID | Name | Entstehung | Verteilung | Seltenheit | Abhängig von | Nutzung |
|---|---|---|---|---|---|---|
| `granit` | Granit | KRATON, ALTGEBIRGE, KOLLISION, KUESTENBOGEN | FLAECHIG | SEHR_HAEUFIG | Gestein | Baustein, Mühlsteine |
| `gneis` | Gneis | KRATON, KOLLISION, ALTGEBIRGE | FLAECHIG | HAEUFIG | Gestein | Baustein, Platten |
| `quarzit` | Quarzit | KRATON, ALTGEBIRGE | FLAECHIG | HAEUFIG | Gestein | Baustein, Steinwerkzeuge |
| `basalt` | Basalt | HOTSPOT, GRABEN, INSELBOGEN, OPHIOLITH | FLAECHIG | HAEUFIG | Gestein | Pflaster, Mühlsteine |
| `andesit` | Andesit | KUESTENBOGEN, INSELBOGEN | FLAECHIG | MITTEL | Gestein | Baustein |
| `tuff` | Tuff | KUESTENBOGEN, INSELBOGEN, HOTSPOT, GRABEN | FLAECHIG | MITTEL | Gestein | leicht bearbeitbarer Baustein, Zuschlag für wasserfesten Mörtel |
| `porphyr` | Porphyr | KUESTENBOGEN, alte Vulkangebiete | KLUMPIG | SELTEN | Gestein | Prestigestein |
| `obsidian` | Obsidian | HOTSPOT, KUESTENBOGEN, INSELBOGEN, GRABEN (aktive Vulkane) | PUNKTUELL | SELTEN | – | Klingen, Spiegel |
| `bimsstein` | Bimsstein | wie Obsidian | PUNKTUELL | SELTEN | – | Schleifmittel, Leichtbaustoff |
| `kalkstein` | Kalkstein | BECKEN, KOLLISION, INSELBOGEN | FLAECHIG | SEHR_HAEUFIG | Gestein | Baustein, Branntkalk, Mörtel, Zuschlag bei Eisenverhüttung |
| `kreide` | Kreide | BECKEN (ehemaliges Flachmeer) | FLAECHIG | MITTEL | Gestein | weicher Baustein, Kalk |
| `marmor` | Marmor | KOLLISION, Kontaktsaum Kalk/Granit in ALTGEBIRGE | KLUMPIG | MITTEL | `kalkstein`, `granit` | Prestigebauten, Skulpturen |
| `marmor_weiss` | Reiner weißer Marmor | wie Marmor, beste Stellen | PUNKTUELL | SEHR_SELTEN | `marmor` | Skulpturen höchster Qualität |
| `sandstein` | Sandstein | BECKEN, KRATON-Deckschicht, Wüstenränder | FLAECHIG | SEHR_HAEUFIG | Gestein | Baustein, Schleifsteine |
| `tonstein` | Tonstein | BECKEN, GRABEN | FLAECHIG | HAEUFIG | Gestein | Ausgangsmaterial für Ton |
| `tonschiefer` | Tonschiefer | ALTGEBIRGE, KOLLISION | FLAECHIG | MITTEL | Gestein | Dachschiefer, Schreibtafeln |
| `travertin` | Travertin | GRABEN (heiße Quellen), Karst in BECKEN | PUNKTUELL | SELTEN | – | Baustein |
| `serpentinit` | Serpentinit | OPHIOLITH | KLUMPIG | SELTEN | Gestein | Schmuckstein, Quelle für Chrom, Nickel, Platin, Jade, Speckstein |
| `speckstein` | Speckstein (Talk) | innerhalb Serpentinit | ABGELEITET | SELTEN | `serpentinit` | Schnitzerei, feuerfeste Gefäße, Gussformen |
| `alabaster` | Alabaster | dichte Gipsvorkommen | ABGELEITET | SELTEN | `gips` | Schnitzerei, Gefäße |

### 5.2 Erze

| ID | Name | Entstehung | Verteilung | Seltenheit | Abhängig von | Nutzung |
|---|---|---|---|---|---|---|
| `eisen_bandeisen` | Gebändertes Eisenerz | KRATON | KLUMPIG | MITTEL | – | Eisen in großen Mengen, aber anspruchsvoll in der Verhüttung |
| `eisen_raseneisen` | Raseneisenerz | Feuchtgebiete und Moore in Cfb, Cfc, Dfb, Dfc, Dwb, Dwc | KLUMPIG | MITTEL | Feuchtgebietsanteil | frühe, einfach zugängliche Eisenquelle |
| `eisen_laterit` | Laterit-Eisen | Af, Am, Aw auf flachem Gelände | FLAECHIG | MITTEL | Boden `laterit` | Eisen, Pigment |
| `eisen_magnetit` | Magneteisenerz | Kontaktsaum, wo Granit an Kalkstein grenzt: KOLLISION, ALTGEBIRGE, KUESTENBOGEN | KLUMPIG | MITTEL | `granit`, `kalkstein` | reiches Eisenerz, magnetisch (Kompassnadel) |
| `eisen_haematit` | Roteisenerz (Hämatit) | Gänge und Lager in Kalkstein und Sandstein: BECKEN, ALTGEBIRGE | KLUMPIG | MITTEL | `kalkstein`, `sandstein` | reiches Eisenerz, Rötel als rotes Pigment |
| `eisensand` | Eisensand (Magnetit und Ilmenit) | Seifen an Küsten und in Flüssen unterhalb von Basalt, Andesit und Granit | SEIFE | MITTEL | `basalt`, `andesit`, `granit` | Eisen ohne Bergbau, sehr guter Stahl in kleinen Öfen; der Ilmenit-Anteil ist Titanerz für späte Technik |
| `meteoreisen` | Meteoreisen | einzelne Einschläge, unabhängig vom Gesteinsraum | PUNKTUELL | EXTREM_SELTEN | – | gediegenes Eisen lange vor der Verhüttung, kalt gehämmert; Prestige, Sagenstoff |
| `kupfer_gediegen` | Gediegenes Kupfer | Hohlräume in Basaltdecken: HOTSPOT, GRABEN, seltener KRATON | KLUMPIG | SELTEN | `basalt` | erstes Metall, kalt gehämmert zu Werkzeug und Schmuck, vor jeder Verhüttung |
| `kupfer_porphyr` | Kupfer (magmatisch) | KUESTENBOGEN, INSELBOGEN | KLUMPIG | MITTEL | – | Kupfer, Bronze |
| `kupfer_sediment` | Kupfer (sedimentär) | GRABEN, BECKEN | KLUMPIG | MITTEL | – | Kupfer, Bronze |
| `kupfer_gang` | Kupfer (in Gängen) | KOLLISION, ALTGEBIRGE | KLUMPIG | SELTEN | – | Kupfer, Bronze |
| `zinn` | Zinn (primär) | KOLLISION, ALTGEBIRGE, an Granit | PUNKTUELL | SEHR_SELTEN | `granit` | Bronze; genau 4 Fundorte weltweit, einer je Festland (Entscheidung 2026-10-25) |
| `zinn_seife` | Zinnseife | flussabwärts von Zinn | SEIFE | SEHR_SELTEN | `zinn` | Bronze, leichter abbaubar als primäres Zinn |
| `wolfram` | Wolfram | gemeinsam mit Zinn | ABGELEITET | SEHR_SELTEN | `zinn` | späte Metallurgie |
| `bismut` | Bismut (Wismut) | gemeinsam mit Zinn, Wolfram und Silber, teils gediegen | ABGELEITET | SELTEN | `zinn`, `silber` | Bismutbronze, wo Zinn knapp ist; leicht schmelzende Legierungen, weißes Pigment, Kosmetik, Heilmittel |
| `gold_primaer` | Gold (gediegen, in Gängen) | KUESTENBOGEN, KRATON, ALTGEBIRGE | PUNKTUELL | SELTEN | – | Schmuck, Währung |
| `gold_seife` | Waschgold | flussabwärts von Gold | SEIFE | SELTEN | `gold_primaer` | historisch fast immer die erste Goldquelle |
| `silber` | Silber | KUESTENBOGEN, Flanken von KOLLISION, überall mit Blei (silberhaltiges Bleierz) | KLUMPIG | MITTEL | `blei` | Schmuck, Währung |
| `silber_gediegen` | Gediegenes Silber | reichste Stellen der Silbergänge, oft mit Kobalt, Nickel und Arsen | ABGELEITET | SEHR_SELTEN | `silber` | Silber ohne Verhüttung, Schmuck, Währung |
| `blei` | Blei | KUESTENBOGEN, KOLLISION-Flanken, in Kalkstein (auch Kalkstein-Becken und Kraton) | KLUMPIG | MITTEL | `kalkstein` | Rohre, Gewichte, Glasur |
| `zink` | Zink | wie Blei | ABGELEITET | MITTEL | `blei` | Messing |
| `quecksilber` | Quecksilber (Zinnober) | Vulkan- und Thermalgebiete: KUESTENBOGEN, GRABEN, HOTSPOT | PUNKTUELL | SEHR_SELTEN | – | Goldgewinnung, rotes Pigment, giftig |
| `chrom` | Chrom | in Serpentinit | ABGELEITET | SELTEN | `serpentinit` | späte Metallurgie |
| `nickel` | Nickel | Serpentinit, Laterit über Serpentinit | ABGELEITET | SELTEN | `serpentinit` | Legierungen |
| `platin` | Platin (gediegen) | Seifen flussabwärts von Serpentinit | SEIFE | EXTREM_SELTEN | `serpentinit` | Prestigemetall |
| `mangan` | Mangan | BECKEN, Laterit | KLUMPIG | MITTEL | – | Stahlveredelung, schwarzes Pigment |
| `kobalt` | Kobalt | Kupfergebiete, GRABEN | ABGELEITET | SELTEN | `kupfer_sediment`, `kupfer_porphyr` | blaues Glas und Keramik |
| `antimon` | Antimon | KOLLISION, KUESTENBOGEN | PUNKTUELL | SELTEN | – | Legierungen, Kosmetik |
| `arsen` | Arsen | mit Kupfer, Gold, Silber | ABGELEITET | SELTEN | `kupfer_porphyr`, `gold_primaer` | Arsenbronze, Gift |
| `bauxit` | Bauxit | KRATON und ALTGEBIRGE in Af, Am, Aw, flach | FLAECHIG | MITTEL | Boden `laterit` | Aluminium (sehr späte Technologie), Alaun |

### 5.3 Brennstoffe

| ID | Name | Entstehung | Verteilung | Seltenheit | Abhängig von | Nutzung |
|---|---|---|---|---|---|---|
| `steinkohle` | Steinkohle | BECKEN, Vorland von ALTGEBIRGE | KLUMPIG | MITTEL | – | Brennstoff, Verhüttung |
| `braunkohle` | Braunkohle | BECKEN, jünger und flacher | KLUMPIG | MITTEL | – | Brennstoff |
| `torf` | Torf | Moore in Cfb, Cfc, Dfb, Dfc, Dwc, ET | FLAECHIG | MITTEL | Feuchtgebietsanteil | Brennstoff, wo Holz fehlt |
| `bitumen` | Bitumen / Erdöl (Austritte) | BECKEN, besonders Vorlandbecken | PUNKTUELL | SELTEN | – | Abdichtung, Kleber, Lampenöl |
| `oelschiefer` | Ölschiefer | BECKEN | KLUMPIG | SELTEN | – | Brennstoff (später) |

### 5.4 Salze und Evaporite

| ID | Name | Entstehung | Verteilung | Seltenheit | Abhängig von | Nutzung |
|---|---|---|---|---|---|---|
| `steinsalz` | Steinsalz | alle BECKEN (Salzlager älter als das Klima, Solequellen), Salzpfannen (Boden `salzboden`) | KLUMPIG | MITTEL | – | Konservierung, Handel |
| `meersalz` | Meersalz | flache Küsten außer in kalten Klimaten (D·c, D·d, E) | FLAECHIG (Küstensaum) | MITTEL | – | Konservierung |
| `gips` | Gips | BECKEN in BW/BS, Salzseen | KLUMPIG | MITTEL | Gestein | Putz, Mörtel |
| `natron` | Natron (Soda) | Salzseen und Salzpfannen (Boden `salzboden`) in Wüsten und in GRABEN | PUNKTUELL | SELTEN | – | Glas, Seife, Konservierung |
| `salpeter` | Salpeter | sehr trockene Wüsten (BWh, BWk), Höhlen in Kalkstein | PUNKTUELL | SELTEN | – | Dünger, später Schießpulver |
| `kalisalz` | Kalisalz | tiefere Salzlager in BECKEN | ABGELEITET | SELTEN | `steinsalz` | Dünger |
| `borax` | Borax | Salzseen in BWh, BWk, GRABEN | PUNKTUELL | SEHR_SELTEN | `natron` | Flussmittel beim Löten, Glasur |

### 5.5 Industrieminerale

| ID | Name | Entstehung | Verteilung | Seltenheit | Abhängig von | Nutzung |
|---|---|---|---|---|---|---|
| `ton` | Ton und Lehm | Tiefland, Flussauen, Tonstein | FLAECHIG | SEHR_HAEUFIG | `tonstein`, Flüsse | Keramik, Ziegel, Lehmbau |
| `kaolin` | Kaolin | Granit in Af, Am, Aw, Cfa, Cwa | KLUMPIG | SELTEN | `granit` | Porzellan |
| `quarzsand` | Quarzsand | Küsten, Wüsten (BW), Flussbetten | FLAECHIG | HAEUFIG | – | Glas, Formsand |
| `feuerstein` | Feuerstein | in Kreide | ABGELEITET | MITTEL | `kreide` | Steinwerkzeuge, Feuerschlagen |
| `chert` | Hornstein | in Kalkstein | ABGELEITET | MITTEL | `kalkstein` | Steinwerkzeuge |
| `graphit` | Graphit | ALTGEBIRGE, KOLLISION (metamorph) | PUNKTUELL | SELTEN | – | Schreibstifte, Schmelztiegel |
| `glimmer` | Glimmer | Granit (Pegmatitgänge) | KLUMPIG | MITTEL | `granit` | Fensterscheiben, Isolierung |
| `schwefel` | Schwefel | aktive Vulkane, heiße Quellen | PUNKTUELL | SELTEN | – | Medizin, Färberei, später Schießpulver |
| `ocker` | Ocker und Erdfarben | Laterit, eisenreiche Böden | FLAECHIG | HAEUFIG | Boden `laterit`, `roterde` | Pigmente |
| `schmirgel` | Schmirgel (Korund) | Marmor in KOLLISION | PUNKTUELL | SELTEN | `marmor` | Schleifmittel |
| `wetzstein` | Wetzschiefer | Tonschiefer, Quarzit | ABGELEITET | MITTEL | `tonschiefer`, `quarzit` | Schleifen von Klingen |

### 5.6 Edel- und Schmucksteine

| ID | Name | Entstehung | Verteilung | Seltenheit | Abhängig von | Nutzung |
|---|---|---|---|---|---|---|
| `granat` | Granat | metamorphe Gesteine: Gneis, Tonschiefer | KLUMPIG | MITTEL | `gneis` | Schmuck, Schleifmittel |
| `achat` | Achat, Jaspis, Chalcedon | Hohlräume in Basalt | KLUMPIG | MITTEL | `basalt` | Schmuck, Siegel |
| `bergkristall` | Bergkristall | Granitgebirge, Gänge | PUNKTUELL | MITTEL | `granit` | Schmuck, Linsen |
| `topas` | Topas | Pegmatit in Granit | PUNKTUELL | SELTEN | `granit` | Schmuck |
| `turmalin` | Turmalin | Pegmatit in Granit | PUNKTUELL | SELTEN | `granit` | Schmuck |
| `malachit` | Malachit | Oxidationszone über Kupfer | ABGELEITET | SELTEN | `kupfer_porphyr`, `kupfer_sediment` | grünes Pigment, Schmuck, Kupfererz |
| `azurit` | Azurit | Oxidationszone über Kupfer | ABGELEITET | SELTEN | `kupfer_porphyr`, `kupfer_sediment` | blaues Pigment |
| `tuerkis` | Türkis | über Kupfer in BWh, BWk, BSh, BSk | ABGELEITET | SELTEN | `kupfer_porphyr` | Schmuck |
| `opal` | Opal | Sedimentbecken in BW/BS | PUNKTUELL | SEHR_SELTEN | – | Schmuck |
| `jade` | Jade (Nephrit, Jadeit) | Serpentinit (in dieser Welt nur an Kollisionszonen) | ABGELEITET | SEHR_SELTEN | `serpentinit` | Werkzeuge, Schmuck, Ritualobjekte |
| `smaragd` | Smaragd | Granitgänge in Tonschiefer | PUNKTUELL | SEHR_SELTEN | `granit`, `tonschiefer` | Schmuck |
| `rubin` | Rubin | Marmor in KOLLISION, Seifen | PUNKTUELL + SEIFE | SEHR_SELTEN | `marmor` | Schmuck |
| `saphir` | Saphir | Marmor in KOLLISION, Basalt-Seifen | PUNKTUELL + SEIFE | SEHR_SELTEN | `marmor`, `basalt` | Schmuck |
| `lapislazuli` | Lapislazuli | Marmor mit Granitkontakt | PUNKTUELL | EXTREM_SELTEN | `marmor`, `granit` | Schmuck, Ultramarin-Pigment |
| `diamant` | Diamant | alte, hoch gelegene Kratonkerne wie der Nordschild (Kimberlitschlote), Seifen | PUNKTUELL + SEIFE | EXTREM_SELTEN | – | Schmuck, Schneidwerkzeuge |
| `bernstein` | Bernstein | küstennahe BECKEN in Cf- und Df-Klimaten (anstelle ehemaliger Nadelwälder) | PUNKTUELL | SELTEN | – | Schmuck, Handel |

## 6. Böden

### 6.1 Bodentypen

Fruchtbarkeit von 1 (sehr arm) bis 5 (sehr fruchtbar). pH ist für die Pflanzenkarten wichtig.

| ID | Name | Fruchtbarkeit | pH | Eigenschaften |
|---|---|---|---|---|
| `laterit` | Laterit / Ferralsol | 1 | sauer | rot, eisen- und aluminiumreich, ausgewaschen |
| `roterde` | Tropische Roterde | 2 | sauer bis neutral | rot, mäßig |
| `vertisol` | Vertisol | 3 | neutral bis basisch | tonreich, quillt und reißt |
| `wuestenrohboden` | Wüstenrohboden | 1 | basisch | kaum Humus, steinig |
| `sandboden` | Sandboden / Dünen | 1 | variabel | durchlässig, trocken |
| `kalkkruste` | Kalkkrustenboden | 2 | basisch | harte Kalkschicht im Boden |
| `salzboden` | Salzboden | 1 | basisch | Salzkrusten, nur salztolerante Pflanzen |
| `kastanienerde` | Kastanienerde | 3 | neutral bis basisch | trockene Steppe |
| `schwarzerde` | Schwarzerde | 5 | neutral | tief, humusreich |
| `terra_rossa` | Terra rossa | 3 | neutral bis basisch | rot, flachgründig, auf Kalk |
| `rendzina` | Rendzina | 2 | basisch | flachgründig, auf Kalk |
| `braunerde` | Braunerde | 4 | schwach sauer | gemäßigte Wälder, gut ackerbaufähig |
| `rotgelberde` | Rotgelber Subtropenboden | 2 | sauer | feuchte Subtropen |
| `podsol` | Podsol | 1 | stark sauer | sandig, ausgewaschen, Nadelwald |
| `gley` | Gley | 2 | variabel | durch Grundwasser vernässt |
| `moor` | Moor | 1 | stark sauer | Torf, nass |
| `permafrost` | Permafrostboden | 1 | sauer | dauerhaft gefroren |
| `auenboden` | Auenboden | 5 | neutral | Flussablagerungen |
| `vulkanboden` | Vulkanboden / Andosol | 5 | schwach sauer | sehr fruchtbar, locker |
| `loessboden` | Lössboden | 5 | neutral bis basisch | windabgelagert, tief |
| `rohboden_fels` | Rohboden / Fels | 0 | variabel | steile Hänge, Hochgebirge |
| `eis` | Eis | 0 | – | kein Boden |

### 6.2 Zonale Zuordnung (Grundwert)

| Köppen | Hauptboden | Nebenboden |
|---|---|---|
| Af, Am | `laterit` | `roterde` |
| Aw, As | `roterde` | `vertisol`, `laterit` |
| BWh | `wuestenrohboden` | `sandboden`, `kalkkruste` |
| BWk | `wuestenrohboden` | `sandboden`, `salzboden` |
| BSh | `kastanienerde` | `kalkkruste`, `roterde`, `sandboden` |
| BSk | `kastanienerde` | `schwarzerde` am feuchteren Rand |
| Csa, Csb, Csc | `braunerde` | `terra_rossa` auf Kalk |
| Cwa | `rotgelberde` | `roterde` |
| Cwb, Cwc | `braunerde` | `rotgelberde` |
| Cfa | `braunerde`, wo dauerhaft feucht `rotgelberde` | `rotgelberde` |
| Cfb | `braunerde` | `podsol` auf Sand, `gley` |
| Cfc | `podsol` | `gley`, `moor` |
| Dfa | `schwarzerde`, wo dauerhaft feucht `braunerde` | `braunerde` |
| Dsa, Dsb, Dwa, Dwb | `braunerde` | `schwarzerde` im Übergang zu BSk |
| Dfb | `braunerde` | `podsol` |
| Dsc, Dwc, Dfc | `podsol` | `moor`, `gley` |
| Dsd, Dwd, Dfd | `podsol` | `permafrost` |
| ET | `permafrost` | `rohboden_fels`, `moor` |
| EF | `eis` | – |

Schwarzerde (Waldsteppe): Pixel der Zonen BSk, Dfa, Dfb, Dwa, Dwb, Cfa, Cfb innerhalb eines Übergangsstreifens zwischen BSk und den feuchteren D- und Cf-Zonen erhalten `schwarzerde` als Hauptboden.

### 6.3 Überschreibungen in fester Reihenfolge

Spätere Regeln überschreiben frühere. Jede Regel setzt Anteile, nicht zwingend den ganzen Pixel.

1. **Gestein:** Kalkstein oder Marmor dominant → in Cs `terra_rossa`, sonst `rendzina`. Basalt, Andesit oder Tuff dominant in jungen Vulkangebieten (Hotspot, schwächer in Insel- und Küstenbögen) → `vulkanboden`; alter Basalt anderswo → `vertisol`. Sandstein dominant in feuchten Zonen → Anteil `podsol` erhöhen.
2. **Windablagerungen:** Sandmeere in BWh → `sandboden`. Löss windabwärts großer Wüsten (BW) und am Rand ehemals vergletscherter Gebiete (Übergang ET/Dfc zu Dfb/BSk) → `loessboden`; braucht die Windrichtung (T-039), bis dahin entfällt die Löss-Regel.
3. **Gewässer:** Flussauen entlang ganzjähriger Flüsse (Breite abhängig vom Abfluss, bei großen Flüssen bis 100 km) → `auenboden`. Feuchtgebietsanteil → `gley` bzw. in kühlen Zonen `moor`. Enden von Trockenflüssen im Landesinneren und abflusslose Senken in BW/BS → `salzboden`. Entlang von Trockenflüssen → `sandboden` bzw. Kiesanteil.
4. **Küste:** flache Küsten in BW/BS → Anteil `salzboden`; Sandküsten → `sandboden`.
5. **Hangneigung:** über einem Schwellwert → `rohboden_fels`, unabhängig vom Klima; im Hochgebirge zusätzlich ein fester Anteil.
6. **Eis:** Eisschild-Maske und EF → `eis`.

## 7. Offen

- E - Ostküstenkette bleibt `ALTGEBIRGE` (2026-10-24). Die Mengen der Erze hängen nicht daran (Kalibrierung auf die Seltenheitsklasse), nur ihre Lage.
- O - Zielklassen der einzelnen Materialien sind ein erster Stand und werden nach den ersten Läufen abgestimmt.
- TODO: Löss braucht die Windrichtung (T-039).
