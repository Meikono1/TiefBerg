# Zivilisations-RPG – Design- & Architekturreferenz (KI-optimiert)

Oct 12, 2026 · @Kono

## 0. Regeln für KI-Assistenten

Verbindliche Referenz für Design und Architektur. Bei Widerspruch zwischen Code und Dokument gilt das Dokument.

**Status:** `E -` entschieden, umsetzen. `O -` offen, `// TODO:` mit Verweis setzen. `O - Vorschlag:` darf umgesetzt werden, im Code als Annahme markieren.

**Code (E):**

- C# in Godot. Entwickler hat Java-Hintergrund.
- Code-Kommentare auf Deutsch, Spieltexte auf Englisch (Abschnitt 12).
- Dateien: Soft-Limit 500, Hard-Limit 1000 Zeilen.
- `// TODO:` statt halb durchdachter Methoden.
- Logging in jeder Klasse (Abschnitt 12).
- `using` statt vollqualifizierter Namen.

**Antworten (E):**

- Nur Änderungen, keine kompletten Dateien. Alte Zeile → neue Zeile, mit Zeilenangabe.
- Alte Zeile exakt kopierbar, damit sie per Strg+C und Suche gefunden wird.
- Neue Dateien vorher ankündigen.

## 1. Vision

2D-Zivilisationssimulator mit RPG-Elementen als Textadventure mit GUI, Solo-Entwicklung. Der Spieler ist eine gottartige Seele, die in Personen einer lebendigen Welt eintritt.

- E - Die Seele tritt in Personen ein. Über Generationen baut der Spieler mehrere Charaktere auf und wechselt zwischen ihnen. Sie müssen nicht verwandt sein.
- E - Charaktere ziehen als Party, eine Gruppe von Abenteurern, in Dungeons (Abschnitt 7).
- E - Eigener Stammbaum als Kernfeature (Abschnitt 6).
- E - Spielercharaktere sind besonders, weil der Spieler ihre Geschichte erlebt: weniger Krankheit, keine zufälligen permanenten Verletzungen. Sonst gelten dieselben Regeln wie für NPCs.
- E - Jede Kulturgruppe und Spezies hat eigene Königreiche und Dörfer, die passiv weiterlaufen und technologisch unterschiedlich weit sein können.
- E - Technologie ist offen. Eisen, Kupfer oder Schwarzpulver sind ab Start möglich, wenn die Voraussetzungen erfüllt sind.
- E - Reiche erreichen 100.000 Einwohner und mehr.
- E - Szenarien sind verschiedene Einstiege in dieselbe Welt (Abschnitt 11).
- E - Die Götter spielen mit der Welt. Für sie ist sie ein Spielplatz, auf dem sie spannende Geschichten erleben wollen, ohne den Planeten zu zerstören. Die Seele des Spielers ist eine solche göttliche Seele, auf die Welt geschleudert, um zu sehen, was geschieht.
- E - Götter nehmen Stellen ein: Jede Gottheit, die die Sterblichen verehren, ist eine Stelle, die jeder Gott einnehmen kann, der vorbeikommt. Derselbe Gott ist daher nicht immer derselbe; er verkörpert nur, wofür die Stelle steht, und setzt Zeichen. Götter können jede Stimme nachahmen und bestimmen, was ein Sterblicher in welcher Stimme hört (Beispiel `lore/voelker/nagalith.md`).
- O - Ob und wie die Seele des Spielers selbst eine solche Stelle einnehmen kann.

* E - Das Spiel enthält NSFW-Elemente. Texte schreibt der Entwickler; der Code enthält Mechaniken, die darauf aufbauen.
* E - NSFW-Mechaniken gelten nur für erwachsene, intelligente Personen. Jede Spezies definiert ihr Erwachsenenalter in den Speziesdaten. Die Prüfung erfolgt zentral im Code, nicht pro Text.
* O - Vorschlag: NSFW-Inhalte per Einstellung abschaltbar und als eigenes Datenpaket getrennt, damit eine jugendfreie Fassung möglich ist (Plattformregeln, Altersfreigabe).

## 2. Technik und Architektur

Der Simulationskern ist reines C# ohne Godot-Abhängigkeit; Godot ist nur Darstellung und Eingabe.

- E - Godot 4.7.1, .NET 10.0.11, C#, 2D.
- E - Simulationskern in eigenem Namespace. C#-Dateien liegen im Script-Ordner.
- E - Simulation ist tick-basiert und unterstützt Multicore. Godot liest nur Snapshots.
- E - Schichten: Daten ← Simulation ← Szenario ← Präsentation. Abhängigkeiten nur nach links. Die UI sendet Befehle (Command-Pattern) statt Methoden direkt aufzurufen.
- E - Inhalte sind Daten in YAML, nicht Klassen pro Eintrag, damit Modding möglich ist. Ein Editor folgt bei Bedarf.
- E - Kein Volk, keine Kultur, kein Profil und keine Zone ist im Code fest verdrahtet; Berechnungen wie die Heimatgebiete (Abschnitt 4) laufen beim Weltstart auf den geladenen Daten.
- O - Vorschlag: Das Spiel soll sehr modfreundlich werden. Ziel ist, dass ein Daten-Mod völlig fremde Inhalte einbringen kann, etwa Tiere mit Entwicklungsstufen, die durch die Welt ziehen, oder eine ganze Zivilisation, deren Entwicklungsstufen als eigene Kulturen auftreten.
- E - Kontinente und grobe Verteilung der Spezies sind fest. Ressourcen liegen teilweise zufällig, werden aber einmal mit festem Startwert erzeugt und liegen dann fest (Abschnitt 4).
- E - Zufall nur über seedbare RNG-Instanzen. Gleicher Seed ergibt gleiche Welt.

## 3. Zeit

