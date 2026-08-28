using System.Linq;
using HarmonyLib;
using Verse;

namespace Core40k;

[HarmonyPatch(typeof(PawnRenderNodeWorker), "GetGraphic")]
[HarmonyPriority(Priority.LowerThanNormal)]
public static class HideBodyPatch
{
    public static void Postfix(PawnRenderNode node, PawnDrawParms parms, ref Graphic __result, PawnRenderNodeWorker __instance)
    {
        if ((parms.flags & PawnRenderFlags.Clothes) != PawnRenderFlags.Clothes)
        {
            //If param flags tell pawn to be unclothes then ignore aswell
            return;
        }
        
        if (__instance is not PawnRenderNodeWorker_Body)
        {
            //If not PawnRenderNodeWorker_Body then its not the body
            return;
        }
        
        if (__instance.GetType().IsSubclassOf(typeof(PawnRenderNodeWorker_Body)))
        {
            //Apparel for instance is a subclass of body and should not be targeted
            return;
        }
        
        if (!parms.pawn.apparel.AnyApparel || !(parms.pawn.RaceProps?.Humanlike).GetValueOrDefault())
        {
            //No clothes or not human, dont care
            return;
        }
        
        if (Enumerable.Any(parms.pawn.apparel.WornApparel, apparel => apparel.def.HasModExtension<DefModExtension_ForcesBodyType>() && apparel.def.GetModExtension<DefModExtension_ForcesBodyType>().hideBodyGraphic))
        {
            __result = Core40kUtils.EmptyMultiGraphic;
        }
    }
}