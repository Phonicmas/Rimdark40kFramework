using HarmonyLib;
using Verse;

namespace Core40k;

[HarmonyPatch(typeof(Verb), "EffectiveRange", MethodType.Getter)]
public static class IncreaseRangeFromVarious
{
    public static void Postfix(Verb __instance, ref float __result)
    {
        if (__instance?.verbProps == null || __instance.IsMeleeAttack)
        {
            return;
        }

        if (!WeaponVerbCompCache.TryGet(__instance.EquipmentSource, out var ammoChangerComp, out var weaponDecoComp))
        {
            return;
        }

        if (ammoChangerComp != null)
        {
            __result = ammoChangerComp.EffectiveRangeOr(__result);
        }
        if (weaponDecoComp != null)
        {
            __result += weaponDecoComp.AdditionalRange;
        }
    }
}
