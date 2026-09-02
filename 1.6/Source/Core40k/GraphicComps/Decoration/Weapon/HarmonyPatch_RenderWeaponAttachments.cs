using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace Core40k;

[HarmonyPatch(typeof(PawnRenderUtility), "DrawEquipmentAiming")]
public static class RenderWeaponAttachments
{
    private const float AltitudePerDecorationLayer = 1f / 2600f;
    private const float MaxDecorationLayer = 99f;

    private static bool loggedDrawFailure;

    static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase original)
    {
        var codeInstructions = instructions.ToList();

        if (!LocalsMatch(original))
        {
            Log.Warning("[Core40k] DrawEquipmentAiming no longer has the expected locals, so weapon decorations will not be drawn. The framework needs updating for this RimWorld version.");
            foreach (var codeInstruction in codeInstructions)
            {
                yield return codeInstruction;
            }
            yield break;
        }

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
        if (mesh == null)
        {
            return;
        }
        if (eq is not ThingWithComps weapon)
        {
            return;
        }
        Rot4 rotation;
        switch (weapon.ParentHolder)
        {
            case Pawn_EquipmentTracker { pawn: not null } equipmentTracker:
                rotation = equipmentTracker.pawn.Rotation;
                break;
            case Building_OutfitStand outfitStand:
                rotation = outfitStand.Rotation;
                break;
            default:
                return;
        }

        var decoComp = weapon.GetComp<CompWeaponDecoration>();

        if (decoComp == null)
        {
            return;
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
                offset = value.OffsetForRot(rotation);
                drawSize *= value.scale;
                layer = value.LayerForRot(rotation, layer);
            }
            else if(decoCompGraphic.Key.drawData != null)
            {
                offset = decoCompGraphic.Key.drawData.OffsetForRot(rotation);
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

            afterOffsetPos.y += Mathf.Clamp(layer, -MaxDecorationLayer, MaxDecorationLayer) * AltitudePerDecorationLayer;

            var size = new Vector3(drawSize.x, 0f, drawSize.y);
            
            var matrix = Matrix4x4.TRS(s: size, pos: afterOffsetPos, q: quaterion);
            Graphics.DrawMesh(mesh, matrix, material, 0);
        }
    }
}
