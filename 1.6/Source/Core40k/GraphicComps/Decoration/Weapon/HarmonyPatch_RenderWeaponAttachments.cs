using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using UnityEngine;
using Verse;

namespace Core40k;

[HarmonyPatch(typeof(PawnRenderUtility), "DrawEquipmentAiming")]
public static class RenderWeaponAttachments
{
    //One RimWorld altitude layer is 1/26th of a world unit. A decoration layer is a hundredth of
    //that, so even a deep stack stays well inside the weapon's own altitude band.
    private const float AltitudePerDecorationLayer = 1f / 2600f;
    private const float MaxDecorationLayer = 99f;

    private static bool loggedDrawFailure;

    static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase original)
    {
        var codeInstructions = instructions.ToList();

        //The injected call reads the mesh and the aim angle out of the method's own locals by slot.
        //Check the slots still hold what we expect rather than trusting the numbers: a compiler
        //change or another transpiler reordering them would otherwise produce invalid IL that only
        //fails once the method is JITed, which is a much worse failure than not drawing.
        if (!LocalsMatch(original))
        {
            Log.Warning("[Core40k] DrawEquipmentAiming no longer has the expected locals, so weapon decorations will not be drawn. The framework needs updating for this RimWorld version.");
            foreach (var codeInstruction in codeInstructions)
            {
                yield return codeInstruction;
            }
            yield break;
        }

        //Injected ahead of every return so whichever exit the method takes still draws the
        //attachments. Only one of them runs per call, so this does not draw twice.
        foreach (var codeInstruction in codeInstructions)
        {
            if (codeInstruction.opcode == OpCodes.Ret)
            {
                yield return new CodeInstruction(OpCodes.Ldarg_0);
                yield return new CodeInstruction(OpCodes.Ldarg_1);
                yield return new CodeInstruction(OpCodes.Ldarg_2);
                yield return new CodeInstruction(OpCodes.Ldloc_0);
                yield return new CodeInstruction(OpCodes.Ldloc_1);
                yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(RenderWeaponAttachments), nameof(DrawAttachments)));
            }
            yield return codeInstruction;
        }
    }

    private static bool LocalsMatch(MethodBase original)
    {
        var locals = original?.GetMethodBody()?.LocalVariables;
        if (locals == null || locals.Count < 2)
        {
            return false;
        }

        return locals[0].LocalType == typeof(Mesh) && locals[1].LocalType == typeof(float);
    }

    private static void DrawAttachments(Thing eq, Vector3 drawLoc, float aimAngle, Mesh mesh, float num)
    {
        //Anything thrown here escapes straight into PawnRenderer and takes the pawn's rendering with
        //it, every frame. Report it once and let the pawn keep drawing without its decorations.
        try
        {
            DrawAttachmentsInner(eq, drawLoc, mesh, num);
        }
        catch (Exception exception)
        {
            if (loggedDrawFailure)
            {
                return;
            }

            loggedDrawFailure = true;
            Log.Error("[Core40k] Failed to draw weapon decorations on " + (eq?.def?.defName ?? "null") + ", further failures will not be logged: " + exception);
        }
    }

    private static void DrawAttachmentsInner(Thing eq, Vector3 drawLoc, Mesh mesh, float num)
    {
        //DrawEquipmentAiming can return before the mesh local is assigned.
        if (mesh == null)
        {
            return;
        }
        if (eq is not ThingWithComps weapon)
        {
            return;
        }
        if (weapon.ParentHolder is not Pawn_EquipmentTracker equipmentTracker)
        {
            return;
        }
        if (equipmentTracker.pawn == null)
        {
            return;
        }
        var decoComp = weapon.GetComp<CompWeaponDecoration>();

        if (decoComp == null)
        {
            return;
        }
        
        if (decoComp.recacheGraphics)
        {
            decoComp.RecacheDecorationGraphics();
        }
        
        foreach (var decoCompGraphic in decoComp.Graphics)
        {
            if (decoCompGraphic.Key is not WeaponDecorationDef weaponDecoration)
            {
                continue;
            }
            var graphic = decoCompGraphic.Value;
            var material = graphic?.MatSingle;
            if (material == null)
            {
                continue;
            }

            var offset = Vector3.zero;
            var drawSize = decoCompGraphic.Key.drawSize;
            var layer = weaponDecoration.layerPlacement;
            if (weaponDecoration.weaponSpecificDrawData != null && weaponDecoration.weaponSpecificDrawData.TryGetValue(eq.def.defName, out var value))
            {
                offset = value.OffsetForRot(equipmentTracker.pawn.Rotation);
                drawSize *= value.scale;
                layer = value.LayerForRot(equipmentTracker.pawn.Rotation, layer);
            }
            else if(decoCompGraphic.Key.drawData != null)
            {
                offset = decoCompGraphic.Key.drawData.OffsetForRot(equipmentTracker.pawn.Rotation);
                drawSize *= decoCompGraphic.Key.drawData.scale;
            }

            if (decoComp.drawDatas.TryGetValue(weaponDecoration, out var drawData))
            {
                offset += drawData.defaultData.offset;
                drawSize *= drawData.defaultData.scale;
                layer += drawData.defaultData.layer;
            }
            
            var quaterion = Quaternion.AngleAxis(num, Vector3.up);
            var afterOffsetPos = drawLoc + quaterion * offset;

            //Draw order on RimWorld's top down camera comes from the y component, the same way the
            //armour side gets it from PawnRenderNodeWorker.LayerFor. The fourth argument of
            //Graphics.DrawMesh is Unity's layer mask index (0-31), not a sort order: the accumulated
            //layer used to go there, so it never affected ordering, and any value above 31 made
            //Unity reject the call outright.
            afterOffsetPos.y += Mathf.Clamp(layer, -MaxDecorationLayer, MaxDecorationLayer) * AltitudePerDecorationLayer;

            var size = new Vector3(drawSize.x, 0f, drawSize.y);
            
            var matrix = Matrix4x4.TRS(s: size, pos: afterOffsetPos, q: quaterion);
            Graphics.DrawMesh(mesh, matrix, material, 0);
        }
    }
}
