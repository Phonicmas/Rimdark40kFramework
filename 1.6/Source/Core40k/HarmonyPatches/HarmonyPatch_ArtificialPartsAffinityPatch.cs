using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace Core40k;

[HarmonyPatch(typeof(PawnCapacityUtility), "CalculatePartEfficiency")]
public class ArtificialPartsAffinity
{
    public static void Postfix(ref float __result, HediffSet diffSet, BodyPartRecord part, ref List<PawnCapacityUtility.CapacityImpactor> impactors)
    {
        if (diffSet?.pawn == null || part == null)
        {
            return;
        }

        //CalculatePartEfficiency runs for every body part on every capacity recalculation, so the
        //stat is read once and a factor of exactly 1 leaves early. It used to push an impactor into
        //the health tab regardless, claiming an effect that was not there.
        var factor = diffSet.pawn.GetStatValue(Core40kDefOf.BEWH_ArtificialPartsAffinityFactor);
        if (Mathf.Approximately(factor, 1f))
        {
            return;
        }

        if (diffSet.HasDirectlyAddedPartFor(part))
        {
            var firstHediffMatchingPart = diffSet.GetFirstHediffMatchingPart<Hediff_AddedPart>(part);
            impactors?.Add(new PawnCapacityUtility.CapacityImpactorHediff
            {
                hediff = firstHediffMatchingPart
            });
            __result *= factor;
        }
        else if (diffSet.IsBionicOrImplant(part.def))
        {
            //Matched on the part record itself, not its def: on a pawn with one bionic arm, the
            //other arm's evaluation was being attributed to the wrong hediff.
            var firstBionicOrImplant = Enumerable.FirstOrDefault(diffSet.hediffs, hediff => hediff.Part == part && hediff.def.countsAsAddedPartOrImplant);
            impactors?.Add(new PawnCapacityUtility.CapacityImpactorHediff
            {
                hediff = firstBionicOrImplant
            });
            __result *= factor;
        }
    }
}