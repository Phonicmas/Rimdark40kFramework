using HarmonyLib;
using RimWorld;
using System.Collections.Generic;
using Verse;

namespace Core40k;

[HarmonyPatch(typeof(SteadyEnvironmentEffects), "FinalDeteriorationRate", [
    typeof(Thing),
    typeof(bool),
    typeof(bool),
    typeof(TerrainDef),
    typeof(List<string>)
], [
    ArgumentType.Normal,
    ArgumentType.Normal,
    ArgumentType.Normal,
    ArgumentType.Normal,
    ArgumentType.Normal
])]
public class StopDeterioationPatch
{
    public static void Postfix(ref float __result, Thing t, List<string> reasons)
    {
        var comp = t.TryGetComp<Comp_DeteriorateOutsideBuilding>();
        if (comp == null)
        {
            return;
        }
            
        if (comp.ShouldDeteriorate)
        {
            __result += comp.Props.deteriorationRateOutside;
            if (!reasons.NullOrEmpty())
            {
                reasons.Add("BEWH.Framework.Comp.ItemDeterioratingNotInContainer".Translate());
            }
        }
        else
        {
            __result = 0;
        }
    }    
}