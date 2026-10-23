using System;
using System.Collections.Generic;

namespace TiefBerg.Simulation.Geography;

// Kürzester Weg über Land in Grad (lore/voelker/verbreitung.md: „Distanz über Land, kein Weg übers Meer“).
// Annahme: Ost-West-Schritte werden mit dem Kosinus der Breite kürzer, damit ein Grad Länge im Norden nicht so weit
// zählt wie am Äquator. Je Thread eine Instanz; der Abstandsspeicher wird wiederverwendet.
internal sealed class LandDistance
{
    private readonly bool[] _land;
    private readonly int _width;
    private readonly int _height;
    private readonly double _cellDegrees;
    private readonly double[] _eastWestStep;
    private readonly double[] _distance;
    private readonly List<int> _touched = new();
    private readonly PriorityQueue<int, double> _queue = new();

    public LandDistance(bool[] land, int width, int height)
    {
        _land = land;
        _width = width;
        _height = height;
        _cellDegrees = 180.0 / height;
        _eastWestStep = new double[height];
        for (int y = 0; y < height; y++)
        {
            double latitude = 90.0 - (y + 0.5) * _cellDegrees;
            _eastWestStep[y] = _cellDegrees * Math.Cos(latitude * Math.PI / 180.0);
        }

        _distance = new double[land.Length];
        Array.Fill(_distance, double.PositiveInfinity);
    }

    // Ruft visit(Zelle, Abstand) für jede Landzelle bis maxDegrees auf, in aufsteigender Entfernung
    public void Visit(int start, double maxDegrees, Action<int, double> visit)
    {
        Reset();
        if (!_land[start])
        {
            return;
        }

        Set(start, 0);
        _queue.Enqueue(start, 0);
        while (_queue.TryDequeue(out int cell, out double distance))
        {
            if (distance > _distance[cell])
            {
                continue;
            }

            visit(cell, distance);
            int y = cell / _width;
            int x = cell % _width;
            for (int dy = -1; dy <= 1; dy++)
            {
                int ny = y + dy;
                if (ny < 0 || ny >= _height)
                {
                    continue;
                }

                for (int dx = -1; dx <= 1; dx++)
                {
                    if (dx == 0 && dy == 0)
                    {
                        continue;
                    }

                    int neighbour = ny * _width + GridOperations.Wrap(x + dx, _width);
                    if (!_land[neighbour])
                    {
                        continue;
                    }

                    double eastWest = dx == 0 ? 0 : (_eastWestStep[y] + _eastWestStep[ny]) / 2;
                    double northSouth = dy == 0 ? 0 : _cellDegrees;
                    double next = distance + Math.Sqrt(eastWest * eastWest + northSouth * northSouth);
                    if (next <= maxDegrees && next < _distance[neighbour])
                    {
                        Set(neighbour, next);
                        _queue.Enqueue(neighbour, next);
                    }
                }
            }
        }
    }

    private void Set(int cell, double distance)
    {
        if (double.IsPositiveInfinity(_distance[cell]))
        {
            _touched.Add(cell);
        }

        _distance[cell] = distance;
    }

    private void Reset()
    {
        foreach (int cell in _touched)
        {
            _distance[cell] = double.PositiveInfinity;
        }

        _touched.Clear();
        _queue.Clear();
    }
}
