using System.Linq;
using RimWorld;
using Verse;

namespace Core40k;

public class DamageWorker_Holy : DamageWorker_AddInjury
{
    public override DamageResult Apply(DamageInfo dinfo, Thing victim)
    {
        var damageResult = new DamageResult
        {
            totalDamageDealt = 0
        };
            
        if (victim is not Pawn victimPawn)
        {
            return damageResult;
        }

        var defMod = def.GetModExtension<DefModExtension_HolyDamageExtension>();
        if (defMod == null)
        {
            return damageResult;
        }

        var instigatorFaction = dinfo.Instigator?.Faction;
        var hostileToWielder = instigatorFaction != null
            ? victimPawn.HostileTo(instigatorFaction)
            : victimPawn.HostileTo(Faction.OfPlayer);

        if (hostileToWielder)
        {
            var hitAmount = Rand.RangeInclusive(defMod.minHitAmount, defMod.maxHitAmount);
                
            var damageAmount = dinfo.Amount;
                
            for (var i = 0; i < hitAmount; i++)
            {
                if (victimPawn.Dead || victimPawn.Destroyed)
                {
                    break;
                }
                var extraDamage = new DamageInfo(dinfo.Def, damageAmount, 999f, dinfo.Angle,
                    dinfo.Instigator, null, dinfo.Weapon, dinfo.Category, dinfo.IntendedTarget);

                var hitResult = base.Apply(extraDamage, victimPawn);

                if (hitResult != null)
                {
                    damageResult.totalDamageDealt += hitResult.totalDamageDealt;
                    damageResult.deflected = hitResult.deflected;
                }
            }

            if (Rand.RangeInclusive(1, 100) <= defMod.chanceToIgnite)
            {
                victimPawn.TryAttachFire(1, dinfo.Instigator);
            }

            return damageResult;
        }

        var injuries = victimPawn.health?.hediffSet?.hediffs?.OfType<Hediff_Injury>().ToList();
        if (injuries.NullOrEmpty())
        {
            return damageResult;
        }

        var healAmount = dinfo.Amount * defMod.healPercentOfDamageToAllies / injuries.Count;

        foreach (var injury in injuries)
        {
            injury.Heal(healAmount);
        }
            
        return damageResult;
    }
}
