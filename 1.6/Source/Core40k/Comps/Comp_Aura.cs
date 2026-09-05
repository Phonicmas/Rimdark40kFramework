using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace Core40k;

[StaticConstructorOnStartup]
public class Comp_Aura : ThingComp
{
    private CompProperties_Aura Props => (CompProperties_Aura)props;

    private List<IntVec3> cachedAuraCells;
    private IntVec3 cachedAuraCentre = IntVec3.Invalid;
    
    private int RefreshIntervalTicks => Mathf.Max(30, Mathf.Min(500, Props.durationOutsideRange / 2));

    public override void CompTick()
    {
        base.CompTick();

        if (!parent.Spawned)
        {
            return;
        }

        if (!parent.IsHashIntervalTick(RefreshIntervalTicks))
        {
            return;
        }

        foreach (var thing in GenRadial.RadialDistinctThingsAround(parent.Position, parent.Map, Props.range, true))
        {
            if (thing is not Pawn { Faction.IsPlayer: true } pawn)
            {
                continue;
            }

            var firstHediffOfDef = pawn.health.hediffSet.GetFirstHediffOfDef(Props.givesHediff);
            if (firstHediffOfDef != null)
            {
                var existingDisappears = firstHediffOfDef.TryGetComp<HediffComp_Disappears>();
                if (existingDisappears != null)
                {
                    existingDisappears.ticksToDisappear = Props.durationOutsideRange;
                    continue;
                }

                pawn.health.RemoveHediff(firstHediffOfDef);
            }

            var hediff = HediffMaker.MakeHediff(Props.givesHediff, pawn, pawn.health.hediffSet.GetBrain());
            var hediffComp_Disappears = hediff.TryGetComp<HediffComp_Disappears>();
                
            if (hediffComp_Disappears != null)
            {
                hediffComp_Disappears.ticksToDisappear = Props.durationOutsideRange;
            }
            pawn.health.AddHediff(hediff);
        }
    }
        
    public override void PostDraw()
    {
        base.PostDraw();

        if (!parent.Spawned || Find.Selector == null || !Find.Selector.IsSelected(parent))
        {
            return;
        }

        if (cachedAuraCells == null || cachedAuraCentre != parent.Position)
        {
            cachedAuraCentre = parent.Position;
            cachedAuraCells = GenRadial.RadialCellsAround(cachedAuraCentre, 0, Props.range).ToList();
        }

        GenDraw.DrawFieldEdges(cachedAuraCells, Color.green);
    }
}