Die Weltzeit ist ein `ulong`-Zähler in Sekunden seit Weltstart; Kalender und Anzeigen werden daraus berechnet.

- E - Feinste Auflösung: 1 Sekunde, nur wenn nötig (z. B. Kampf, Abschnitt 7). Sonst läuft die Zeit gröber, um den Spieler nicht zu verwirren.
- E - Kalender: 60 Sekunden, 60 Minuten, 20 Stunden pro Tag, 23 Tage pro Monat, 12 Monate (276 Tage pro Jahr). Ergebnis einer Physiksimulation.
- E - Setting orientiert sich am Mittelalter um das Jahr 1000. Jede Spezies bzw. Kultur hat einen eigenen Kalender für die Anzeige; intern gibt es eine Weltzeit.
- E - Zeitraffer über Monate, Jahre und Jahrzehnte sowie "bis zum nächsten Ereignis".
- E - Ein Simulations-Tick entspricht 1 Sekunde Weltzeit. Eine Kampfrunde dauert 6 Sekunden (Abschnitt 7).
- E - Der Spieler kann Zeit in Abschnitten von Tagen, Monaten und Jahren laufen lassen. Eine Partie kann 500 Jahre und länger dauern (Stammbaum, Abschnitt 6; Drachen werden rund 200 Jahre alt und wachsen entsprechend). Lange Abschnitte springen daher von Fälligkeit zu Fälligkeit, statt jede Sekunde einzeln zu rechnen.
- E - Die Vorsimulation beginnt bei Weltzeit 0. Das Spiel beginnt rund 1000 Jahre später in der fertigen Welt mit Städten, Charakteren und Geschichten (Abschnitt 4, 11); das genaue Startjahr kommt aus dem Szenario. So ist auch Vergangenheit (z. B. Geburt eines Drachen, der zum Start 150 Jahre alt ist) als positive `ulong`-Weltzeit darstellbar, und Szenarien können zu verschiedenen Zeiten in dieselbe Welt einsteigen.
- E - Jede Spezies hat einen eigenen Kalender mit eigener Epoche: der Weltzeit, zu der dessen Jahr 1 beginnt. Aufbau (Tage, Monate, Jahre) ist für alle gleich.
- E - Kalender kennen Daten vor ihrem Jahr 1 (wie "vor Christus"): Jahr 1 davor folgt direkt auf Jahr 1 danach, ein Jahr 0 gibt es nicht.
- E - Geschwindigkeit 1 ist Echtzeit: 1 Weltsekunde pro Echtzeitsekunde. Das Spiel startet pausiert und läuft danach mit Normalgeschwindigkeit.
- E - Zeitraffer vorerst nur als Sprung: einen Abschnitt (fester Dauer ab jetzt: 1 Tag = 20 Stunden, 1 Monat = 23 Tage, 1 Jahr = 276 Tage) oder bis zum nächsten Ereignis, jeweils auf Anfrage. Danach gilt Pause. Gleichmäßige Stufen schneller als Echtzeit gibt es vorerst nicht.
- O - Vorschlag: Bis ein Weltereignislog existiert, ist „nächstes Ereignis“ die nächste Fälligkeit im Scheduler. Was später als Ereignis für den Spieler zählt (und Sprünge unterbricht), ist offen.
- E - Im Kampf läuft die Zeit in Runden (6 Sekunden) und steht, bis der eigene Charakter gehandelt hat (Abschnitt 7).
- E - Personen haben Start, Ziel und Aufbruchszeit statt einer laufend berechneten Position. Wo sie gerade sind, wird nur berechnet, wenn der Spieler sie anspricht oder sucht; die genaue Position ist für das Spiel kaum relevant.
- E - Reisen (z. B. zu Gebäuden der eigenen Stadt): Die Zeit läuft schnell bis zur Ankunft oder bis unterwegs etwas passiert. Wege von Minuten bis zu einer halben Stunde beeinflussen das Spielgefühl nicht, eine Partie dauert Monate.
- E - Systeme laufen in eigenen Takten (z. B. Bedürfnisse stündlich, Wirtschaft täglich, Diplomatie wöchentlich).
- E - Abarbeitung über eine nach Fälligkeit sortierte Liste, statt jedes Mal zu prüfen, was berechnet werden muss.
- E - Freie Kapazität wird genutzt, um Berechnungen vorzuziehen. Beispiel: Pro Tick werden 100 Personen berechnet; an einem Feiertag ist Kapazität frei, also rechnet das System Nachbardörfer vor. Vorgezogenes wird nicht unnötig neu berechnet.
- O - Wann vorgezogene Ergebnisse ungültig werden (z. B. Krieg, Seuche, Spielereingriff).

## 4. Welt und Karte

Die Welt ist fest und wird über mehrere Bitmaps beschrieben, aus denen Grenzen berechnet werden; es gibt keine Provinzen.

