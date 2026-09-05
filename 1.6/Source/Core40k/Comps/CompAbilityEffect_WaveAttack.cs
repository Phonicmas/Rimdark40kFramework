using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace Core40k;

public class CompAbilityEffect_WaveAttack : CompAbilityEffect_WithDuration
{
    private new CompProperties_AbilityWaveAttack Props => (CompProperties_AbilityWaveAttack)props;
        
    private List<IntVec3> tmpCells = [];
    private readonly HashSet<IntVec3> tmpCellSet = [];

    private Pawn Pawn => parent.pawn;

    public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
    {
        var affectedCells = AffectedCells(target);

        foreach (var vec3 in affectedCells.Where(vec3 => vec3.GetFirstPawn(parent.pawn.MapHeld) != null))
        {
            var pawnTarget = vec3.GetFirstPawn(parent.pawn.MapHeld);
                
            if (Props.stunTicks > 0)
            {
                pawnTarget.stances.stunner.StunFor(Props.stunTicks, Pawn);
            }
            if (Props.hediffDef != null)
            {
                ApplyHediff(pawnTarget, Pawn);
            }
        }

        base.Apply(target, dest);
    }

    private void ApplyHediff(Pawn target, Pawn other)
    {
        if (target == null)
        {
            return;
        }
        if (Props.replaceExisting)
        {
            var firstHediffOfDef = target.health.hediffSet.GetFirstHediffOfDef(Props.hediffDef);
            if (firstHediffOfDef != null)
            {
                target.health.RemoveHediff(firstHediffOfDef);
            }
        }
        var hediff = HediffMaker.MakeHediff(Props.hediffDef, target, Props.onlyBrain ? target.health.hediffSet.GetBrain() : null);
        var hediffComp_Disappears = hediff.TryGetComp<HediffComp_Disappears>();
        if (hediffComp_Disappears != null)
        {
            hediffComp_Disappears.ticksToDisappear = GetDurationSeconds(target).SecondsToTicks();
        }
        if (Props.severity >= 0f)
        {
            hediff.Severity = Props.severity;
        }
        var hediffComp_Link = hediff.TryGetComp<HediffComp_Link>();
        if (hediffComp_Link != null)
        {
            hediffComp_Link.other = other;
            hediffComp_Link.drawConnection = target == parent.pawn;
        }
        target.health.AddHediff(hediff);
    }

    public override IEnumerable<PreCastAction> GetPreCastActions()
    {
        yield return new PreCastAction
        {
            action = delegate (LocalTargetInfo a, LocalTargetInfo b)
            {
                parent.AddEffecterToMaintain(Props.effecterDef.Spawn(parent.pawn.Position, a.Cell, parent.pawn.Map), Pawn.Position, a.Cell, 17, Pawn.MapHeld);
            },
            ticksAwayFromCast = 17
        };
    }

    public override void DrawEffectPreview(LocalTargetInfo target)
    {
        GenDraw.DrawFieldEdges(AffectedCells(target));
    }

    public override bool AICanTargetNow(LocalTargetInfo target)
    {
        if (Pawn.Faction != null)
        {
            foreach (var item in AffectedCells(target))
            {
                var thingList = item.GetThingList(Pawn.Map);
                if (Enumerable.Any(thingList, t => t.Faction == Pawn.Faction))
                {
                    return false;
                }
            }
        }
        return true;
    }

    private List<IntVec3> AffectedCells(LocalTargetInfo target)
    {
        tmpCells.Clear();
        tmpCellSet.Clear();
        var vector = Pawn.Position.ToVector3Shifted().Yto0();
        var intVec = target.Cell.ClampInsideMap(Pawn.Map);
        if (Pawn.Position == intVec)
        {
            return tmpCells;
        }
        var lengthHorizontal = (intVec - Pawn.Position).LengthHorizontal;
        var num = (intVec.x - Pawn.Position.x) / lengthHorizontal;
        var num2 = (intVec.z - Pawn.Position.z) / lengthHorizontal;
        intVec.x = Mathf.RoundToInt(Pawn.Position.x + num * Props.range);
        intVec.z = Mathf.RoundToInt(Pawn.Position.z + num2 * Props.range);
        var target2 = Vector3.SignedAngle(intVec.ToVector3Shifted().Yto0() - vector, Vector3.right, Vector3.up);
        var num3 = Props.lineWidthEnd / 2f;
        var num4 = Mathf.Sqrt(Mathf.Pow((intVec - Pawn.Position).LengthHorizontal, 2f) + Mathf.Pow(num3, 2f));
        var num5 = 57.29578f * Mathf.Asin(num3 / num4);
        var num6 = GenRadial.NumCellsInRadius(Props.range);
        for (var i = 0; i < num6; i++)
        {
            var intVec2 = Pawn.Position + GenRadial.RadialPattern[i];
            if (CanUseCell(intVec2) && Mathf.Abs(Mathf.DeltaAngle(Vector3.SignedAngle(intVec2.ToVector3Shifted().Yto0() - vector, Vector3.right, Vector3.up), target2)) <= num5)
            {
                tmpCells.Add(intVec2);
                tmpCellSet.Add(intVec2);
            }
        }
        var list = GenSight.BresenhamCellsBetween(Pawn.Position, intVec);
        foreach (var intVec3 in list)
        {
            if (!tmpCellSet.Contains(intVec3) && CanUseCell(intVec3))
            {
                tmpCells.Add(intVec3);
                tmpCellSet.Add(intVec3);
            }
        }
        return tmpCells;
        bool CanUseCell(IntVec3 c)
        {
            if (!c.InBounds(Pawn.Map))
            {
                return false;
            }
            if (c == Pawn.Position)
            {
                return false;
            }
            if (c.Filled(Pawn.Map))
            {
                return false;
            }
            if (!c.InHorDistOf(Pawn.Position, Props.range))
            {
                return false;
            }
            return GenSight.LineOfSight(Pawn.Position, c, Pawn.Map, skipFirstCell: true);
        }
    }
        
}