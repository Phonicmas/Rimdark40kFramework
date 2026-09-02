using System.Collections.Generic;
using HarmonyLib;
using Verse;

namespace Core40k;

[HarmonyPatch(typeof(CompEquippable), "CompGetEquippedGizmosExtra")]
public static class AmmoChangerGizmo
{
    public static IEnumerable<Gizmo> Postfix(IEnumerable<Gizmo> __result, CompEquippable __instance)
    {
        foreach (Gizmo gizmo in __result)
        {
            yield return gizmo;
        }

        var comp = __instance.parent?.GetComp<Comp_AmmoChanger>();
        if (comp?.CurrentlySelectedProjectile == null)
        {
            yield break;
        }

        var gizmo_AmmoChanger = new Gizmo_AmmoChanger(comp)
        {
            defaultLabel = comp.Equippable?.PrimaryVerb?.verbProps?.defaultProjectile?.label
                           ?? comp.CurrentlySelectedProjectile.label,
        };

        yield return gizmo_AmmoChanger;
    }
}