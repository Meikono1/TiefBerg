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
- E - Charaktere ziehen wie eine D&D-Party in Dungeons (Abschnitt 7).
- E - Eigener Stammbaum als Kernfeature (Abschnitt 6).
- E - Spielercharaktere sind besonders, weil der Spieler ihre Geschichte erlebt: weniger Krankheit, keine zufälligen permanenten Verletzungen. Sonst gelten dieselben Regeln wie für NPCs.
- E - Jede Kulturgruppe und Spezies hat eigene Königreiche und Dörfer, die passiv weiterlaufen und technologisch unterschiedlich weit sein können.
- E - Technologie ist offen. Eisen, Kupfer oder Schwarzpulver sind ab Start möglich, wenn die Voraussetzungen erfüllt sind.
- E - Reiche erreichen 100.000 Einwohner und mehr.
- E - Szenarien sind verschiedene Einstiege in dieselbe Welt (Abschnitt 11).

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
- E - Kontinente und grobe Verteilung der Spezies sind fest. Ressourcen liegen teilweise zufällig.
- E - Zufall nur über seedbare RNG-Instanzen. Gleicher Seed ergibt gleiche Welt.

## 3. Zeit

Die Weltzeit ist ein `ulong`-Zähler in Sekunden seit Weltstart; Kalender und Anzeigen werden daraus berechnet.

- E - Feinste Auflösung: 1 Sekunde, nur wenn nötig (z. B. Kampf, Abschnitt 7). Sonst läuft die Zeit gröber, um den Spieler nicht zu verwirren.
- E - Kalender: 60 Sekunden, 60 Minuten, 20 Stunden pro Tag, 23 Tage pro Monat, 12 Monate (276 Tage pro Jahr). Ergebnis einer Physiksimulation.
- E - Setting orientiert sich am Mittelalter um das Jahr 1000. Jede Spezies bzw. Kultur hat einen eigenen Kalender für die Anzeige; intern gibt es eine Weltzeit.
- E - Zeitraffer über Monate, Jahre und Jahrzehnte sowie "bis zum nächsten Ereignis".
- E - Ein Simulations-Tick entspricht 1 Sekunde Weltzeit. Eine Kampfrunde dauert 6 Sekunden (Abschnitt 7).
- E - Der Spieler kann Zeit in Abschnitten von Tagen, Monaten und Jahren laufen lassen. Eine Partie kann 500 Jahre und länger dauern (Stammbaum, Abschnitt 6; Drachen werden rund 200 Jahre alt und wachsen entsprechend). Lange Abschnitte springen daher von Fälligkeit zu Fälligkeit, statt jede Sekunde einzeln zu rechnen.
- E - Weltzeit 0 ist ein fester Punkt weit in der Urzeit, nicht der Simulationsstart. Die Simulation beginnt bei einer Startzeit aus Welt- bzw. Szenariodaten; so bleibt auch Vergangenheit (z. B. Geburt eines Drachen, der zum Start 150 Jahre alt ist) als positive `ulong`-Weltzeit darstellbar, und Szenarien können zu verschiedenen Zeiten in dieselbe Welt einsteigen (Abschnitt 11).
- E - Jede Spezies hat einen eigenen Kalender mit eigener Epoche: der Weltzeit, zu der dessen Jahr 1 beginnt. Aufbau (Tage, Monate, Jahre) ist für alle gleich.
- E - Kalender kennen Daten vor ihrem Jahr 1 (wie "vor Christus"): Jahr 1 davor folgt direkt auf Jahr 1 danach, ein Jahr 0 gibt es nicht.
- O - Konkreter Wert der Startzeit und wie weit Weltzeit 0 zurückliegt.
- E - Systeme laufen in eigenen Takten (z. B. Bedürfnisse stündlich, Wirtschaft täglich, Diplomatie wöchentlich).
- E - Abarbeitung über eine nach Fälligkeit sortierte Liste, statt jedes Mal zu prüfen, was berechnet werden muss.
- E - Freie Kapazität wird genutzt, um Berechnungen vorzuziehen. Beispiel: Pro Tick werden 100 Personen berechnet; an einem Feiertag ist Kapazität frei, also rechnet das System Nachbardörfer vor. Vorgezogenes wird nicht unnötig neu berechnet.
- O - Wann vorgezogene Ergebnisse ungültig werden (z. B. Krieg, Seuche, Spielereingriff).

## 4. Welt und Karte

Die Welt ist fest und wird über mehrere Bitmaps beschrieben, aus denen Grenzen berechnet werden; es gibt keine Provinzen.

