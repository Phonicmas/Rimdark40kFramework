using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace Core40k
{
    public class CompAbilityEffect_AoeHit : CompAbilityEffect
    {
        public new CompProperties_AbilityAoeHit Props => (CompProperties_AbilityAoeHit)props;
        
        public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
        {
            var pawn = target.Pawn;
            if (pawn == null)
            {
                return;
            }
            
            if (Props.fleckDefTarget != null)
            {
                FleckMaker.AttachedOverlay(pawn, Props.fleckDefTarget, Vector3.zero);
            }
            
            var damageAmount = Props.damageAmount;

            if (Props.scaleStat != null)
            {
                var stat = parent.pawn.GetStatValue(Props.scaleStat) * Props.scaleFactor;
                damageAmount *= stat;
            }

            var dInfo = new DamageInfo(Props.damageDef, damageAmount, instigator: parent.pawn,
                weapon: parent.verb?.EquipmentSource?.def);
            
            pawn.TakeDamage(dInfo);
        }
        
        public override void PostApplied(List<LocalTargetInfo> targets, Map map)
        {
            if (Props.fleckDefLocation == null || map == null)
            {
                return;
            }

            if (!targets.NullOrEmpty())
            {
                foreach (var target in targets)
                {
                    FleckMaker.Static(target.Cell, map, Props.fleckDefLocation);
                }

                return;
            }

            if (parent.verb != null)
            {
                FleckMaker.Static(parent.verb.CurrentTarget.Cell, map, Props.fleckDefLocation);
            }
        }
    }
}