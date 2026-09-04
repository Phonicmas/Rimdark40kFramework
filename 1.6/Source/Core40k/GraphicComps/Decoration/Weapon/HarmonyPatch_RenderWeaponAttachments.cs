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

        DrawDecorations(weapon, drawLoc, mesh, num, rotation);
    }

    private static bool loggedGroundDrawFailure;

    /// <summary>
    /// Draws a carried weapon's decorations. The ground texture is the held texture, so the held
    /// placement applies unchanged; only the item's ground rotation is added.
    /// </summary>
    public static void DrawOnGround(ThingWithComps weapon, Vector3 drawLoc)
    {
        try
        {
            DrawDecorations(weapon, drawLoc, MeshPool.plane10, GroundRotationUtility.GroundAngleFor(weapon), Rot4.South);
        }
        catch (Exception exception)
        {
            LogGroundFailure(weapon, exception);
        }
    }

    /// <summary>
    /// Prints a spawned weapon's decorations into the map mesh, the way the item itself is printed.
    /// </summary>
    public static void PrintOnGround(ThingWithComps weapon, SectionLayer layer)
    {
        try
        {
            var groundAngle = GroundRotationUtility.GroundAngleFor(weapon);
            var groundRotation = Quaternion.AngleAxis(groundAngle, Vector3.up);
            var sizeMult = weapon.MultipleItemsPerCellDrawn() ? 0.8f : 1f;
            var drawPos = weapon.DrawPos;

            foreach (var placement in Placements(weapon, Rot4.South))
            {
                var center = drawPos + groundRotation * (placement.offset * sizeMult);
                center.y += Mathf.Clamp(placement.layer, -MaxDecorationLayer, MaxDecorationLayer) * AltitudePerDecorationLayer;
                Printer_Plane.PrintPlane(layer, center, placement.drawSize * sizeMult, placement.material, groundAngle);
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

    private readonly struct Placement
    {
        public readonly Material material;
        public readonly Vector3 offset;
        public readonly Vector2 drawSize;
        public readonly float layer;

        public Placement(Material material, Vector3 offset, Vector2 drawSize, float layer)
        {
            this.material = material;
            this.offset = offset;
            this.drawSize = drawSize;
            this.layer = layer;
        }
    }

    private static IEnumerable<Placement> Placements(ThingWithComps weapon, Rot4 rotation)
    {
        var decoComp = weapon.GetComp<CompWeaponDecoration>();
        if (decoComp == null)
        {
            yield break;
        }

        foreach (var decoCompGraphic in decoComp.Graphics)
        {
            if (decoCompGraphic.Key is not WeaponDecorationDef weaponDecoration)
            {
                continue;
            }
            var material = decoCompGraphic.Value?.MatSingle;
            if (material == null)
            {
                continue;
            }

            var offset = Vector3.zero;
            var drawSize = decoCompGraphic.Key.drawSize;
            var layer = weaponDecoration.layerPlacement;
            if (weaponDecoration.weaponSpecificDrawData != null && weaponDecoration.weaponSpecificDrawData.TryGetValue(weapon.def.defName, out var value))
            {
                offset = value.OffsetForRot(rotation);
                drawSize *= value.scale;
                layer = value.LayerForRot(rotation, layer);
            }
            else if (decoCompGraphic.Key.drawData != null)
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

            yield return new Placement(material, offset, drawSize, layer);
        }
    }

    private static void DrawDecorations(ThingWithComps weapon, Vector3 drawLoc, Mesh mesh, float num, Rot4 rotation)
    {
        var quaternion = Quaternion.AngleAxis(num, Vector3.up);
        foreach (var placement in Placements(weapon, rotation))
        {
            var afterOffsetPos = drawLoc + quaternion * placement.offset;
            afterOffsetPos.y += Mathf.Clamp(placement.layer, -MaxDecorationLayer, MaxDecorationLayer) * AltitudePerDecorationLayer;

            var size = new Vector3(placement.drawSize.x, 0f, placement.drawSize.y);
            var matrix = Matrix4x4.TRS(s: size, pos: afterOffsetPos, q: quaternion);
            Graphics.DrawMesh(mesh, matrix, placement.material, 0);
        }
    }
}
