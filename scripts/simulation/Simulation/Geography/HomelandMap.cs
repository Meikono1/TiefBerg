using System.Collections.Generic;
using TiefBerg.Data.Peoples;

namespace TiefBerg.Simulation.Geography;

// Gebietsarten der Berechnung (Design-Referenz Abschnitt 4). Kern = Heimatgebiet der Völker;
// Einfluss- und Mischgebiet sind Vergleichswerte für das Ergebnis der Vorsimulation.
public enum HomelandArea : byte
{
    Wilderness,
    Core,
    Influence,
    Mixed,
}

// Ergebnis von HomelandCalculator auf dem Rechenraster (lore/voelker/verbreitung.md).
// Kulturindizes beziehen sich auf Cultures, −1 = keine. Heimatgebiete der Völker einer Kultur dürfen sich überlappen:
// Dort leben sie nebeneinander, gemeinsame Städte entstehen in der Vorsimulation (Design-Referenz Abschnitt 5).
public sealed class HomelandMap
{
    private readonly HomelandArea[] _area;
    private readonly short[] _culture;
    private readonly int[][] _homelands;

    internal HomelandMap(int width, int height, IReadOnlyList<CultureDefinition> cultures,
        IReadOnlyList<PeopleDefinition> peoples, HomelandArea[] area, short[] culture, int[][] homelands)
    {
        Width = width;
        Height = height;
        Cultures = cultures;
        Peoples = peoples;
        _area = area;
        _culture = culture;
        _homelands = homelands;
    }

    public int Width { get; }

    public int Height { get; }

    public IReadOnlyList<CultureDefinition> Cultures { get; }

    // Alle berechneten Völker, kulturweise in Dateireihenfolge
    public IReadOnlyList<PeopleDefinition> Peoples { get; }

    public HomelandArea AreaAt(int x, int y) => _area[y * Width + x];

    // Stärkste Kultur der Zelle, auch in Einfluss- und Mischgebiet; −1 in der Wildnis
    public int CultureAt(int x, int y) => _culture[y * Width + x];

    // Zellen (Index = y · Width + x) des Heimatgebiets eines Volks, aufsteigend; leer, wenn es keins hat
    public IReadOnlyList<int> HomelandCells(int peopleIndex) => _homelands[peopleIndex];
}
