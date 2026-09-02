using HarmonyLib;
using Verse;

namespace Core40k;

[HarmonyPatch(typeof(Verb), "WarmupTime", MethodType.Getter)]
public static class IncreaseWarmupTimeFromVarious
{
    public static void Postfix(Verb __instance, ref float __result)
    {
        //A weapon's melee tool verbs share its comps, so without this guard a bolt pistol's
        //bash verb was handed the ranged weapon's warmup, burst count and range.
        if (__instance?.verbProps == null || __instance.IsMeleeAttack)
        {
            return;
        }

        var equipment = __instance.EquipmentSource;
        if (!Core40kUtils.CanModifyVerbs(equipment))
        {
            return;
        }

        var ammoChangerComp = equipment.GetComp<Comp_AmmoChanger>();
        if (ammoChangerComp != null)
        {
            __result = ammoChangerComp.WarmupTimeOr(__result);
        }
        var weaponDecoComp = equipment.GetComp<CompWeaponDecoration>();
        //Decorations lazily creates the dictionary, so the old null test was never false and every
        //decorated weapon allocated one on the first read.
        if (weaponDecoComp?.Decorations is { Count: > 0 })
        {
            foreach (var weaponDecoration in weaponDecoComp.Decorations)
            {
                if (weaponDecoration.Key is not WeaponDecorationDef weaponDecorationDef)
                {
                    continue;
                }
                
                if (weaponDecorationDef.verbModifier != null)
                {
                    __result += weaponDecorationDef.verbModifier.additionalWarmupTime;
                }
            }
        }
    }
}
