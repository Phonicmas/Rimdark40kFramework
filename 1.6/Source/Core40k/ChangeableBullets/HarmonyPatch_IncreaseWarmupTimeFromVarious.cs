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

        var ammoChangerComp = __instance.EquipmentSource?.GetComp<Comp_AmmoChanger>();
        if (ammoChangerComp != null)
        {
            __result = ammoChangerComp.WarmupTimeOr(__result);
        }
        var weaponDecoComp = __instance.EquipmentSource?.GetComp<CompWeaponDecoration>();
        if (weaponDecoComp?.Decorations != null)
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