- E - Handgestaltete Welt, Karte ist WIP. Jedes Spiel beginnt in einer vorsimulierten Welt: Entweder wird sie beim Start neu vorsimuliert, oder man wählt die mitgelieferte, fertig vorsimulierte Welt. Weiterführen einer bestehenden Welt kommt später.
- E - Vorsimulation: Jedes Volk beginnt in seinem **Heimatgebiet**. Darin werden mehrere Startpunkte zufällig gewählt; dort entsteht je eine erste Stadt, und von dort baut die Simulation die Zivilisation auf. Nach rund 1000 Jahren steht das Heimatgebiet unter der Kontrolle des Volks, und es wirkt nach außen.
- E - Begriffe: **Heimatgebiet** ist das Gebiet eines Volks zu Beginn der Vorsimulation. **Startort** ist der Einstieg eines Szenarios (Punkt plus Sicht, Abschnitt 11).
- E - Heimatgebiete werden aus Mittelpunkt, Radius und Lebensraum-Profil je Volk über den Klimazonen berechnet (`lore/voelker/verbreitung.md`); Heimatgebiet ist das Kerngebiet der Berechnung. Einfluss- und Mischgebiet der Berechnung sind Vergleichswerte für das Ergebnis der Vorsimulation. Eine gemalte Ebene in der Größe der Weltkarte (4320×2160) legt sich darüber und hat Vorrang, wo sie bemalt ist. `voelkerkarte.png` ist ein Schaubild (Weltkarte plus 400 px Legende, mit Beschriftungen) und keine Datenebene.
- E - Das Lebensraum-Profil beschreibt das natürliche Habitat eines Volks, nicht wo es leben kann.
- E - Der Planet dreht sich wie die Erde. Die Windrichtung folgt Windgürteln nach Breitengrad (Passate aus Osten, Westwinde in mittleren Breiten, polare Ostwinde), die Windstärke kommt aus den 96 Monatsaufnahmen (12 Monate × 8) der Klimasimulation.
- O - Vorschlag: Die Vorsimulation muss nicht reproduzierbar sein. Ein fester Seed entfällt, wenn sie dadurch auf mehreren Kernen schneller läuft; das Spielgefühl sichern die festen Heimatgebiete und die Startbedingungen der Szenarien (Abschnitt 11).
- O - Dauer der Vorsimulation: Sie soll auf einem aktuellen System in Grenzen bleiben, konkretes Budget offen. Die mitgelieferte Welt trägt eine Version und wird bei Änderungen an Simulation oder Daten neu erzeugt.
- E - Die Weltkarte zeigt nur Hauptflüsse (aus der Klimasimulation). Ein Fluss ist schmaler als eine Zelle: Die Ebene speichert seinen Verlauf als Linie und seine Größe als Klasse, nicht seine gezeichnete Breite. Bäche und Teiche haben keinen Einfluss auf die Welt; sie werden erst beim Betreten einer Gegend zufällig mit platziert.
- O - Vorschlag: Trockenflüsse in Steppe und Wüste: Flussbetten, die nur in der Regenzeit Wasser führen (Oasen, Sturzfluten, Handelswege entlang der Täler).
- E - Einzelne Flussläufe dürfen von Hand vorgegeben werden (`lore/flussvorgaben.yaml`), z. B. der Strom vom Regenwald durch die Wüste in die Bucht im Süden des Ostkontinents.
- O - Vorschlag: Ein einmaliger, riesiger Wasserfall als Naturwunder, von Hand gesetzt. Ort offen (`lore/welt_konfiguration.md` Abschnitt 7).
- E - Zwei Hauptkontinente (West, Ost), Inseln, offenes Meer, Polareis im Norden.
- E - Mehrere Bitmaps sagen, was wo ist. Grenzen werden daraus berechnet.
- E - Kulturen liegen auf einer eigenen Karte und sind fluide.
- E - Gebietsarten: Kerngebiet, Einflussgebiet, Mischgebiet zweier Kulturen, Wildnis.
- E - Ortsgraph statt Raster: Welt → Region → Siedlung → Ort (z. B. Hütte, Koboldhöhle, Marktplatz). Bewegung zwischen Knoten kostet Zeit.
- E - Spielfeld: Der Spieler wählt einen Pixel der Karte 8192 × 4096 (Raster der Heightmap) und beginnt dort. Ein Pixel ist am Äquator rund 4,9 × 4,9 km (≈ 24 km²) und wird polwärts schmaler (bei 60° Breite halb so breit). Siedlungen und Orte liegen im Ortsgraph unterhalb dieser Auflösung.
- E - Weltdaten (Klima, Flüsse, Gestein, Lagerstätten, Bodenfeuchte, Böden, Pflanzen) liegen auf einem gemeinsamen, kleineren Raster 2160 × 1080 (eine Zelle ≈ 18,5 km am Äquator). Jede Ebene hat dieselbe Größe, jede Zelle hat für jedes Material, jeden Boden und jede Pflanze einen Wert. Feiner gibt es die Klimadaten nicht.
- E - Wird ein Spielfeld gewählt, berechnet das Spiel seine Werte aus den umliegenden Zellen der Weltdaten (Mittelwert bzw. Interpolation) und überträgt sie als Eintrag in die Spielwelt. Was im Feld konkret liegt (Erzader, Bach, Wäldchen), wird erst dort gesetzt.
- O - Verfahren für die Werte des Spielfelds (gewichteter Mittelwert, bilinear, Zufall im Feld) und wie die Spielwelt das Feld daraus aufbaut.
- O - Die angezeigte Weltkarte (`resources/maps/weltkarte.png`) hat 4320 × 2160, das Spielfeldraster 8192 × 4096; die Auswahl rechnet zwischen beiden um.
- E - Ressourcen (Gestein, Lagerstätten, Böden, Pflanzen) werden von Werkzeugen in `tools/` einmal mit festem Startwert erzeugt und liegen dann fest; sie werden nicht je Spiel neu verteilt. Verfahren: `lore/welt_konfiguration.md` Abschnitte 8–12, Inhalte: `lore/materialien_und_boeden.md`, `lore/pflanzen.md`.
- E - Als Zwischenschritt bekommt jede Landzelle einen **Gesteinsraum** (geologische Herkunft, z. B. altes Vulkangebiet oder uralter Kontinentkern). Er bestimmt, welche Gesteine und Lagerstätten zusammen auftreten. Gesteinsräume sind keine Gebiete im Spiel und haben mit Grenzen nichts zu tun. Die Zuordnung erfolgt je Gebirgskette von Hand.
- E - Natur-Ebenen: Klimazonen, Köppen-Klassen, Flüsse, Gesteinsräume, Gestein, Lagerstätten, Bodenfeuchte, Böden, Pflanzen.
- O - Weitere Bitmap-Layer (z. B. Spezies, Kultur, Reiche).

