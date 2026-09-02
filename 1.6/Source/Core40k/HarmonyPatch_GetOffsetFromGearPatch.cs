using HarmonyLib;
using RimWorld;
using Verse;

namespace Core40k;

[HarmonyPatch(typeof(StatWorker), "StatOffsetFromGear")]
public static class GetOffsetFromGearPatch
{
    public static void Postfix(ref float __result, Thing gear, StatDef stat)
    {
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
