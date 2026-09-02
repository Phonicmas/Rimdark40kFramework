using HarmonyLib;
using RimWorld;
using Verse;

namespace Core40k;

[HarmonyPatch(typeof(InteractionWorker_EnslaveAttempt), "Interacted")]
public class UnslaveablePatch
{
    public static bool Prefix(Pawn initiator, Pawn recipient)
    {
        if (recipient.genes == null)
        {
            return true;
        }
        if (recipient.Faction == Faction.OfPlayer)
        {
            return true;
        }
        foreach (var gene in recipient.genes.GenesListForReading)
        {
            if (!gene.def.HasModExtension<DefModExtension_SlaveabilityRecruitability>())
            {
                continue;
            }

            if (gene.def.GetModExtension<DefModExtension_SlaveabilityRecruitability>().canBeEnslaved)
            {
                continue;
            }
                    
            Find.LetterStack.ReceiveLetter("BEWH.Framework.Unbreakable.CannotEnslaveLetter".Translate(), "BEWH.Framework.Unbreakable.CannotEnslaveMessage".Translate(recipient.Named("PAWN"), initiator.Named("PAWN")), LetterDefOf.NeutralEvent);
            return false;
        }
        return true;
    }
}