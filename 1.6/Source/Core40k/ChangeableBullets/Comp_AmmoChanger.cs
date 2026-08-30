using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using UnityEngine;
using Verse;

namespace Core40k;

public class Comp_AmmoChanger : ThingComp
{
    public CompProperties_AmmoChanger Props => (CompProperties_AmmoChanger)props;

    public CompEquippable Equippable => parent.GetComp<CompEquippable>();
    public List<ThingDef> AvailableProjectiles => Props.availableProjectiles;

    public Pawn pawn => (parent?.ParentHolder as Pawn_EquipmentTracker)?.pawn;
    public ThingWithComps Weapon => parent;
    
    private ThingDef nextProjectile;
    private ThingDef currentlySelectedProjectile;
    public ThingDef CurrentlySelectedProjectile => currentlySelectedProjectile ??= AvailableProjectiles?.FirstOrFallback(def => HasResearchForAmmo(def, out _)) ?? Equippable?.PrimaryVerb?.verbProps?.defaultProjectile;

    //GetStatOffset and GetStatFactor are called very often, so the extension lookup is cached.
    private ThingDef cachedExtensionFor;
    private DefModExtension_AmmoChanger cachedExtension;

    public DefModExtension_AmmoChanger DefModExtensionAmmoChanger
    {
        get
        {
            var projectile = CurrentlySelectedProjectile;
            if (projectile == null)
            {
                return null;
            }

            if (cachedExtensionFor != projectile)
            {
                cachedExtensionFor = projectile;
                cachedExtension = projectile.GetModExtension<DefModExtension_AmmoChanger>();
            }

            return cachedExtension;
        }
    }

    public int ShotsPerBurst => DefModExtensionAmmoChanger?.shotsPerBurst ?? Weapon.def.Verbs.FirstOrDefault()?.burstShotCount ?? 0;
    public float EffectiveRange => DefModExtensionAmmoChanger?.effectiveRange ?? Weapon.def.Verbs.FirstOrDefault()?.range ?? 0;
    public float WarmupTime => DefModExtensionAmmoChanger?.warmupTime ?? Weapon.def.Verbs.FirstOrDefault()?.warmupTime ?? 0;
    
    public void LoadNextProjectile()
    {
        currentlySelectedProjectile = nextProjectile;
        nextProjectile = null;
        cachedExtensionFor = null;
    }

    public void SetNextProjectile(ThingDef projectile)
    {
        nextProjectile = projectile;
    }
    
    public bool HasResearchForAmmo(ThingDef ammoDef, out ResearchProjectDef researchDef)
    {
        if (!ammoDef.HasModExtension<DefModExtension_AmmoChanger>())
        {
            researchDef = null;
            return true;
        }

        var research = ammoDef.GetModExtension<DefModExtension_AmmoChanger>().unlockedBy;
        researchDef = research;
        return research?.IsFinished ?? true;
    }

    //Stat Offset
    public override float GetStatOffset(StatDef stat)
    {
        var statOffsets = DefModExtensionAmmoChanger?.statOffsets;
        return statOffsets.NullOrEmpty() ? 0f : statOffsets.GetStatOffsetFromList(stat);
    }

    //Stat Factor
    public override float GetStatFactor(StatDef stat)
    {
        var statFactors = DefModExtensionAmmoChanger?.statFactors;
        return statFactors.NullOrEmpty() ? 1f : statFactors.GetStatFactorFromList(stat);
    }

    public override void GetStatsExplanation(StatDef stat, StringBuilder sb, string whitespace = "")
    {
        var defModExtension = DefModExtensionAmmoChanger;
        if (defModExtension == null)
        {
            return;
        }

        var statOffset = defModExtension.statOffsets.NullOrEmpty() ? 0f : defModExtension.statOffsets.GetStatOffsetFromList(stat);
        var statFactor = defModExtension.statFactors.NullOrEmpty() ? 1f : defModExtension.statFactors.GetStatFactorFromList(stat);

        if (Mathf.Approximately(statOffset, 0f) && Mathf.Approximately(statFactor, 1f))
        {
            return;
        }

        sb.AppendLine(whitespace + "BEWH.Framework.StatReport.Ammo".Translate() + ":");

        if (!Mathf.Approximately(statOffset, 0f))
        {
            sb.AppendLine(whitespace + "    " + CurrentlySelectedProjectile.LabelCap + ": " + Core40kUtils.ValueToString(stat, statOffset, finalized: false, ToStringNumberSense.Offset));
        }

        if (!Mathf.Approximately(statFactor, 1f))
        {
            sb.AppendLine(whitespace + "    " + CurrentlySelectedProjectile.LabelCap + ": " + Core40kUtils.ValueToString(stat, statFactor, finalized: false, ToStringNumberSense.Factor));
        }
    }

    public override void PostExposeData()
    {
        Scribe_Defs.Look(ref currentlySelectedProjectile, "currentlySelectedProjectile");
        Scribe_Defs.Look(ref nextProjectile, "nextProjectile");
        cachedExtensionFor = null;
        base.PostExposeData();
    }
}
