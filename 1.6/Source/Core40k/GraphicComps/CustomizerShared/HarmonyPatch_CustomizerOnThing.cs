using HarmonyLib;
using RimWorld;
using System.Collections.Generic;
using Verse;
using Verse.AI;

namespace Core40k;

[HarmonyPatch(typeof(ThingWithComps), "GetFloatMenuOptions")]
public class CustomizerOnThing
{
    public static IEnumerable<FloatMenuOption> Postfix(IEnumerable<FloatMenuOption> __result, ThingWithComps __instance, Pawn selPawn)
    {
        foreach (var floatMenu in __result)
        {
            yield return floatMenu;
        }

        var defMod = __instance?.def?.GetModExtension<DefModExtension_AllowColoringOfThings>();
        if (defMod == null || selPawn == null)
        {
            yield break;
        }

        if (defMod.allowColoringOfApparel && selPawn.apparel?.WornApparel != null)
        {
            if (selPawn.apparel.WornApparel.Any(a => CustomizationTabResolver.HasAnyTab(a.def)))
            {
                var secondColourChangeFloatMenu = FloatMenuUtility.DecoratePrioritizedTask(new FloatMenuOption("BEWH.Framework.Customization.ArmourDecorationFeature".Translate().CapitalizeFirst(), delegate
                {
                    selPawn.jobs.TryTakeOrderedJob(JobMaker.MakeJob(Core40kDefOf.BEWH_OpenStylingStationDialogForApparelMultiColor, __instance), JobTag.Misc);
                }), selPawn, __instance);
                yield return secondColourChangeFloatMenu;
            }
        }

        //Equipment
        if (defMod.allowColoringOfEquipment && selPawn.equipment?.Primary != null)
        {   
            if (CustomizationTabResolver.HasAnyTab(selPawn.equipment.Primary.def))
            {
                var changeFloatMenu = FloatMenuUtility.DecoratePrioritizedTask(new FloatMenuOption("BEWH.Framework.Customization.WeaponDecorationFeature".Translate().CapitalizeFirst(), delegate
                {
                    selPawn.jobs.TryTakeOrderedJob(JobMaker.MakeJob(Core40kDefOf.BEWH_OpenStylingStationDialogForWeaponMultiColor, __instance), JobTag.Misc);
                }), selPawn, __instance);
                yield return changeFloatMenu;
            }
        }
    }
}