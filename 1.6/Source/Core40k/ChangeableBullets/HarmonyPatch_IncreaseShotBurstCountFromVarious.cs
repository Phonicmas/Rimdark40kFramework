using HarmonyLib;
using Verse;

namespace Core40k;

[HarmonyPatch(typeof(Verb), "BurstShotCount", MethodType.Getter)]
public static class IncreaseShotBurstCountFromVarious
{
    public static void Postfix(Verb __instance, ref int __result)
    {
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
            __result = ammoChangerComp.ShotsPerBurstOr(__result);
        }
        var weaponDecoComp = equipment.GetComp<CompWeaponDecoration>();
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
                    __result += weaponDecorationDef.verbModifier.additionalBurstShotCount;
                }
            }
        }
    }
}
