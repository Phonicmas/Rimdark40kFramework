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
        var tools = __instance.parent?.GetComp<CompWeaponDecoration>()?.DecoratedTools;
        if (tools != null)
        {
            __result = tools;
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
