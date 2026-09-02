using HarmonyLib;
using RimWorld;
using Verse;

namespace Core40k;

[HarmonyPatch(typeof(DamageWorker_AddInjury), "ChooseHitPart")]
public class BeheadingCutWorkerNormalPatch
{
    public static void Postfix(ref BodyPartRecord __result, DamageInfo dinfo, Pawn pawn)
    {
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