- E - Handgestaltete Welt, Karte ist WIP. Vorerst startet jedes Spiel mit frischer Welt; Weiterführen kommt später.
- E - Zwei Hauptkontinente (West, Ost), Inseln, offenes Meer, Polareis im Norden.
- E - Mehrere Bitmaps sagen, was wo ist. Grenzen werden daraus berechnet.
- E - Kulturen liegen auf einer eigenen Karte und sind fluide.
- E - Gebietsarten: Kerngebiet, Einflussgebiet, Mischgebiet zweier Kulturen, Wildnis.
- E - Ortsgraph statt Raster: Welt → Region → Siedlung → Ort (z. B. Hütte, Koboldhöhle, Marktplatz). Bewegung zwischen Knoten kostet Zeit.
- O - Maßstab aktuell 5 km pro Pixel. Vorschlag: reicht für Weltlayer (Terrain, Klima, Ressourcen, Grenzen). Siedlungen und Orte liegen im Ortsgraph unterhalb der Pixelauflösung. Bei Bedarf eigene, feinere Karten nur für Siedlungen.
- O - Welche Bitmap-Layer es gibt (z. B. Terrain, Höhe, Ressourcen, Spezies, Kultur).
- O - Was eine Region im Ortsgraph ist und welche Daten sie trägt.
- O - Startort von Szenario 1 auf dem Ostkontinent.

## 5. Völker und Fauna

Rund 70 Spezies in 16 Kulturgruppen, von drachenartigen Riesen bis zu mausartigen Völkern; die Fauna besteht aus prähistorischen Tieren.

- E - Spezies ist Biologie. Eine Reproduktionstabelle legt fest, welche Spezies gemeinsam Nachkommen haben können. Das gilt nicht automatisch innerhalb einer Kulturgruppe.
- E - Kulturgruppen sind grobe Zusammenschlüsse, die sich aus den Regionen ergeben haben. Kulturen sind fluide (Karte, Abschnitt 4).
- E - Jede Person hat eine Spezies und eine Kultur. Kulturwandel über Generationen ist möglich und eine Konfliktquelle.
- E - Jede Spezies hat eigene Präferenzen, die Siedlungen prägen und Konflikte auslösen (Abschnitt 9).
- E - Größenklasse, Lebensdauer und Ernährung wirken auf Gebäude, Nahrungsbedarf, KCAL und Generationenfolge.
- E - Fauna: prähistorische Tiere als Gegenstück zu den Anthro-Spezies. Sie liefern Rohstoffe wie Fleisch, Fell, Leder und Wolle (z. B. Wollmammut, Wollnashorn).
- O - Konkrete Werte je Spezies (Größenklasse, Lebensdauer, Ernährung).
- O - Aufteilung RUMINAN, Lebensraum von Cristasus.
- O - Spezies außerhalb ihres Kulturgebiets auf der Karte (WIP): Jerboa, Euplerid, Axolotl, Kitsune, Husky, Pinniped.

O - Nagaji (Abschnitt 9, Währung) fehlen in der Tabelle: eigene Spezies, Fauna oder anderer Name für eine NAGALITH-Spezies?

| Kulturgruppe | Spezies |
| --- | --- |
| HUMANIC | Human, Elf, Dwarf, Gnome |
| FEYLIN | Halfling, Changeling, Pixie |
| FENNARI | Wolfkin, Husky, Shepherd, Kitsune, Fennec |
| TARPAN | Equus, Asinus, Hippotigris |
| RUMINAN | Bovinea, Cervinae, Capreolinae, Saola |
| RODINI | Chinchilla, Caviid, Leporid, Squirrel, Mustelid, Murid, Jerboa, Euplerid |
| VESKRAL | Girtablilu, Araneae, Mantodea, Odonata |
| NAGALITH | Reptilid, Serathi, DraconicNaga, Lamia, Viper |
| KATARU | Pantherine, Serval, Acinonyx, Caracal |
| HYRAX | Gnoll, Jackal, Bat, Cristasus |
| GOBLIN | Goblin, Hobgoblin |
| KOBOLD | Dragonkin, Kobold, Felbold |
| TENGU | Gryphon, Owl, Harpy, Falcon |
| BRUTAHK | Giant, Goliath, Ursine |
| CORALOR | Hippocampus, Cephalopod, Carcharhini, Cetacean, Crocodilian, Axolotl, Pinniped |
| MERMAN | Merman, Leviathan, Siren, Delphinoid |

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

Alles hat ein Level von 0 bis 30 (30 = gottgleich); Proben sind W20 plus Level, angelehnt an D&D 3.5 und Pathfinder 1/2.

