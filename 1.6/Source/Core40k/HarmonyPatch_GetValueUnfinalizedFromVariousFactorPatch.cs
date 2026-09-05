using HarmonyLib;
using RimWorld;
using Verse;

namespace Core40k;

[HarmonyPatch(typeof(StatWorker), "GetValueUnfinalized")]
public static class GetValueUnfinalizedFromVariousFactorPatch
{
    private static Game cachedGameForCoreUtils;
    private static GameComponent_CoreUtils coreUtils;

    private static GameComponent_CoreUtils CoreUtils => CoreUtilsFor();

    private static GameComponent_CoreUtils CoreUtilsFor()
    {
        if (coreUtils != null && cachedGameForCoreUtils == Current.Game)
        {
            return coreUtils;
        }

        cachedGameForCoreUtils = Current.Game;
        coreUtils = cachedGameForCoreUtils?.GetComponent<GameComponent_CoreUtils>();

        return coreUtils;
    }
    
    public static void Postfix(ref float __result, StatWorker __instance, StatRequest req)
    {
        if (req.Thing is not Pawn pawn)
        {
            return;
        }

        __result *= GetStatFactorForX(req, __instance, pawn);;
    }
    
    public static float GetStatFactorForX(StatRequest req, StatWorker statWorker, Pawn pawn)
    {
        var num = 1f;

        var coreUtilsComp = CoreUtils;
        if (coreUtilsComp == null)
        {
            return num;
        }

        if (coreUtilsComp.cachedDecoratives.TryGetValue(pawn, out var cachedDecoratives))
        {
            num *= FactorFrom(cachedDecoratives, statWorker.stat);
        }

        if (coreUtilsComp.cachedAlternateTexture.TryGetValue(pawn, out var cachedAlternateTexture))
        {
            num *= FactorFrom(cachedAlternateTexture, statWorker.stat);
        }
        
        return num;
    }

    private static float FactorFrom(GameComponent_CoreUtils.CachedDecoratives cached, StatDef stat)
    {
        var num = 1f;

        var apparelComps = cached.apparelComps;
        for (var i = 0; i < apparelComps.Count; i++)
        {
            num *= apparelComps[i].GetStatFactor(stat);
        }

        if (cached.weaponComp != null)
        {
            num *= cached.weaponComp.GetStatFactor(stat);
        }

        return num;
    }
}