using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace Core40k;

[HarmonyPatch(typeof(StatDef), "PopulateMutableStats")]
public class PopulateMutableStatsWithRankStatsPatch
{
    public static void Postfix(ref HashSet<StatDef> ___mutableStats)
    {
        foreach (var rankDef in DefDatabase<RankDef>.AllDefsListForReading)
        {
            if (rankDef.statFactors != null)
            {
                ___mutableStats.AddRange(rankDef.statFactors.Select(mod => mod.stat));
            }
            if (rankDef.statOffsets != null)
            {
                ___mutableStats.AddRange(rankDef.statOffsets.Select(mod => mod.stat));
            }
                
            foreach (var conditionalStatAffecter in rankDef.conditionalStatAffecters)
            {
                if (conditionalStatAffecter.statFactors != null)
                {
                    ___mutableStats.AddRange(conditionalStatAffecter.statFactors.Select(mod => mod.stat));
                }
                if (conditionalStatAffecter.statOffsets != null)
                {
                    ___mutableStats.AddRange(conditionalStatAffecter.statOffsets.Select(mod => mod.stat));
                }
            }
        }
        
        foreach (var decorationDef in DefDatabase<DecorationDef>.AllDefsListForReading)
        {
            if (decorationDef.statFactors != null)
            {
                ___mutableStats.AddRange(decorationDef.statFactors.Select(mod => mod.stat));
            }
            if (decorationDef.statOffsets != null)
            {
                ___mutableStats.AddRange(decorationDef.statOffsets.Select(mod => mod.stat));
            }
                
            foreach (var conditionalStatAffecter in decorationDef.conditionalStatAffecters)
            {
                if (conditionalStatAffecter.statFactors != null)
                {
                    ___mutableStats.AddRange(conditionalStatAffecter.statFactors.Select(mod => mod.stat));
                }
                if (conditionalStatAffecter.statOffsets != null)
                {
                    ___mutableStats.AddRange(conditionalStatAffecter.statOffsets.Select(mod => mod.stat));
                }
            }
        }
    }
}