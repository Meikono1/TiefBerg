using System.Collections.Generic;

namespace TiefBerg.Data.World;

public enum ZoneKind
{
    Land,
    Sea,
    InlandWater,
}

public sealed record ZoneDefinition(byte Id, string NameKey, ZoneKind Kind);

// Zonentabelle aus data/world/klimazonen.yaml; die Bedeutung einer Zonen-ID steht nur hier, nie im Code
public sealed class ZoneTable
{
    private readonly ZoneDefinition?[] _byId = new ZoneDefinition?[256];

    public ZoneTable(IReadOnlyList<ZoneDefinition> zones)
    {
        Zones = zones;
        foreach (ZoneDefinition zone in zones)
        {
            _byId[zone.Id] = zone;
        }
    }

    public IReadOnlyList<ZoneDefinition> Zones { get; }

    public ZoneDefinition? Find(byte id) => _byId[id];

    public bool IsLand(byte id) => _byId[id]?.Kind == ZoneKind.Land;

    public bool IsSea(byte id) => _byId[id]?.Kind == ZoneKind.Sea;
}

// Seasonal = Trockenfluss: führt nur in der Regenzeit Wasser (lore/welt_konfiguration.md Abschnitt 7)
public sealed record RiverClass(byte Id, string NameKey, bool Seasonal, double MinDischargeM3S);

// Flussklassen aus data/world/fluesse.yaml; 0 im Raster = kein Fluss
public sealed class RiverTable
{
    private readonly RiverClass?[] _byId = new RiverClass?[256];

    public RiverTable(IReadOnlyList<RiverClass> classes)
    {
        Classes = classes;
        foreach (RiverClass river in classes)
        {
            _byId[river.Id] = river;
        }
    }

    public IReadOnlyList<RiverClass> Classes { get; }

    public RiverClass? Find(byte id) => _byId[id];

    // Fließt das ganze Jahr; Trockenflüsse zählen z. B. nicht für den Fluss-Bonus der Völker
    public bool IsFlowing(byte id) => _byId[id] is { Seasonal: false };
}
