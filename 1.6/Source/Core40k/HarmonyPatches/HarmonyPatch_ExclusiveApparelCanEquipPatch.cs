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
    //cantReason is taken by ref, not out: this runs on every CanEquip call in the game and must
    //leave whatever reason vanilla (or an earlier patch) produced alone unless it is rejecting the
    //equip itself.
    public static void Postfix(Thing thing, Pawn pawn, ref string cantReason, ref bool __result)
    {
        var defMod = thing?.def?.GetModExtension<DefModExtension_ExclusiveApparel>();

        if (defMod?.requiredGene == null)
        {
            return;
        }

        if (pawn?.genes != null && pawn.genes.HasActiveGene(defMod.requiredGene))
        {
            return;
        }

        __result = false;
        cantReason = "BEWH.Framework.ExclusiveWear.MissingGene".Translate(defMod.requiredGene.LabelCap).CapitalizeFirst();
    }
}