- E - Level haben: Personen, Kreaturen, Gegenstände, Zauber, Städte.
- E - Probe wie PF2e: W20 + Level + Modifikatoren ≥ SG. Der SG leitet sich ebenfalls aus einem Level ab.
- E - Voller Level-Bonus. Ein Drache Stufe 15 überrennt Bauern Stufe 1. Schlachten können mehrere hundert Beteiligte haben; wer nicht weiß, wogegen er kämpft, riskiert ein Massaker.
- E - Kampf in Runden zu 6 Sekunden (PF2e), auf einem Quadratraster. Hexagon wäre schöner, passt aber nicht zu rechteckigen Räumen und Gebäuden.
- E - Charaktere ziehen als Party (Abschnitt 6) in Dungeons.
- E - Ein zentraler, seedbarer Würfeldienst. Jeder Wurf wird auf Debug geloggt. Auf Statistik-Ebene (Abschnitt 10) wird mit Wahrscheinlichkeiten statt Würfen gerechnet.
- O - Vorschlag: Schlachten mit hunderten Beteiligten statistisch abwickeln, Einzelwürfe nur für benannte Personen.
- O - Kritische Erfolge, Attribute, Fertigkeiten.
- O - Herkunft der Dungeons (handgebaut oder generiert), Darstellung des Rasterkampfs im Textadventure.

## 8. Magie

Magie kostet KCAL und verknüpft damit Magie, Ernährung, Spezies-Größe und Wirtschaft.

- E - Magie basiert auf KCAL.
- E - Große Spezies haben mehr Reserve, aber höheren Grundumsatz. Nahrung wird zur Magieressource. Zauber haben ein Level, Kosten skalieren damit.
- E - Jeder hat eine KCAL-Reserve, relevant ist sie aber nur für Magier. Einzeln getrackt wird KCAL nur bei Magiern und anderweitig wichtigen Personen, nicht bei Bauern.
- E - Für die Siedlung wirkt Magie wie Technologie: Personen, die sie gelernt haben, geben Boni.
- O - Vorschlag: KCAL ganzzahlig speichern.
- O - Reserve, Regeneration, Folgen von Überverbrauch (Erschöpfung, Abbau, Tod).
- O - Weitere Kosten, externe Speicher (Tränke, Kristalle) und wer zaubern kann; hängt auch von der Spezies ab.

## 9. Siedlung, Wirtschaft, Technologie, Konflikte

Der Herrscher vergibt Aufträge, die Einwohner arbeiten sie ab und bringen ihre Kultur ein; unterschiedliche Präferenzen erzeugen Konflikte, die er löst.

- E - Aufträge für Gebäude und Pläne, ausgeführt von den Einwohnern.
- E - Aufgabenlisten wie in Prison Architect: offene und laufende Aufgaben, filterbar nach Person, Dorf, Königreich.
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
- O - Welche Pflanzen und Materialien natürlich vorkommen (Grundlage aller Rezepte).
- E - Das Spiel startet nicht im Kapitalismus. Bis Wohlstand entsteht, zählen Vertrauen und Beziehungen mehr; jeder kennt jeden, die Gemeinschaft hilft sich.
- E - Währung ergibt sich aus den Umständen: das Gut, das die Krone festlegt und das akzeptiert wird, oder ein Gut, das selten, aber verfügbar ist. Beispiel: Die Startkobolde handeln mit Zähnen oder Schuppen von Nagaji; wer Besucher will, muss die Nagaji beschützen.

**Bedürfnisse**

- E - Bedürfnisse sind ein ausgearbeitetes Kernsystem. Zu jedem Bedürfnis gibt es Technologien oder Lösungen, die NPCs nutzen. Eigenes Interesse entsteht aus Bedürfnis, verfügbarem Material und Wissen (z. B. Kälte + Wolle vorhanden → Spinnerei).
- E - Viele Bedürfnisse wie Familie und Freude werden kostenlos erfüllt, etwa durch Feiern. Dadurch wird weniger gearbeitet.
- E - NPCs wollen ein gutes Leben, nicht arbeiten. Bier und stärkere Rauschmittel stehen weit oben. Leidenschaften dürfen dabei nicht zu kurz kommen.
- E - NPCs mit viel Freizeit suchen sich Beschäftigung.
- E - NPCs, die für ihr Überleben viel arbeiten müssen (Beeren sammeln, Holz hacken), tun wenig für den Herrscher.
- O - Vorschlag: Konflikte systemisch. Bedürfnisse und Tabus je Spezies und Kultur plus eine Haltungsmatrix ergeben Spannungen. Über einem Schwellenwert entsteht ein Ereignis mit Optionen aus generischen Vorlagen.