**Zielwerte:** Wunschergebnisse der Erzeugung, keine Regeln.

- O - Vorschlag: Seltenheit als flächengewichteter Anteil der Landzellen mit Vorkommen: sehr häufig über 20 %, häufig 5–20 %, mittel 1–5 %, selten 0,1–1 %, sehr selten 0,01–0,1 %, extrem selten unter 0,01 % (einzelne Fundorte weltweit). Einzelne Materialien mit eigener Richtung, z. B. Zinn an 2 bis 4 Fundorten weltweit.
- O - Vorschlag: Jede Landzelle außer Eis hat mindestens eine Pflanze für Nahrung, für Holz oder Brennmaterial und für Faser, sofern das Klima es zulässt.
- O - Was eine Region im Ortsgraph ist und welche Daten sie trägt.
- O - Startort von Szenario 1 auf dem Ostkontinent.

## 5. Völker und Fauna

Rund 70 Spezies in 16 Kulturgruppen, von drachenartigen Riesen bis zu Feenvölkern; die Fauna besteht aus prähistorischen Tieren.

- E - Spezies ist Biologie. Eine Reproduktionstabelle legt fest, welche Spezies gemeinsam Nachkommen haben können. Das gilt nicht automatisch innerhalb einer Kulturgruppe.
- E - Kulturgruppen sind grobe Zusammenschlüsse, die sich aus den Regionen ergeben haben. Kulturen sind fluide (Karte, Abschnitt 4).
- E - Jede Person hat eine Spezies und eine Kultur. Kulturwandel über Generationen ist möglich und eine Konfliktquelle.
- E - Jede Spezies hat eigene Präferenzen, die Siedlungen prägen und Konflikte auslösen (Abschnitt 9).
- E - Die Völker einer Kulturgruppe leben nahe beieinander und ergänzen sich, können aber auch eigene Städte bauen. Siedlungen sind daher von einem Volk, von einer Kulturgruppe oder multikulturell bewohnt; was davon entsteht, ergibt sich in der Vorsimulation.
- E - Wer zuzieht, bringt seine Kultur mit und beeinflusst die Siedlung (Präferenzen, Gebäude, Bräuche), auch die des Spielers (Abschnitt 9).
- E - Größenklasse, Lebensdauer und Ernährung wirken auf Gebäude, Nahrungsbedarf, KCAL und Generationenfolge.
- E - Mystische Wesen (z. B. Drachen) sind benannte Individuen mit Namen und Geschichte aus der Vorsimulation.
- E - Eine Liste der Pflanzen und Tiere stattet die ganze Welt aus: Keine Klima- oder Vegetationszone bleibt ohne Leben, und jede Ressource (z. B. Leinen aus Flachs) hat eine Quelle, die irgendwo wächst bzw. lebt.
- O - Regeln, damit Bestände in der Simulation nicht zusammenbrechen (Nachwachsen, Mindestbestände, Wanderung).
- E - Fauna: prähistorische Tiere als Gegenstück zu den Anthro-Spezies. Sie liefern Rohstoffe wie Fleisch, Fell, Leder und Wolle (z. B. Wollmammut, Wollnashorn).
- O - Konkrete Werte je Spezies (Größenklasse, Lebensdauer, Ernährung).
- E - Die Lebenserwartung im Steckbrief einer Rasse ist das Alter, das ihre Biologie gut erreicht, kein Durchschnitt. Viele sterben früher; bei den Odonata etwa überleben manche Erwachsene das erste Jahr nicht.
- E - Ausarbeitung je Kulturgruppe in `lore/voelker/<kulturgruppe>.md` (Gemeinsame Grundlage, Rassen, Zivilisation); bisher DRACONIK, KATARU, VESKRAL, HYRAX, TARPAN, NAGALITH, HUMANIC, RUMINAN, MERMAN, URODAK. Der Rest steht noch in `lore/Races V3.odt`.
- E - Draconik wechseln ihr Geschlecht im Lauf des Lebens (`lore/voelker/draconik.md`). Das Geschlecht einer Person ist damit veränderlich, und eine Person kann bei verschiedenen Kindern Mutter und Vater sein (Abschnitt 6).
- O - Aufteilung RUMINAN.
- O - Spezies außerhalb ihres Kulturgebiets auf der Karte (WIP): Kitsune, Husky.
- O - CORALOR vorerst gestrichen (2026-10-23): Ein Jägervolk zwischen Land und Wasser wird später neu gedacht. Die alten Texte stehen nur noch in `lore/Races V3.odt`.
- O - RODINI gestrichen (2026-10-24): Ohne eigenen Kern; an ihre Stelle tritt URODAK, eine spielbare Lurchkultur, die mit lebendem Gewebe baut und weiteren Rassen einen Einstieg gibt (`lore/voelker/urodak.md`). Die alten Texte stehen nur noch in `lore/Races V3.odt`.

O - Nagaji (Abschnitt 9, Währung) fehlen in der Tabelle: eigene Spezies, Fauna oder anderer Name für eine NAGALITH-Spezies?

