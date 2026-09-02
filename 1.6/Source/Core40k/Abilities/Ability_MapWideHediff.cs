using System.Collections.Generic;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace Core40k;

public class Ability_MapWideHediff : VEF.Abilities.Ability
{
    private void AffectThings()
    {
        var defMod = def.GetModExtension<DefModExtension_MapWideHediff>();
        var map = CasterPawn?.Map;

        if (defMod?.hediffDef == null || map == null)
        {
            return;
        }

        var pawnsToAffect = new HashSet<Pawn>();
            
        if (defMod.affectEnemies)
        {
            foreach (var pawn in map.mapPawns.AllPawnsSpawned)
            {
                if (pawn.HostileTo(CasterPawn))
                {
                    pawnsToAffect.Add(pawn);
                }
            }
        }
            
        if (defMod.affectPlayerColonists)
        {
            foreach (var pawn in map.mapPawns.FreeColonistsSpawned)
            {
                pawnsToAffect.Add(pawn);
            }
        }
            
        if (!defMod.affectCaster)
        {
            pawnsToAffect.Remove(CasterPawn);
        }

        foreach (var affectedPawn in pawnsToAffect)
        {
            var hediffForPawn = HediffMaker.MakeHediff(defMod.hediffDef, affectedPawn);
                
            var hediffComp_Disappears = hediffForPawn.TryGetComp<HediffComp_Disappears>();
            if (hediffComp_Disappears != null)
            {
                hediffComp_Disappears.ticksToDisappear = def.durationTime;
            }
                
            affectedPawn.health.AddHediff(hediffForPawn);
        }
    }
        
    public override void Cast(params GlobalTargetInfo[] targets)
    {
        AffectThings();
        base.Cast(targets);
    }
}