using VEF.Abilities;
using Verse;

namespace Core40k;

public class Gene_GiveVEFAbility : Gene
{
    public override void PostAdd()
    {
        var comp = pawn.GetComp<CompAbilities>();
        if (comp != null)
        {
            var defModExtension = def.GetModExtension<DefModExtension_GivesVEFAbility>();
            if (!defModExtension?.abilityDefs.NullOrEmpty() ?? false)
            {
                foreach (var abilityDef in defModExtension.abilityDefs)
                {
                    comp.GiveAbility(abilityDef);
                }
            }
        }

        base.PostAdd();
    }

    public override void PostRemove()
    {
        var comp = pawn.GetComp<CompAbilities>();
        if (comp?.LearnedAbilities != null)
        {
            var defModExtension = def.GetModExtension<DefModExtension_GivesVEFAbility>();
            if (!defModExtension?.abilityDefs.NullOrEmpty() ?? false)
            {
                for (var i = comp.LearnedAbilities.Count - 1; i >= 0; i--)
                {
                    if (defModExtension.abilityDefs.Contains(comp.LearnedAbilities[i].def))
                    {
                        comp.LearnedAbilities.RemoveAt(i);
                    }
                }
            }
        }

        base.PostRemove();
    }
}