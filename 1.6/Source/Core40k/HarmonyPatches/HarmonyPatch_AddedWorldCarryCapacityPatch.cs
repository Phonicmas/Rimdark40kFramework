using HarmonyLib;
using RimWorld;
using System.Linq;
using Verse;

namespace Core40k;

[HarmonyPatch(typeof(MassUtility), "Capacity")]
public class AddedWorldCarryCapacity
{
    public static void Postfix(ref float __result, Pawn p)
    {
        if (__result == 0)
        {
            return;
        }
        if (p.genes == null)
        {
            return;
        }
        var genes = p.genes.GenesListForReading;
        var num = 0f;
        for (var i = 0; i < genes.Count; i++)
        {
            var extension = genes[i].def.GetModExtension<DefModExtension_GeneExtension>();
            if (extension != null)
            {
                num += extension.addedWorldCarryCapacity;
            }
        }

        __result += num;
    }
}