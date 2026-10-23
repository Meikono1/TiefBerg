using System;

namespace TiefBerg.Simulation.Geography;

// Rasteroperationen auf Weltkarten: Spalten laufen an der Datumsgrenze um, Zeilen enden an den Polen
internal static class GridOperations
{
    private const float Diagonal = 1.41421356f;

    // Gauß-Weichzeichnen, getrennt nach Zeilen und Spalten; Radius 3 Sigma
    public static float[] GaussianBlur(float[] values, int width, int height, double sigma)
    {
        int radius = (int)Math.Ceiling(sigma * 3);
        var kernel = new float[radius * 2 + 1];
        float sum = 0;
        for (int i = -radius; i <= radius; i++)
        {
            kernel[i + radius] = (float)Math.Exp(-(i * i) / (2 * sigma * sigma));
            sum += kernel[i + radius];
        }

        for (int i = 0; i < kernel.Length; i++)
        {
            kernel[i] /= sum;
        }

        var horizontal = new float[values.Length];
        for (int y = 0; y < height; y++)
        {
            int row = y * width;
            for (int x = 0; x < width; x++)
            {
                float value = 0;
                for (int k = -radius; k <= radius; k++)
                {
                    value += kernel[k + radius] * values[row + Wrap(x + k, width)];
                }

                horizontal[row + x] = value;
            }
        }

        var result = new float[values.Length];
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                float value = 0;
                for (int k = -radius; k <= radius; k++)
                {
                    int sy = Math.Clamp(y + k, 0, height - 1);
                    value += kernel[k + radius] * horizontal[sy * width + x];
                }

                result[y * width + x] = value;
            }
        }

        return result;
    }

    // Abstand in Zellen zur nächsten Quellzelle (Näherung über 8 Nachbarn, diagonal √2).
    // Zwei Durchläufe je Richtung, damit Abstände auch über die Datumsgrenze hinweg ankommen.
    public static float[] DistanceToNearest(bool[] sources, int width, int height)
    {
        var distance = new float[sources.Length];
        for (int i = 0; i < distance.Length; i++)
        {
            distance[i] = sources[i] ? 0f : float.MaxValue;
        }

        for (int round = 0; round < 2; round++)
        {
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    Relax(distance, width, height, x, y, -1, 0, 1f);
                    Relax(distance, width, height, x, y, -1, -1, Diagonal);
                    Relax(distance, width, height, x, y, 0, -1, 1f);
                    Relax(distance, width, height, x, y, 1, -1, Diagonal);
                }
            }

            for (int y = height - 1; y >= 0; y--)
            {
                for (int x = width - 1; x >= 0; x--)
                {
                    Relax(distance, width, height, x, y, 1, 0, 1f);
                    Relax(distance, width, height, x, y, 1, 1, Diagonal);
                    Relax(distance, width, height, x, y, 0, 1, 1f);
                    Relax(distance, width, height, x, y, -1, 1, Diagonal);
                }
            }
        }

        return distance;
    }

    public static int Wrap(int x, int width) => ((x % width) + width) % width;

    private static void Relax(float[] distance, int width, int height, int x, int y, int dx, int dy, float step)
    {
        int ny = y + dy;
        if (ny < 0 || ny >= height)
        {
            return;
        }

        float neighbour = distance[ny * width + Wrap(x + dx, width)];
        if (neighbour == float.MaxValue)
        {
            return;
        }

        int index = y * width + x;
        if (neighbour + step < distance[index])
        {
            distance[index] = neighbour + step;
        }
    }
}
