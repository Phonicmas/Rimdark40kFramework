using HarmonyLib;
using RimWorld;
using Verse;

namespace Core40k;

[HarmonyPatch(typeof(StatWorker), "GetValueUnfinalized")]
public static class GetValueUnfinalizedFromVariousFactorPatch
{
    //RankDef's pattern: these types outlive a game, so the cache has to be keyed on the game it
    //came from. A plain ??= keeps handing out the previous save's component after loading a second
    //save in the same session, which pins every pawn and item from the old game in memory and
    //answers every lookup from the wrong colony.
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
            var stat = statWorker.stat;
        
            //Apparel factor
            if (!cachedDecoratives.apparels.NullOrEmpty())
            {
                foreach (var apparel in cachedDecoratives.apparels)
                {
                    var compApparel = apparel.GetComp<CompDecorative>();
                    num *= compApparel.GetStatFactor(stat);
                }
            }

            //Weapon factor
            if (cachedDecoratives.weapon != null)
            {
                var compWeapon = cachedDecoratives.weapon.GetComp<CompWeaponDecoration>();
                num *= compWeapon.GetStatFactor(stat);
            }
        }

        if (coreUtilsComp.cachedAlternateTexture.TryGetValue(pawn, out var cachedAlternateTexture))
        {
            var stat = statWorker.stat;
        
            //Apparel factor
            if (!cachedAlternateTexture.apparels.NullOrEmpty())
            {
                foreach (var apparel in cachedAlternateTexture.apparels)
                {
                    var compApparel = apparel.GetComp<CompAlternateTexture>();
                    num *= compApparel.GetStatFactor(stat);
                }
            }

            //Weapon factor
            if (cachedAlternateTexture.weapon != null)
            {
                var compWeapon = cachedAlternateTexture.weapon.GetComp<CompAlternateTexture>();
                num *= compWeapon.GetStatFactor(stat);
            }
        }
        
        return num;
    }
}