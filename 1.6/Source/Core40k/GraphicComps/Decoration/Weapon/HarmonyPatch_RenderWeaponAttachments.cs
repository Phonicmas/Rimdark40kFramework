using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using HarmonyLib;
using UnityEngine;
using Verse;

namespace Core40k;

[HarmonyPatch(typeof(PawnRenderUtility), "DrawEquipmentAiming")]
public static class RenderWeaponAttachments
{
    static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator iLGenerator)
    {
        var codeInstructions = instructions.ToList();
        foreach (var codeInstruction in codeInstructions)
        {
            if (codeInstruction.opcode == OpCodes.Ret)
            {
                yield return new CodeInstruction(OpCodes.Ldarg_0);
                yield return new CodeInstruction(OpCodes.Ldarg_1);
                yield return new CodeInstruction(OpCodes.Ldarg_2);
                yield return new CodeInstruction(OpCodes.Ldloc_0);
                yield return new CodeInstruction(OpCodes.Ldloc_1);
                yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(RenderWeaponAttachments), "DrawAttachments"));
            }
            yield return codeInstruction;
        }
    }

    private static void DrawAttachments(Thing eq, Vector3 drawLoc, float aimAngle, Mesh mesh, float num)
    {
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
            var material = graphic.MatSingle;

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
            var size = new Vector3(drawSize.x, 0f, drawSize.y);
            
            var matrix = Matrix4x4.TRS(s: size, pos: afterOffsetPos, q: quaterion);
            Graphics.DrawMesh(mesh, matrix, material, Mathf.RoundToInt(layer));
        }
    }
}