| Kulturgruppe | Spezies |
| --- | --- |
| HUMANIC | Human, Elf, Dwarf, Gnome |
| FEYLIN | Halfling, Changeling, Pixie |
| FENNARI | Wolfkin, Husky, Shepherd, Kitsune, Fennec |
| TARPAN | Equus, Asinus, Hippotigris |
| RUMINAN | Bovinea, Cervinae, Capreolinae, Saola |
| URODAK | Axolotl, Anuran, Salamander, Newt |
| VESKRAL | Girtablilu, Araneae, Mantodea, Odonata |
| NAGALITH | Reptilid, Serathi, DraconicNaga, Lamia, Viper |
| KATARU | Pantherine, Serval, Acinonyx, Caracal |
| HYRAX | Gnoll, Jackal, Bat, Cristatus |
| GOBLIN | Goblin, Hobgoblin |
| DRACONIK | Dragonkin, Kobold, Felbold |
| TENGU | Gryphon, Owl, Harpy, Falcon |
| BRUTAHK | Giant, Goliath, Ursine |
| MERMAN | Mermaid, Siren, Undine, Cecaelia (dazu Leviathan als mystische Wesen) |

**Zielwerte:** Wunschergebnisse der Simulation, keine Regeln. Sie dienen als Prüfwert beim Abstimmen und stehen nicht in der Lore.

- O - Vorschlag: Etwa 1 % der Draconik können fliegen (grobe Richtung).
- O - Vorschlag: Nach rund 1000 Jahren Vorsimulation steht das Heimatgebiet unter Kontrolle des Volks, es gibt 5–6 Reiche und Einfluss über das Heimatgebiet hinaus, ungefähr wie Einfluss- und Mischgebiet der Berechnung (Abschnitt 4). Offen: 5–6 Reiche je Volk oder je Kulturgruppe. Ergibt sich aus Flug ab Stufe 8 (`lore/voelker/draconik.md`, Abschnitt 7), nicht aus einer festen Quote.

## 6. Personen, Charaktere, Stammbaum

Der Spieler steuert eine Party aus beliebigen Personen und pflegt einen lesbaren Stammbaum.

- E - Party: bis zu 6 Charaktere, unabhängig von Familie. Aufnahme per Entscheidung des Spielers, jede intelligente Spezies möglich.
- E - Nicht gesteuerte Partymitglieder bleiben eigenständig, verfolgen eigene Ziele oder gegebene Aufgaben und können jederzeit übernommen werden.
- E - Person-Felder: ID (nie wiederverwendet), Name, Geschlecht, Spezies, Kultur, Geburt und Tod (Weltzeit), Eltern, Kinder, Partner, Level und Attribute, KCAL-Reserve, Merkmale, Ort, Zugehörigkeit, Flag Spielercharakter.
- E - Eltern, Kinder und Partner sind gegenseitig per ID verlinkt.
- E - Der Stammbaum ist lesbar, inklusive Verstorbener.
- E - NPCs haben ebenfalls einen Stammbaum. Er wird erst generiert, wenn der NPC wichtig wird.
- O - Was "Stammbaum verwalten" konkret bedeutet (Ehen arrangieren, Erben bestimmen, Ausbildung, Titel).

## 7. RPG-Regeln, Party, Dungeons

Alles hat ein Level von 0 bis 30 (30 = gottgleich); Proben sind W20 plus Level, wie in klassischen Pen-and-Paper-Rollenspielen.

- E - Level haben: Personen, Kreaturen, Gegenstände, Zauber, Städte.
- E - Probe: W20 + Level + Modifikatoren ≥ SG. Der SG leitet sich ebenfalls aus einem Level ab.
- E - Voller Level-Bonus. Ein Drache Stufe 15 überrennt Bauern Stufe 1. Schlachten können mehrere hundert Beteiligte haben; wer nicht weiß, wogegen er kämpft, riskiert ein Massaker.
- E - Die Stufe ist die Verbindung zur mystischen Welt und gibt übernatürliche Fähigkeiten. Ein Schwert einer Person der Stufe 1 kann einer Person der Stufe 30 nichts anhaben, auch wenn diese sich treffen lassen will.
- E - Kampf in Runden zu 6 Sekunden, auf einem Quadratraster. Hexagon wäre schöner, passt aber nicht zu rechteckigen Räumen und Gebäuden.
- E - Charaktere ziehen als Party (Abschnitt 6) in Dungeons.
- E - Ein zentraler, seedbarer Würfeldienst. Jeder Wurf wird auf Debug geloggt. Auf Statistik-Ebene (Abschnitt 10) wird mit Wahrscheinlichkeiten statt Würfen gerechnet.
- O - Vorschlag: Schlachten mit hunderten Beteiligten statistisch abwickeln, Einzelwürfe nur für benannte Personen.
- E - Vier Erfolgsgrade: Ergebnis ≥ SG + 10 kritischer Erfolg, ≥ SG Erfolg, ≤ SG − 10 kritischer Fehlschlag, sonst Fehlschlag.
- E - Rettungswurf gegen Schaden: kritischer Erfolg kein Schaden, Erfolg halber Schaden, Fehlschlag voller Schaden, kritischer Fehlschlag doppelter Schaden.
- E - Eine natürliche 20 verbessert, eine natürliche 1 verschlechtert den Erfolgsgrad um eine Stufe. Gerundet wird immer abwärts (z. B. halber Schaden).
- O - Attribute, Fertigkeiten.
- O - Herkunft der Dungeons (handgebaut oder generiert), Darstellung des Rasterkampfs im Textadventure.

## 8. Magie

Magie kostet KCAL und verknüpft damit Magie, Ernährung, Spezies-Größe und Wirtschaft.

- E - Magie basiert auf KCAL.
- E - Große Spezies haben mehr Reserve, aber höheren Grundumsatz. Nahrung wird zur Magieressource. Zauber haben ein Level, Kosten skalieren damit.
- E - Jeder hat eine KCAL-Reserve, relevant ist sie aber nur für Magier. Einzeln getrackt wird KCAL nur bei Magiern und anderweitig wichtigen Personen, nicht bei Bauern.
- E - Für die Siedlung wirkt Magie wie Technologie: Personen, die sie gelernt haben, geben Boni.
- E - Körpermagie: Eine Rasse kann selbst die Herkunft ihrer Magie sein. Sie wirkt angeboren im eigenen Körper, ist keine Tradition, muss nicht gelernt werden und kostet KCAL (`lore/Magiesystem.md`, z. B. Atmung der Veskral, Knochensinn der Gnolle).
- O - Vorschlag: KCAL ganzzahlig speichern.
- O - Reserve, Regeneration, Folgen von Überverbrauch (Erschöpfung, Abbau, Tod).
- O - Weitere Kosten, externe Speicher (Tränke, Kristalle) und wer zaubern kann; hängt auch von der Spezies ab.

