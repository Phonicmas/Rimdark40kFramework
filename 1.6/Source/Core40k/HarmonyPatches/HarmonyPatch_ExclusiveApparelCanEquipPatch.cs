using HarmonyLib;
using RimWorld;
using Verse;

namespace Core40k;

[HarmonyPatch(typeof(EquipmentUtility), "CanEquip", [
    typeof(Thing),
    typeof(Pawn),
    typeof(string),
    typeof(bool),
], [
    ArgumentType.Normal,
    ArgumentType.Normal,
    ArgumentType.Out,
    ArgumentType.Normal,
])]
public class ExclusiveApparelCanEquipPatch
{
    public static void Postfix(Thing thing, Pawn pawn, out string cantReason, ref bool __result)
    {
        var defMod = thing.def.GetModExtension<DefModExtension_ExclusiveApparel>();

        if (defMod?.requiredGene == null)
        {
            cantReason = null;
            return;
        }

        if (pawn?.genes != null && pawn.genes.HasActiveGene(defMod.requiredGene))
        {
            cantReason = null;
            return;
        }

        __result = false;
        cantReason = "BEWH.Framework.ExclusiveWear.MissingGene".Translate(defMod.requiredGene.LabelCap).CapitalizeFirst();
    }
}