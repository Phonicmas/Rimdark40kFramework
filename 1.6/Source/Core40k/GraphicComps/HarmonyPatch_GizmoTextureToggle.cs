using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using RimWorld;
using Verse;

namespace Core40k;

[HarmonyPatch(typeof(Thing), "GetGizmos")]
public class GizmoTextureTogglePatch
{
    private static GameComponent_CoreUtils CoreUtils => Current.Game?.GetComponent<GameComponent_CoreUtils>();
    
    public static IEnumerable<Gizmo> Postfix(IEnumerable<Gizmo> __result, Thing __instance)
    {
        foreach (var floatMenu in __result)
        {
            yield return floatMenu;
        }
        
        var defMod = __instance?.def?.GetModExtension<DefModExtension_TextureFlags>();
        if (defMod == null)
        {
            yield break;
        }

        var toggleFlags = defMod.textureFlags.Where(flag => flag.gizmoActivated).ToList();
        if (toggleFlags.NullOrEmpty())
        {
            yield break;
        }
        
        var wearer = __instance.ParentHolder is not Pawn_ApparelTracker pawn_ApparelTracker ? null : pawn_ApparelTracker.pawn;
        var holder = __instance.ParentHolder is not Pawn_EquipmentTracker pawn_EquipmentTracker ? null : pawn_EquipmentTracker.pawn;

        var pawn = wearer ?? holder;

        var coreUtils = CoreUtils;
        if (pawn == null || coreUtils == null)
        {
            yield break;
        }

        var pair = (pawn, __instance);
        
        if (!coreUtils.cachedGizmoToggles.ContainsKey(pair))
        {
            coreUtils.cachedGizmoToggles.Add(pair, false);
        }
        
        foreach (var textureFlag in toggleFlags)
        {
            var gizmoOn = coreUtils.cachedGizmoToggles.TryGetValue(pair, out var on) && on;

            var toggleCommand = new Command_Toggle
            {
                defaultLabel = gizmoOn ? textureFlag.gizmoOnText : textureFlag.gizmoOffText,
                icon = Core40kUtils.FlippedIconTex,
                isActive = () => coreUtils.cachedGizmoToggles.TryGetValue(pair, out var active) && active,
                toggleAction = delegate
                {
                    var current = coreUtils.cachedGizmoToggles.TryGetValue(pair, out var active) && active;
                    coreUtils.cachedGizmoToggles[pair] = !current;
                    pawn.Drawer?.renderer?.SetAllGraphicsDirty();
                },
            };

            yield return toggleCommand;
        }
    }
}