## 9. Siedlung, Wirtschaft, Technologie, Konflikte

Der Herrscher vergibt Aufträge, die Einwohner arbeiten sie ab und bringen ihre Kultur ein; unterschiedliche Präferenzen erzeugen Konflikte, die er löst.

- E - Aufträge für Gebäude und Pläne, ausgeführt von den Einwohnern.
- E - Aufgabenlisten mit offenen und laufenden Aufgaben, filterbar nach Person, Dorf, Königreich.
- E - Gebäude nach Baukastenprinzip. Offizielle Gebäude müssen die Vorlieben der Bewohner berücksichtigen. Sonst bauen Einwohner nach eigenen Vorlieben.
- E - Jede Spezies hat eine Prioritäten- und Wunschliste, was sie im Dorf verändern will. Neubau und Erweiterung haben Vorrang vor Umbau und Abriss, damit Gruppen sich nicht gegenseitig abreißen.
- E - Ressourcen werden gesammelt oder gehandelt.
- E - Alles wird aus dem hergestellt, was existiert: Werkzeuge, Kleidung, Waren. Nichts entsteht aus dem Nichts.
- E - Werkzeuge stellen NPCs her; sie sind die Grundlage der Technologie. Anfangs Steinwerkzeug und Ähnliches. Bessere Werkzeuge entstehen in natürlicher Weiterentwicklung, je nachdem, was verfügbar ist.
- E - Gleiches gilt für Kleidung und andere Waren. Beispiel: Kobolde tragen anfangs einfaches Leder oder nichts und bauen später aus eigenem Interesse eine Spinnerei und einen Webstuhl.
- E - Spinnen braucht Fasern: Wolle, Baumwolle oder Fell. Tierische Fasern liefert die prähistorische Fauna (Abschnitt 5).
- E - Technologie als Rezepte mit Voraussetzungen (Wissen, Ressourcen, Werkzeug, Fähigkeit oder Level), kein fester Baum.
- E - Rezepte ergeben sich aus natürlich vorkommenden Pflanzen und Materialien. Es gibt keine feste Abfolge: Wo es Papyrus gibt, entsteht Papyrus, anderswo etwas anderes.
- E - Rezepte kommen auch über Personen: Ein Meisterschmied ermöglicht neue Materialien. Neue Technologien entwickeln NPCs selbst; der Spieler schafft dafür die Freiräume.
- E - Werkzeug ist Voraussetzung in Rezepten. Material und Level des Werkzeugs wirken auf Arbeitszeit und Qualität.
- E - Dinge werden stabil und zuverlässig gebaut. Reparatur hat Vorrang vor Neuanfertigung.
- O - Welche Pflanzen und Materialien natürlich vorkommen (Grundlage aller Rezepte). Arbeitsstand: `lore/materialien_und_boeden.md` und `lore/pflanzen.md`, noch nicht bestätigt; die Listen werden zu YAML unter `data/`.
- E - Das Spiel startet nicht im Kapitalismus. Bis Wohlstand entsteht, zählen Vertrauen und Beziehungen mehr; jeder kennt jeden, die Gemeinschaft hilft sich.
- E - Währung ergibt sich aus den Umständen: das Gut, das die Krone festlegt und das akzeptiert wird, oder ein Gut, das selten, aber verfügbar ist. Beispiel: Die Startkobolde handeln mit Zähnen oder Schuppen von Nagaji; wer Besucher will, muss die Nagaji beschützen.

**Bedürfnisse**

- E - Bedürfnisse sind ein ausgearbeitetes Kernsystem. Zu jedem Bedürfnis gibt es Technologien oder Lösungen, die NPCs nutzen. Eigenes Interesse entsteht aus Bedürfnis, verfügbarem Material und Wissen (z. B. Kälte + Wolle vorhanden → Spinnerei).
- E - Viele Bedürfnisse wie Familie und Freude werden kostenlos erfüllt, etwa durch Feiern. Dadurch wird weniger gearbeitet.
- E - NPCs wollen ein gutes Leben, nicht arbeiten. Bier und stärkere Rauschmittel stehen weit oben. Leidenschaften dürfen dabei nicht zu kurz kommen.
- E - NPCs mit viel Freizeit suchen sich Beschäftigung.
- E - NPCs, die für ihr Überleben viel arbeiten müssen (Beeren sammeln, Holz hacken), tun wenig für den Herrscher.
- O - Vorschlag: Konflikte systemisch. Bedürfnisse und Tabus je Spezies und Kultur plus eine Haltungsmatrix ergeben Spannungen. Über einem Schwellenwert entsteht ein Ereignis mit Optionen aus generischen Vorlagen.
- O - Vorschlag: Startwerte der Haltungsmatrix kommen aus der Lore. Beispiel: Die übrigen Völker begegnen den NAGALITH mit Abscheu (`lore/voelker/nagalith.md`).

**Offen**

- O - Handel zwischen Siedlungen und Reichen: Tausch, Händler als Personen, Karawanen?
- O - Wie stark kann der Spieler Einwohner außerhalb der Party steuern, außer über Aufträge?
- O - Wie lösen NPC-Herrscher Konflikte in ihren Reichen? Vorschlag: dasselbe System, eine KI wählt die Option.

