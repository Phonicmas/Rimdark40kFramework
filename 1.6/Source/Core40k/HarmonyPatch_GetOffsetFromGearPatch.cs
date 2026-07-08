using HarmonyLib;
using RimWorld;
using Verse;

namespace Core40k;

[HarmonyPatch(typeof(StatWorker), "StatOffsetFromGear")]
public static class GetOffsetFromGearPatch
{
    public static void Postfix(ref float __result, Thing gear, StatDef stat)
    {
        if (gear == null)
        {
            return;
        }
        var decoComp = gear.TryGetComp<CompGraphicParent>();
        var colorComp = gear.TryGetComp<CompMultiColor>();
        if (decoComp != null)
        {
            __result += decoComp.GetStatOffset(stat);
        }
        if (colorComp != null)
        {
            __result += colorComp.GetStatOffset(stat);
        }
    }
}