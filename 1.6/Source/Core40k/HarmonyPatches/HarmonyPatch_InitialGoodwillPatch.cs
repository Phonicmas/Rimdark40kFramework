using HarmonyLib;
using RimWorld;

namespace Core40k;

[HarmonyPatch(typeof(Faction), "TryMakeInitialRelationsWith")]
public class InitialGoodwillPatch
{
    //Vanilla's own goodwill thresholds for deriving a relation kind.
    private const int HostileThreshold = -75;
    private const int AllyThreshold = 75;

    public static void Postfix(Faction other, Faction __instance)
    {
        if (__instance == null || other == null)
        {
            return;
        }

        DefModExtension_InitialGoodwill defMod;
        
        if (__instance.def.HasModExtension<DefModExtension_InitialGoodwill>())
        {
            defMod = __instance.def.GetModExtension<DefModExtension_InitialGoodwill>();
        }
        else if (other.def.HasModExtension<DefModExtension_InitialGoodwill>())
        {
            defMod = other.def.GetModExtension<DefModExtension_InitialGoodwill>();
        }
        else
        {
            return;
        }

        if ((__instance.IsPlayer || other.IsPlayer) && !defMod.applyToPlayer)
        {
            return;
        }

        if (defMod.onlyApplyToPlayer && !(__instance.IsPlayer || other.IsPlayer))
        {
            return;
        }

        SetGoodwill(__instance.RelationWith(other), defMod.initialGoodwill);
        SetGoodwill(other.RelationWith(__instance), defMod.initialGoodwill);
    }

    private static void SetGoodwill(FactionRelation relation, int goodwill)
    {
        if (relation == null)
        {
            return;
        }

        relation.baseGoodwill = goodwill;
        relation.kind = goodwill <= HostileThreshold
            ? FactionRelationKind.Hostile
            : goodwill >= AllyThreshold
                ? FactionRelationKind.Ally
                : FactionRelationKind.Neutral;
    }
}