## 10. Simulationsebenen

Bei 100.000+ Einwohnern pro Reich und vielen parallelen Reichen wird nicht jeder Einwohner einzeln simuliert.

- E - Drei Detailstufen. Lokal (Umfeld der Spielercharaktere): Einzelpersonen in voller Auflösung. Eigenes Reich außerhalb des Fokus: Bevölkerungsgruppen und Produktionsraten, stündlich bis täglich. Fremde Reiche: strategisch (Bevölkerung, Wirtschaft, Diplomatie, Krieg), täglich bis wöchentlich.
- E - Benannte Personen (Spielercharaktere, Familie, Herrscher, Meister, alle mit Spielerkontakt) existieren dauerhaft als Objekte. Das übrige Volk ist Statistik je Siedlung.
- E - Relevante Personen und Kreaturen werden auch außerhalb des Fokus und in der Vorsimulation einzeln simuliert, nicht jede Person. Relevanz ergibt sich aus dem Level: Ab einem Schwellwert wird eine Kreatur einzeln simuliert (z. B. Abenteurer Stufe 5 jagen einen Drachen; daraus entsteht die Geschichte des Drachen). Welche Kreatur auf welchem Level liegt, legt eine Liste fest.
- O - Level-Schwellwert für die Einzelsimulation.
- E - Betritt ein Spielercharakter einen Ort, entstehen aus der Statistik konkrete Personen (deterministisch aus Seed und Ort). Sie bleiben benannt, weil Spieler sich an Namen erinnern (z. B. Schmiede, Magier).
- O - Vorschlag: Personen ohne Rolle und ohne Spielerkontakt über lange Zeit dürfen zurück in die Statistik, falls Speicher knapp wird.
- E - Massendaten in Arrays oder Structs statt tiefer Objektgraphen. Vor allem Zeitstempel loggen.
- E - Eine Übersicht zeigt, wo wie viel Aufwand in die Simulation fließt.
- E - Ein Simulations-Tick dauert höchstens 8 ms (Ziel: 120 FPS). Überschreitungen werden als Warning geloggt.
- O - Zeitbudget pro Spieltag im Zeitraffer.

## 11. Szenarien

Ein Szenario ist ein Einstiegspunkt in die fertig vorsimulierte Welt, kein absoluter Weltstart: Person, Ort, Umstände, Ziele.

- E - Szenarien stellen Bedingungen an den Startort statt ihn nur fest vorzugeben. Erfüllt die Welt sie am Punkt nicht, verschiebt das Szenario den Start in die Nähe (z. B. bis etwa 50 km, um eine Stadt oder Burg zu erreichen).

**Szenario 1: Expedition in die östlichen Lande**

- E - Start als Mensch einer Expedition in die östlichen Lande, zunächst allein. Ziel: Stadt und später Königreich über Generationen.
- E - Ein Gott startet die Expedition: Er fordert einen Menschen der HUMANIC auf, in den Osten zu reisen, und lässt ihn unmenschlich schnell dorthin gelangen. Dort übernimmt die Seele des Spielers den Körper und tötet dabei die eigentliche Seele (`lore/voelker/humanic.md`).
- O - Was der Körper von seinem früheren Leben behält (Sprache, Wissen, Erinnerungen, Fertigkeiten).
- O - Tötet jeder Eintritt der Seele die eigentliche Seele, auch beim Charakterwechsel über Generationen (Abschnitt 1, 6), oder nur dieser erste?
- E - Durch Erkundung kommen weitere Charaktere dazu, die alle besonders sind.
- E - Ein Kobold ist RPG-Begleiter und Tutorial; der Spieler redet mit ihm. Er gräbt selbstständig eine Höhle, danach kommen 3–4 Kobolde dazu und versorgen sich selbst. Mit ihrer Hilfe entsteht die erste Hütte.
- E - Nur die Dialoge des Kobolds sind gescriptet, sein Verhalten läuft über die normale Simulation. Tutorial-Fortschritt ist Szenario-Zustand.
- E - In 20–80 km Entfernung zum Start liegt immer eine Koboldkolonie.
- O - Sind die besonderen Charaktere unsterblich?
- O - Startort.

**Spätere Szenarien**

- E - Geboren bei den Naga. Gefangen bei den Hyrax, freihandeln oder freikämpfen.
- E - Das Naga-Szenario startet immer im Naga-Gebiet; in welcher Stadt, ergibt die Vorsimulation.
- O - Weiterführen einer bestehenden Welt in einem neuen Szenario.

## 12. UI, Logging, Speichern, Sprache

Logging ist Pflicht und dient zugleich als Chronik des Reiches im Spiel.

