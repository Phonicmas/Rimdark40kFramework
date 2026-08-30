using HarmonyLib;
using Verse;

namespace Core40k;

[HarmonyPatch(typeof(PawnGenerator), nameof(PawnGenerator.GeneratePawn), typeof(PawnGenerationRequest))]
public static class PawnKindRanksPatch
{
    public static void Postfix(Pawn __result)
    {
        if (__result == null)
        {
            return;
        }

        RankUtils.TryGivePawnKindRanks(__result);
    }
}
