using RimWorld;
using Verse;

namespace Core40k;

public class PlaceWorker_OnlyOnePerMap : PlaceWorker
{
    public override AcceptanceReport AllowsPlacing(BuildableDef checkingDef, IntVec3 loc, Rot4 rot, Map map, Thing thingToIgnore = null, Thing thing = null)
    {
        if (checkingDef is not ThingDef thingDef || map == null)
        {
            return true;
        }

        if (map.listerBuildings.ColonistsHaveBuilding(thingDef))
        {
            return "BEWH.Framework.PlacementLimit.OnlyOneBuildingAllowedPerMap".Translate(thingDef.label.CapitalizeFirst());
        }

        foreach (var pendingDef in new[] { thingDef.blueprintDef, thingDef.frameDef })
        {
            if (pendingDef == null)
            {
                continue;
            }

            foreach (var pending in map.listerThings.ThingsOfDef(pendingDef))
            {
                if (pending != thingToIgnore && pending.Faction == Faction.OfPlayer)
                {
                    return "BEWH.Framework.PlacementLimit.OnlyOneBuildingAllowedPerMap".Translate(thingDef.label.CapitalizeFirst());
                }
            }
        }

        return true;
    }
}
