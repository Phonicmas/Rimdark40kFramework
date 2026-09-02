using System.Collections.Generic;
using HarmonyLib;
using Verse;

namespace Core40k;

[HarmonyPatch(typeof(CompEquippable))]
public static class WeaponDecorationToolsAndVerbs
{
    [HarmonyPostfix]
    [HarmonyPatch(nameof(CompEquippable.Tools), MethodType.Getter)]
    public static void ToolsPostfix(CompEquippable __instance, ref List<Tool> __result)
    {
        var weapon = __instance?.parent;
        if (weapon == null)
        {
            return;
        }

        var tools = weapon.GetComp<CompWeaponDecoration>()?.DecoratedTools;
        if (tools != null)
        {
            __result = tools;
        }

        var forceWeapon = weapon.GetComp<Comp_ForceWeapon>();
        if (forceWeapon != null)
        {
            __result = forceWeapon.ApplyExtraDamage(__result);
        }
    }

    [HarmonyPostfix]
    [HarmonyPatch(nameof(CompEquippable.VerbProperties), MethodType.Getter)]
    public static void VerbPropertiesPostfix(CompEquippable __instance, ref List<VerbProperties> __result)
    {
        var verbProperties = __instance.parent?.GetComp<CompWeaponDecoration>()?.DecoratedVerbProperties;
        if (verbProperties != null)
        {
            __result = verbProperties;
        }
    }
}
