using System.Collections.Generic;
using RimWorld;
using Verse;

namespace Core40k;

/// <summary>
/// Shared helpers for the voidfaring stats. Every one of them answers the same question:
/// "of the people aboard this thing, who is the best at X?"
///
/// Nothing in here references Odyssey or Save Our Ship 2 types, so it is always safe to call.
/// </summary>
public static class VoidfaringUtility
{
    // A gravship's range is read every frame while the player drags the destination cursor,
    // so the crew scan is cached rather than run per read.
    private const int CacheDurationTicks = 60;
    private const int MaxCacheEntries = 128;

    private static readonly Dictionary<(int thingId, ushort statIndex), (int tick, float value)> crewStatCache = new Dictionary<(int, ushort), (int, float)>();

    private static int lastSeenTick = -1;

    private static void ValidateCache(int currentTick)
    {
        // Loading a save resets thingIDNumber assignment, so a cache built in the previous
        // game must not be trusted. A tick going backwards is the cheap tell.
        if (currentTick < lastSeenTick || crewStatCache.Count > MaxCacheEntries)
        {
            crewStatCache.Clear();
        }
        lastSeenTick = currentTick;
    }

    public static void ClearCache()
    {
        crewStatCache.Clear();
        lastSeenTick = -1;
    }

    /// <summary>
    /// Best value of <paramref name="stat"/> among pawns standing on <paramref name="engine"/>'s
    /// valid substructure - i.e. the people who will actually be carried by the gravship.
    /// </summary>
    public static float BestGravshipCrewStat(Building_GravEngine engine, StatDef stat, float fallback, bool lowerIsBetter)
    {
        if (engine == null || stat == null || !engine.Spawned)
        {
            return fallback;
        }

        var map = engine.Map;
        if (map == null)
        {
            return fallback;
        }

        var currentTick = Find.TickManager?.TicksGame ?? 0;
        ValidateCache(currentTick);

        var key = (engine.thingIDNumber, stat.index);
        if (crewStatCache.TryGetValue(key, out var cached) && currentTick - cached.tick < CacheDurationTicks)
        {
            return cached.value;
        }

        var best = fallback;
        var owner = engine.Faction;

        // NoRegen deliberately: this runs inside stat evaluation, and the plain ValidSubstructure
        // getter would rebuild the gravship's section layers from there.
        var substructure = engine.ValidSubstructureNoRegen;

        foreach (var pawn in map.mapPawns.AllPawnsSpawned)
        {
            if (!IsCrew(pawn, owner))
            {
                continue;
            }
            if (!substructure.Contains(pawn.Position))
            {
                continue;
            }
            best = Best(best, pawn.GetStatValue(stat), lowerIsBetter);
        }

        crewStatCache[key] = (currentTick, best);
        return best;
    }

    /// <summary>
    /// Best value of <paramref name="stat"/> among the crew of a whole map. Used for ship maps,
    /// where the map itself is the vessel.
    /// </summary>
    public static float BestMapCrewStat(Map map, StatDef stat, float fallback, bool lowerIsBetter)
    {
        if (map == null || stat == null)
        {
            return fallback;
        }

        var currentTick = Find.TickManager?.TicksGame ?? 0;
        ValidateCache(currentTick);

        // Negated so map ids cannot collide with thing ids in the shared cache.
        var key = (-(map.uniqueID + 1), stat.index);
        if (crewStatCache.TryGetValue(key, out var cached) && currentTick - cached.tick < CacheDurationTicks)
        {
            return cached.value;
        }

        var best = fallback;
        var owner = map.ParentFaction;
        foreach (var pawn in map.mapPawns.AllPawnsSpawned)
        {
            if (!IsCrew(pawn, owner))
            {
                continue;
            }
            best = Best(best, pawn.GetStatValue(stat), lowerIsBetter);
        }

        crewStatCache[key] = (currentTick, best);
        return best;
    }

    private static float Best(float current, float candidate, bool lowerIsBetter)
    {
        if (lowerIsBetter)
        {
            return candidate < current ? candidate : current;
        }
        return candidate > current ? candidate : current;
    }

    /// <summary>
    /// A pawn counts as crew if they are alive, conscious and belong to whoever owns the vessel.
    /// Prisoners in the brig do not crew the ship they are locked inside.
    /// </summary>
    private static bool IsCrew(Pawn pawn, Faction owner)
    {
        if (pawn == null || pawn.Dead || pawn.Downed)
        {
            return false;
        }
        if (pawn.RaceProps == null || !pawn.RaceProps.Humanlike)
        {
            return false;
        }
        if (pawn.IsPrisoner)
        {
            return false;
        }
        if (owner != null && pawn.Faction != owner)
        {
            return false;
        }
        return true;
    }
}
