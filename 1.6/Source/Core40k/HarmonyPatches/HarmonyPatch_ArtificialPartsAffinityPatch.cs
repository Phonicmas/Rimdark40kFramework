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
            var firstBionicOrImplant = Enumerable.FirstOrDefault(diffSet.hediffs, hediff => hediff.Part == part && hediff.def.countsAsAddedPartOrImplant);
            impactors?.Add(new PawnCapacityUtility.CapacityImpactorHediff
            {
                hediff = firstBionicOrImplant
            });
            __result *= factor;
        }
    }
}