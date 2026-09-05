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
        if (eq is not ThingWithComps weapon || !Core40kUtils.DefHasComp<CompWeaponDecoration>(weapon.def))
        {
            return;
        }
        var decoComp = weapon.GetComp<CompWeaponDecoration>();
        if (decoComp == null)
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

        DrawDecorations(decoComp, drawLoc, mesh, num, rotation);
    }

    private static bool loggedGroundDrawFailure;

    /// <summary>
    /// Draws a carried weapon's decorations. The ground texture is the held texture, so the held
    /// placement applies unchanged; only the item's ground rotation is added.
    /// </summary>
    public static void DrawOnGround(CompWeaponDecoration decoComp, Vector3 drawLoc)
    {
        try
        {
            DrawDecorations(decoComp, drawLoc, MeshPool.plane10, GroundRotationUtility.GroundAngleFor(decoComp.parent), Rot4.South);
        }
        catch (Exception exception)
        {
            LogGroundFailure(decoComp.parent, exception);
        }
    }

    /// <summary>
    /// Prints a spawned weapon's decorations into the map mesh, the way the item itself is printed.
    /// </summary>
    public static void PrintOnGround(CompWeaponDecoration decoComp, SectionLayer layer)
    {
        var weapon = decoComp.parent;
        try
        {
            var groundAngle = GroundRotationUtility.GroundAngleFor(weapon);
            var groundRotation = Quaternion.AngleAxis(groundAngle, Vector3.up);
            var sizeMult = weapon.MultipleItemsPerCellDrawn() ? 0.8f : 1f;
            var drawPos = weapon.DrawPos;
            var baseLength = (weapon.Graphic?.drawSize.y ?? 1f) * sizeMult;

            foreach (var placement in decoComp.PlacementsFor(Rot4.South))
            {
                GroundDecorationRenderer.PrintOverBase(layer, drawPos, baseLength, groundAngle, groundRotation, placement.offset * sizeMult, placement.drawSize * sizeMult, placement.material, 0f, false, placement.layer);
            }
        }
        catch (Exception exception)
        {
            LogGroundFailure(weapon, exception);
        }
    }

    private static void LogGroundFailure(ThingWithComps weapon, Exception exception)
    {
        if (loggedGroundDrawFailure)
        {
            return;
        }

        loggedGroundDrawFailure = true;
        Log.Error("[Core40k] Failed to draw ground weapon decorations on " + (weapon?.def?.defName ?? "null") + ", further failures will not be logged: " + exception);
    }

    private static void DrawDecorations(CompWeaponDecoration decoComp, Vector3 drawLoc, Mesh mesh, float num, Rot4 rotation)
    {
        var placements = decoComp.PlacementsFor(rotation);
        if (placements.Count == 0)
        {
            return;
        }

        var quaternion = Quaternion.AngleAxis(num, Vector3.up);
        foreach (var placement in placements)
        {
            var afterOffsetPos = drawLoc + quaternion * placement.offset;
            afterOffsetPos.y += Mathf.Clamp(placement.layer, -MaxDecorationLayer, MaxDecorationLayer) * AltitudePerDecorationLayer;

            var size = new Vector3(placement.drawSize.x, 0f, placement.drawSize.y);
            var matrix = Matrix4x4.TRS(s: size, pos: afterOffsetPos, q: quaternion);
            Graphics.DrawMesh(mesh, matrix, placement.material, 0);
        }
    }
}
