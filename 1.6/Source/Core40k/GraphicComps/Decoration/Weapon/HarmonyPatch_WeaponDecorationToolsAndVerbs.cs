using System.Collections.Generic;
using HarmonyLib;
using Verse;

namespace Core40k;

//Weapon decorations can grant extra tools (a bayonet) and extra verbs (an underbarrel launcher).
//CompEquippable reads both straight off the ThingDef, so the decorated lists get swapped in here.
//CompWeaponDecoration returns null when nothing about the weapon changed, which leaves the
//vanilla lists untouched.
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

        //Runs after the decoration swap so the scaled damage lands on the list that is actually
        //used, and always on a per instance copy rather than the shared def tools.
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