- E - Haupttext über `RichTextLabel` mit BBCode. Namen und Orte sind anklickbar (`meta_clicked`) und öffnen Personenkarte, Stammbaum oder Ort.
- O - Welche Ansichten nötig sind, kommt später.
- E - Szenarioauswahl: Hauptmenü → Start → Szenarioauswahl auf der Weltkarte. Oben Jahresanzeige (Startjahr des gewählten Szenarios) und Buttons für Kartenmodi; links die Liste aller Szenarien und darunter die Details (Vorschaubild, Titel, Tags, Beschreibung); in der Mitte die Karte; unten die neuesten Szenarien mit Text zu Änderungen und Features; unten links Zurück (Hauptmenü), unten rechts Weiter.
- E - Die Karte lässt sich verschieben (Ziehen) und zoomen (Mausrad, Buttons, Zurücksetzen auf Gesamtansicht). Kartenmodi tauschen die Kartenebene, Ausschnitt und Zoom bleiben.
- E - Szenarien erscheinen zusätzlich als Punkte mit Icon auf der Karte. Punkt, Listeneintrag und News-Karte wählen dasselbe aus; die Auswahl fährt die Karte an den Punkt heran („Zoom zum Punkt“), abschaltbar. Doppelklick wirkt wie Weiter.
- E - Gesperrte bzw. geplante Szenarien erscheinen als gesperrt, zeigen ihre Details und lassen sich nicht starten.
- E - Szenarien werden über eine Konfigurationsdatei beschrieben (Position auf der Weltkarte, Icon, Vorschaubild, Startjahr, Texte über Schlüssel, Neuigkeiten mit Datum/Version). Icons liegen unter `resources/MapIcons/`.
- O - Vorschlag: Zustand der Punkte (normal, Hover, ausgewählt, gesperrt) über einen Ring bzw. Hintergrundkreis hinter dem Icon, da die Icons schwarz sind und sich per `Modulate` nicht einfärben lassen.
- E - Kartenmodi der Szenarioauswahl: Ressourcen, Bevölkerung, Politik, Flora/Fauna; Terrain liegt darunter. Sie zeigen Daten der vorsimulierten Welt und kommen daher mit der Weltsimulation (Meilenstein 5).
- E - Im Spiel gibt es eine eigene Weltkarte, u. a. mit der bekannten Welt (was der Spielercharakter kennt). Weitere Modi offen.
- O - Ob Weiter später einen Zwischenschritt öffnet (Charakter, Optionen) statt direkt zu starten; ob das Startjahr wählbar wird; welchen Stand der Welt die Kartenmodi der Szenarioauswahl zeigen (Vorschlag: vorsimulierte Welt im Startjahr des Szenarios); ob „Zoom zum Punkt“ gespeichert wird.
- E - Schriften: Eczar für alle UI-Texte, Cardo für Beschreibungstexte.
- E - Aufbau in Godot steht in den Szenen selbst (`scenes/ui/ScenarioSelect.tscn`, Module unter `scenes/ui/scenario/`, Theme `resources/ui/TiefbergTheme.tres`); die Scripts binden die Nodes ausschließlich über Exports, ohne Ersatzpfad; fehlt eine Zuweisung, wird das als Error geloggt und abgebrochen. Szenen und Theme baut der Entwickler im Editor, Scripts kommen dazu passend.
- O - Vorschlag: Tooltip am Kartenpunkt, Tastatursteuerung (Pfeil hoch/runter, Enter, Esc), letzte Auswahl merken, Schwierigkeit/Tags im Panel, Freischalt-Hinweis bei gesperrten Szenarien.
- E - Logging ist Pflicht, darf aber am Ende nicht viel Leistung kosten.
- E - Technisches Log über `Microsoft.Extensions.Logging` oder Serilog im Simulationskern, unabhängig von `GD.Print`. Logger per Konstruktor. Jede Simulationsmeldung trägt die Weltzeit.
- O - Vorschlag: Teure Meldungen nur nach Level-Prüfung erzeugen; im Release standardmäßig ab Warning.
- E - Jede Logzeile enthält Systemzeit und Klasse.Methode, damit Logs nach Abläufen und Laufzeit ausgewertet werden können.
- E - Log-Level: Error für Invariantenverletzungen, Warning für Budgetüberschreitungen, Information für System- und Szenariostarts, Debug für Würfe und Entscheidungen.
- E - Debug-Werkzeuge dürfen Spielern zugänglich sein, auch zum Cheaten. Spaß hat hohe Priorität.
- E - Das Debugmenü öffnet sich, wenn der Spieler die Buchstabenfolge D E B U G N O W eintippt.
- E - Weltereignislog (Geburten, Tode, Krönungen, Bauten, Kriege, Konflikte) als strukturierte Ereignisse. Daraus entsteht die Chronik.
- E - Weltzustand und Szenariozustand werden getrennt gespeichert; der Weltzustand weiß nichts vom Szenario. So kann das Szenario gewechselt werden. Speicherformat versioniert, Seed und Weltzeit werden mitgespeichert.
- E - Spielstände binär für schnelles Laden und Speichern.
- E - Regelmäßige Autosaves, nicht destruktiv (überschreiben keine älteren Stände ohne Rotation).
- E - Spiel komplett auf Englisch.
- E - Texte immer über Schlüssel, nie im Code, damit später weitere Sprachen möglich sind.

## 13. Meilensteine und Glossar

Erst ein kleiner spielbarer Prototyp mit voller Architektur, dann Inhaltsbreite. Offene Punkte stehen als `O -` direkt in den Abschnitten.

**Meilensteine (E)**

1. **Fundament:** Repo, Script-Ordner und Namespaces, Logging, Scheduler, Weltzeit, Würfeldienst, Speichern und Laden.
2. **Prototyp Expedition:** eine Siedlung, 30–50 Personen, 4 Spezies (Mensch, Kobold, eine sehr große, eine sehr kleine), 5 Gebäude, 3 Ressourcen, Aufgabenliste, Kobold-Tutorial bis zur Hütte.
3. **Generationen:** Geburt, Tod, Reproduktionstabelle, Charakterwechsel, Stammbaum, Chronik.
4. **Konflikte und Kultur:** Spannungssystem, erste Ereignisse, Kulturwandel.
5. **Weltsimulation:** Bitmap-Layer, statistische Bevölkerung, fremde Reiche, Handel.
6. **RPG-Ausbau:** Party, Dungeons, Kampf, Magie.

**Glossar**

| Begriff | Bedeutung |
| --- | --- |
| Seele | Spielerfigur, tritt in Personen ein |
| Spielercharakter | Person, in die die Seele eintreten kann |
| Benannte Person | Dauerhaft als Objekt simulierte Person |
| Bevölkerungsgruppe | Statistisch simulierte Menge von Personen |
| SG | Schwierigkeitsgrad einer W20-Probe |
| KCAL | Energie für Körper und Magie |
