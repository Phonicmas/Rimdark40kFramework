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

    //The buff has to be renewed before it expires. With the old fixed 500 tick interval and the
    //default durationOutsideRange of 250, it ran out 250 ticks before every refresh and pawns
    //standing inside the aura watched it flicker on and off.
    private int RefreshIntervalTicks => Mathf.Max(30, Mathf.Min(500, Props.durationOutsideRange / 2));

    public override void CompTick()
    {
        base.CompTick();

        //Unspawned means worn, minified or inside a container: parent.Map is null and the radial
        //scan below would throw.
        if (!parent.Spawned)
        {
            return;
        }

        if (!parent.IsHashIntervalTick(RefreshIntervalTicks))
        {
            return;
        }

        var list = GenRadial.RadialDistinctThingsAround(parent.Position, parent.Map, Props.range, true).Where(thing => thing is Pawn pawn && pawn.Faction != null && pawn.Faction.IsPlayer);
            
        var things = list.ToList();
            
        if (things.NullOrEmpty())
        {
            return;
        }
            
        foreach (var thing in things)
        {
            if (!(thing is Pawn pawn))
            {
                continue;
            }

            var firstHediffOfDef = pawn.health.hediffSet.GetFirstHediffOfDef(Props.givesHediff);
            if (firstHediffOfDef != null)
            {
                //Refreshed in place rather than removed and re-added, which used to reset any
                //severity progression on the hediff on every cycle.
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

        //Only while the building is selected, matching how vanilla radius indicators behave. This
        //used to materialise a few hundred cells and rebuild the mesh on every frame, always.
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
