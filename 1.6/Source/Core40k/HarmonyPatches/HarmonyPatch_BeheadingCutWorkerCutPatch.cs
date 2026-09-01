using HarmonyLib;
using RimWorld;
using Verse;

namespace Core40k;

[HarmonyPatch(typeof(DamageWorker_Cut), "ChooseHitPart")]
public class BeheadingCutWorkerCutPatch
{
    public static void Postfix(ref BodyPartRecord __result, DamageInfo dinfo, Pawn pawn)
    {
        //This runs for every injury in the game, so the cheap mod check goes first and __result is
        //never assumed to be non null - ChooseHitPart returns null when nothing matches its filter.
        var beheadingCut = dinfo.Weapon?.GetModExtension<DefModExtension_BeheadingCut>();
        if (beheadingCut == null)
        {
            return;
        }

        if (__result != null && __result.def == BodyPartDefOf.Neck)
        {
            return;
        }

        if (!Rand.Chance(beheadingCut.neckTargetingChance))
        {
            return;
        }

        var neck = pawn?.health?.hediffSet?.GetBodyPartRecord(BodyPartDefOf.Neck);
        if (neck != null)
        {
            __result = neck;
        }
    }
}