using HarmonyLib;
using RimWorld;
using Verse;

namespace Core40k;

[HarmonyPatch(typeof(StatWorker), "StatOffsetFromGear")]
public static class GetOffsetFromGearPatch
{
    public static void Postfix(ref float __result, Thing gear, StatDef stat)
    {
        //Every CompGraphicParent on the item can contribute an offset - decorations, multi colour,
        //alternate base textures - so sum them all.
        //
        //This used to be TryGetComp<CompGraphicParent>() plus TryGetComp<CompMultiColor>(). Because
        //TryGetComp matches by assignability and returns the FIRST match, on an item whose comps
        //list CompMultiColor before CompDecorative the "decoration" lookup resolved to the multi
        //colour comp: its offset was added twice and the decoration's offsets were never read at
        //all. That is why a decoration granting, say, +0.4 Move Speed did nothing on such an item.
        if (gear is not ThingWithComps thingWithComps || thingWithComps.AllComps == null)
        {
            return;
        }

        foreach (var comp in thingWithComps.AllComps)
        {
            if (comp is CompGraphicParent graphicComp)
            {
                __result += graphicComp.GetStatOffset(stat);
            }
        }
    }
}