**Offen**

- O - Handel zwischen Siedlungen und Reichen: Tausch, Händler als Personen, Karawanen?
- O - Wie stark kann der Spieler Einwohner außerhalb der Party steuern, außer über Aufträge?
- O - Wie lösen NPC-Herrscher Konflikte in ihren Reichen? Vorschlag: dasselbe System, eine KI wählt die Option.

## 10. Simulationsebenen

Bei 100.000+ Einwohnern pro Reich und vielen parallelen Reichen wird nicht jeder Einwohner einzeln simuliert.

- E - Drei Detailstufen. Lokal (Umfeld der Spielercharaktere): Einzelpersonen in voller Auflösung. Eigenes Reich außerhalb des Fokus: Bevölkerungsgruppen und Produktionsraten, stündlich bis täglich. Fremde Reiche: strategisch (Bevölkerung, Wirtschaft, Diplomatie, Krieg), täglich bis wöchentlich.
- E - Benannte Personen (Spielercharaktere, Familie, Herrscher, Meister, alle mit Spielerkontakt) existieren dauerhaft als Objekte. Das übrige Volk ist Statistik je Siedlung.
- E - Betritt ein Spielercharakter einen Ort, entstehen aus der Statistik konkrete Personen (deterministisch aus Seed und Ort). Sie bleiben benannt, weil Spieler sich an Namen erinnern (z. B. Schmiede, Magier).
- O - Vorschlag: Personen ohne Rolle und ohne Spielerkontakt über lange Zeit dürfen zurück in die Statistik, falls Speicher knapp wird.
- E - Massendaten in Arrays oder Structs statt tiefer Objektgraphen. Vor allem Zeitstempel loggen.
- E - Eine Übersicht zeigt, wo wie viel Aufwand in die Simulation fließt.
- E - Ein Simulations-Tick dauert höchstens 8 ms (Ziel: 120 FPS). Überschreitungen werden als Warning geloggt.
- O - Zeitbudget pro Spieltag im Zeitraffer.

## 11. Szenarien

Ein Szenario ist eine Startkonfiguration in der Welt: Person, Ort, Umstände, Ziele.

**Szenario 1: Expedition in die östlichen Lande**

- E - Start als Mensch einer Expedition in die östlichen Lande, zunächst allein. Ziel: Stadt und später Königreich über Generationen.
- E - Durch Erkundung kommen weitere Charaktere dazu, die alle besonders sind.
- E - Ein Kobold ist RPG-Begleiter und Tutorial; der Spieler redet mit ihm. Er gräbt selbstständig eine Höhle, danach kommen 3–4 Kobolde dazu und versorgen sich selbst. Mit ihrer Hilfe entsteht die erste Hütte.
- E - Nur die Dialoge des Kobolds sind gescriptet, sein Verhalten läuft über die normale Simulation. Tutorial-Fortschritt ist Szenario-Zustand.
- O - Sind die besonderen Charaktere unsterblich?
- O - Startort.

**Spätere Szenarien**

- E - Geboren bei den Naga. Gefangen bei den Hyrax, freihandeln oder freikämpfen.
- O - Weiterführen einer bestehenden Welt in einem neuen Szenario.

## 12. UI, Logging, Speichern, Sprache

Logging ist Pflicht und dient zugleich als Chronik des Reiches im Spiel.

- E - Haupttext über `RichTextLabel` mit BBCode. Namen und Orte sind anklickbar (`meta_clicked`) und öffnen Personenkarte, Stammbaum oder Ort.
- O - Welche Ansichten nötig sind, kommt später.
- E - Logging ist Pflicht, darf aber am Ende nicht viel Leistung kosten.
- E - Technisches Log über `Microsoft.Extensions.Logging` oder Serilog im Simulationskern, unabhängig von `GD.Print`. Logger per Konstruktor. Jede Simulationsmeldung trägt die Weltzeit.
- O - Vorschlag: Teure Meldungen nur nach Level-Prüfung erzeugen; im Release standardmäßig ab Warning.
- E - Jede Logzeile enthält Systemzeit und Klasse.Methode, damit Logs nach Abläufen und Laufzeit ausgewertet werden können.
- E - Log-Level: Error für Invariantenverletzungen, Warning für Budgetüberschreitungen, Information für System- und Szenariostarts, Debug für Würfe und Entscheidungen.
- E - Debug-Werkzeuge dürfen Spielern zugänglich sein, auch zum Cheaten. Spaß hat hohe Priorität.
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
