using System.Collections.Concurrent;
using HarmonyLib;
using Verse;

namespace Core40k;

[HarmonyPatch(typeof(PawnRenderNodeWorker), "GetGraphic")]
[HarmonyPriority(Priority.LowerThanNormal)]
public static class HideBodyPatch
{
    private static readonly ConcurrentDictionary<ThingDef, bool> hidesBodyCache = new();

    private static bool HidesBody(ThingDef def)
    {
        return hidesBodyCache.GetOrAdd(def, static thingDef => thingDef.GetModExtension<DefModExtension_ForcesBodyType>()?.hideBodyGraphic ?? false);
    }

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
        
        if (__instance.GetType() != typeof(PawnRenderNodeWorker_Body))
        {
            //Apparel for instance is a subclass of body and should not be targeted
            return;
        }
        
        //Humanlike check first: apparel is null on races without an apparel tracker, and evaluating
        //AnyApparel before the check that was meant to protect it NRE'd on every such pawn.
        if (!(parms.pawn.RaceProps?.Humanlike).GetValueOrDefault() || parms.pawn.apparel == null || !parms.pawn.apparel.AnyApparel)
        {
            //No clothes or not human, dont care
            return;
        }
        
        var wornApparel = parms.pawn.apparel.WornApparel;
        for (var i = 0; i < wornApparel.Count; i++)
        {
            if (HidesBody(wornApparel[i].def))
            {
                __result = Core40kUtils.EmptyMultiGraphic;
                return;
            }
        }
    }
}