using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace Core40k;

//One stack, and how much of it to take.
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

//Resource side of fitting decorations and upgrades.
//
//Materials come out of colony storage only - loose items lying around the map are ignored, the same
//way a bill at a workbench ignores them. The pawn hauls what the change needs to the station before
//starting work, carrying it in its inventory, and it is consumed from there when the work finishes.
//An interrupted refit therefore costs nothing: the materials are still in the pawn's inventory and
//get returned to storage by the usual haul behaviour.
//
//The price is checked when the change is accepted and again when it is committed, because the
//resources can disappear in between.
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

    //Candidates for paying a cost: in colony storage, not forbidden, reachable, nearest first.
    //Everything that prices, reserves or spends walks this same list, so the confirm dialog, the
    //haul queue and the final deduction can never disagree about what is available.
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

            //Storage only. A stack sitting on the floor outside a stockpile is not colony stock.
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

    private static int AvailableCount(Pawn pawn, ThingDef thingDef)
    {
        return Candidates(pawn, thingDef).Sum(thing => thing.stackCount);
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

    //All or nothing. Verifies the whole bill first so a half paid refit can never happen.
    //Used by the work-disabled path, where there is no job and therefore no hauling.
    public static bool Consume(Pawn pawn, List<ThingDefCountClass> cost)
    {
        if (cost.NullOrEmpty())
        {
            return true;
        }

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

    //Which stacks to haul, and how much from each. Null when storage cannot cover the whole bill,
    //so the caller can bail before the pawn walks anywhere.
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

    //Spend what the pawn hauled. All or nothing, same as Consume.
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
