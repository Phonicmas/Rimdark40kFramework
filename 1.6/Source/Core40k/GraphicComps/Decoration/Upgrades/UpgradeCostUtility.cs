using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace Core40k;

public readonly struct ThingCountToHaul
{
    public readonly Thing Thing;
    public readonly int Count;

    public ThingCountToHaul(Thing thing, int count)
    {
        Thing = thing;
        Count = count;
    }
}

public static class UpgradeCostUtility
{
    public static void AddCost(List<ThingDefCountClass> into, List<ThingDefCountClass> cost)
    {
        if (cost.NullOrEmpty())
        {
            return;
        }

        foreach (var thingCount in cost)
        {
            if (thingCount?.thingDef == null || thingCount.count <= 0)
            {
                continue;
            }

            var existing = into.FirstOrDefault(entry => entry.thingDef == thingCount.thingDef);
            if (existing != null)
            {
                existing.count += thingCount.count;
                continue;
            }

            into.Add(new ThingDefCountClass(thingCount.thingDef, thingCount.count));
        }
    }

    private static List<Thing> Candidates(Pawn pawn, ThingDef thingDef)
    {
        var result = new List<Thing>();
        var map = pawn?.Map;
        if (map == null || thingDef == null)
        {
            return result;
        }

        foreach (var thing in map.listerThings.ThingsOfDef(thingDef))
        {
            if (thing.stackCount <= 0 || thing.IsForbidden(pawn))
            {
                continue;
            }
            
            if (!thing.IsInValidStorage())
            {
                continue;
            }

            if (!pawn.CanReach(thing, PathEndMode.ClosestTouch, Danger.Deadly))
            {
                continue;
            }

            result.Add(thing);
        }

        result.SortBy(thing => thing.Position.DistanceToSquared(pawn.Position));
        return result;
    }
    
    private static Pawn availableCachePawn;
    private static int availableCacheFrame = -1;
    private static readonly Dictionary<ThingDef, int> availableCache = new();

    public static void InvalidateAvailability()
    {
        availableCache.Clear();
        availableCacheFrame = -1;
        availableCachePawn = null;
    }

    private static int AvailableCount(Pawn pawn, ThingDef thingDef)
    {
        if (availableCacheFrame != Time.frameCount || availableCachePawn != pawn)
        {
            availableCache.Clear();
            availableCacheFrame = Time.frameCount;
            availableCachePawn = pawn;
        }

        if (availableCache.TryGetValue(thingDef, out var cached))
        {
            return cached;
        }

        var count = Candidates(pawn, thingDef).Sum(thing => thing.stackCount);
        availableCache.Add(thingDef, count);
        return count;
    }

    public static bool CanAfford(Pawn pawn, List<ThingDefCountClass> cost, out ThingDefCountClass missing)
    {
        missing = null;
        if (cost.NullOrEmpty())
        {
            return true;
        }

        foreach (var thingCount in cost)
        {
            var available = AvailableCount(pawn, thingCount.thingDef);
            if (available >= thingCount.count)
            {
                continue;
            }

            missing = new ThingDefCountClass(thingCount.thingDef, thingCount.count - available);
            return false;
        }

        return true;
    }

    public static bool CanAfford(Pawn pawn, List<ThingDefCountClass> cost)
    {
        return CanAfford(pawn, cost, out _);
    }

    public static bool Consume(Pawn pawn, List<ThingDefCountClass> cost)
    {
        if (cost.NullOrEmpty())
        {
            return true;
        }

        InvalidateAvailability();

        if (!CanAfford(pawn, cost))
        {
            return false;
        }

        foreach (var thingCount in cost)
        {
            var left = thingCount.count;
            foreach (var thing in Candidates(pawn, thingCount.thingDef))
            {
                if (left <= 0)
                {
                    break;
                }

                var take = Mathf.Min(left, thing.stackCount);
                thing.SplitOff(take).Destroy();
                left -= take;
            }
        }

        return true;
    }

    public static List<ThingCountToHaul> FindIngredients(Pawn pawn, List<ThingDefCountClass> cost)
    {
        var result = new List<ThingCountToHaul>();
        if (cost.NullOrEmpty())
        {
            return result;
        }

        foreach (var thingCount in cost)
        {
            var left = thingCount.count;
            foreach (var thing in Candidates(pawn, thingCount.thingDef))
            {
                if (left <= 0)
                {
                    break;
                }

                var take = Mathf.Min(left, thing.stackCount);
                result.Add(new ThingCountToHaul(thing, take));
                left -= take;
            }

            if (left > 0)
            {
                return null;
            }
        }

        return result;
    }

    public static bool ConsumeFromInventory(Pawn pawn, List<ThingDefCountClass> cost)
    {
        if (cost.NullOrEmpty())
        {
            return true;
        }

        var container = pawn?.inventory?.innerContainer;
        if (container == null)
        {
            return false;
        }

        foreach (var thingCount in cost)
        {
            var have = 0;
            foreach (var thing in container)
            {
                if (thing.def == thingCount.thingDef)
                {
                    have += thing.stackCount;
                }
            }

            if (have < thingCount.count)
            {
                return false;
            }
        }

        foreach (var thingCount in cost)
        {
            var left = thingCount.count;
            for (var i = container.Count - 1; i >= 0 && left > 0; i--)
            {
                var thing = container[i];
                if (thing.def != thingCount.thingDef)
                {
                    continue;
                }

                var take = Mathf.Min(left, thing.stackCount);
                thing.SplitOff(take).Destroy();
                left -= take;
            }
        }

        return true;
    }

    public static string CostToString(List<ThingDefCountClass> cost)
    {
        if (cost.NullOrEmpty())
        {
            return string.Empty;
        }

        return cost
            .Select(entry => "BEWH.Framework.Customization.CostLine".Translate(entry.thingDef.LabelCap, entry.count).ToString())
            .ToLineList();
    }
}
