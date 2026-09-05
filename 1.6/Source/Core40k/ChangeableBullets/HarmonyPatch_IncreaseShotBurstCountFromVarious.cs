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

        if (!WeaponVerbCompCache.TryGet(__instance.EquipmentSource, out var ammoChangerComp, out var weaponDecoComp))
        {
            return;
        }

        if (ammoChangerComp != null)
        {
            __result = ammoChangerComp.ShotsPerBurstOr(__result);
        }
        if (weaponDecoComp != null)
        {
            __result += weaponDecoComp.AdditionalBurstShotCount;
        }
    }
}
