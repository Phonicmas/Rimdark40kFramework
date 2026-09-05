using HarmonyLib;
using Verse;

namespace Core40k;

[HarmonyPatch(typeof(Verb_LaunchProjectile), "Projectile", MethodType.Getter)]
public static class ChangeCurrentProjectile
{
    public static void Postfix(Verb_LaunchProjectile __instance, ref ThingDef __result)
    {
        if (WeaponVerbCompCache.TryGet(__instance?.EquipmentSource, out var ammoChangerComp, out _) && ammoChangerComp != null)
        {
            __result = ammoChangerComp.CurrentlySelectedProjectile;
        }
    }
}