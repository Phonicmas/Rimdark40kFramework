using System.Collections.Generic;
using Verse;

namespace Core40k;

public class WeightedSelection<T>
{
    private struct Entry
    {
        public double weight;
        public T item;
    }

    private readonly List<Entry> entries = [];
    private double totalWeight;

    public void AddEntry(T item, double weight)
    {
        if (weight <= 0)
        {
            return;
        }

        totalWeight += weight;
        entries.Add(new Entry { item = item, weight = weight });
    }

    //Verse.Rand rather than System.Random: seeded from the world, reproducible, and immune to the
    //"several instances constructed in the same millisecond share a seed" problem that made every
    //pawn in a raid roll the same result.
    private int PickIndex()
    {
        if (entries.Count == 0 || totalWeight <= 0)
        {
            return -1;
        }

        var roll = Rand.Range(0f, (float)totalWeight);
        var running = 0d;

        for (var i = 0; i < entries.Count; i++)
        {
            running += entries[i].weight;
            if (roll <= running)
            {
                return i;
            }
        }

        //Floating point can leave the roll a hair past the total.
        return entries.Count - 1;
    }

    public T GetRandom()
    {
        var index = PickIndex();
        return index < 0 ? default(T) : entries[index].item;
    }

    //Weights are stored raw and accumulated per draw, so removing an entry cannot leave the running
    //totals stale. The old version removed an entry without adjusting them, which skewed every
    //later draw and could fall through the loop and hand back null.
    public T GetRandomUnique()
    {
        var index = PickIndex();
        if (index < 0)
        {
            return default(T);
        }

        var picked = entries[index];
        entries.RemoveAt(index);
        totalWeight -= picked.weight;
        if (totalWeight < 0)
        {
            totalWeight = 0;
        }

        return picked.item;
    }

    public int Count => entries.Count;

    public bool NoEntriesOrNull()
    {
        return entries.NullOrEmpty();
    }
}
