using System;
using System.Numerics;

namespace TiefBerg.Simulation.Dice;

// Zustand eines RandomSource, für Spielstände (T-012). Gleicher Zustand -> gleiche Folge.
public readonly record struct RandomState(ulong S0, ulong S1, ulong S2, ulong S3);

// Seedbarer Zufallsgenerator (xoshiro256**) für den Simulationskern (Abschnitt 2, 7).
// Eigene Implementierung statt System.Random: Algorithmus ist über .NET-Versionen fest und der Zustand speicherbar.
// Nicht thread-sicher; parallele Arbeit bekommt eigene Instanzen über Derive. Reine Rechnung, daher kein Logger;
// geloggt wird jeder Wurf im DiceService.
public sealed class RandomSource
{
    private ulong _s0;
    private ulong _s1;
    private ulong _s2;
    private ulong _s3;

    private RandomSource(RandomState state)
    {
        SetState(state);
    }

    public static RandomSource FromSeed(ulong seed)
    {
        // SplitMix64 verteilt auch schlechte Seeds (0, 1, 2 …) gleichmäßig auf den Zustand
        ulong x = seed;
        return new RandomSource(new RandomState(SplitMix64(ref x), SplitMix64(ref x), SplitMix64(ref x),
            SplitMix64(ref x)));
    }

    public static RandomSource FromState(RandomState state) => new(state);

    // Unabhängiger Strom aus Weltseed und Schlüssel, unabhängig davon, wie oft vorher gewürfelt wurde.
    // Beispiel: Personen eines Ortes entstehen deterministisch aus Seed und Ort (Abschnitt 10).
    public static RandomSource Derive(ulong seed, ulong key)
    {
        ulong x = seed;
        ulong mixedSeed = SplitMix64(ref x) ^ key;
        return FromSeed(SplitMix64(ref mixedSeed));
    }

    public RandomState GetState() => new(_s0, _s1, _s2, _s3);

    public void SetState(RandomState state)
    {
        if ((state.S0 | state.S1 | state.S2 | state.S3) == 0)
        {
            // xoshiro liefert aus dem Nullzustand nur Nullen
            throw new ArgumentException("Zustand darf nicht vollständig 0 sein.", nameof(state));
        }

        _s0 = state.S0;
        _s1 = state.S1;
        _s2 = state.S2;
        _s3 = state.S3;
    }

    public ulong NextULong()
    {
        ulong result = BitOperations.RotateLeft(_s1 * 5, 7) * 9;
        ulong t = _s1 << 17;

        _s2 ^= _s0;
        _s3 ^= _s1;
        _s1 ^= _s2;
        _s0 ^= _s3;
        _s2 ^= t;
        _s3 = BitOperations.RotateLeft(_s3, 45);

        return result;
    }

    // Gleichverteilt in [0, exclusiveMax), ohne Modulo-Verzerrung (Lemire)
    public uint NextUInt(uint exclusiveMax)
    {
        if (exclusiveMax == 0)
        {
            throw new ArgumentOutOfRangeException(nameof(exclusiveMax), "Muss größer als 0 sein.");
        }

        ulong product = Next32() * (ulong)exclusiveMax;
        uint low = (uint)product;
        if (low < exclusiveMax)
        {
            uint threshold = (0u - exclusiveMax) % exclusiveMax;
            while (low < threshold)
            {
                product = Next32() * (ulong)exclusiveMax;
                low = (uint)product;
            }
        }

        return (uint)(product >> 32);
    }

    // Gleichverteilt in [0, 1)
    public double NextDouble() => (NextULong() >> 11) * (1.0 / (1UL << 53));

    // Obere Bits sind bei xoshiro256** die besten
    private ulong Next32() => NextULong() >> 32;

    private static ulong SplitMix64(ref ulong x)
    {
        ulong z = x += 0x9E3779B97F4A7C15;
        z = (z ^ (z >> 30)) * 0xBF58476D1CE4E5B9;
        z = (z ^ (z >> 27)) * 0x94D049BB133111EB;
        return z ^ (z >> 31);
    }
}
