using System;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace Core40k;

public class GameComponent_RankInfo : GameComponent
{
    public Dictionary<RankDef, int> rankLimits = new Dictionary<RankDef, int>();
    
    private bool eligibilityBaselineDone;
    
    private int cachedColonistCount = -1;
    private int cachedColonistCountTick = -1;

    public GameComponent_RankInfo(Game game)
    {
    }

    public override void GameComponentTick()
    {
        base.GameComponentTick();
        RankEligibilityNotifier.Tick();
    }

    public override void StartedNewGame()
    {
        base.StartedNewGame();
        eligibilityBaselineDone = true;
    }

    public override void LoadedGame()
    {
        base.LoadedGame();

        if (eligibilityBaselineDone)
        {
            return;
        }

        RankEligibilityNotifier.SeedBaseline();
        eligibilityBaselineDone = true;
    }

    public void PawnResetRanks(List<RankDef> unlockedRanks)
    {
        foreach (var rankDef in unlockedRanks.Where(rankDef => rankDef.colonyLimitOfRank.x > 0 || (rankDef.colonyLimitOfRank.x == 0 && rankDef.colonyLimitOfRank.y > 0)))
        {
            if (!rankLimits.ContainsKey(rankDef))
            {
                continue;
            }
            if (rankLimits[rankDef] == 1)
            {
                rankLimits.Remove(rankDef);
            }
            else
            {
                rankLimits[rankDef] -= 1;
            }
        }
    }

    public void PawnLostRank(RankDef rankDef)
    {
        if (!rankLimits.ContainsKey(rankDef))
        {
            return;
        }
        if (rankLimits[rankDef] == 1)
        {
            rankLimits.Remove(rankDef);
        }
        else
        {
            rankLimits[rankDef] -= 1;
        }
    }

    public void PawnGainedRank(RankDef rankDef)
    {
        if (rankLimits.ContainsKey(rankDef))
        {
            rankLimits[rankDef] += 1;
        }
        else
        {
            rankLimits.Add(rankDef, 1);
        }
    }
        
    public bool CanHaveMoreOfRank(RankDef rankDef)
    {
        var playerPawnAmount = GetColonistForCounting();
                
        var allowedAmount = rankDef.colonyLimitOfRank.y > 0 ? rankDef.colonyLimitOfRank.x + Math.Floor(playerPawnAmount/rankDef.colonyLimitOfRank.y) : rankDef.colonyLimitOfRank.x;
                
        var currentAmount = 0;

        if (rankLimits.ContainsKey(rankDef))
        {
            currentAmount = rankLimits.TryGetValue(rankDef);
        }

        return allowedAmount > currentAmount;
    }
        
    public (bool allowed, int allowedAmount, int currentAmount) CanHaveMoreOfRankWithInfo(RankDef rankDef)
    {
        var playerPawnAmount = GetColonistForCounting();
                
        var allowedAmount = (int)(rankDef.colonyLimitOfRank.y > 0 ? rankDef.colonyLimitOfRank.x + Math.Floor(playerPawnAmount/rankDef.colonyLimitOfRank.y) : rankDef.colonyLimitOfRank.x);
                
        var currentAmount = 0;

        if (rankLimits.ContainsKey(rankDef))
        {
            currentAmount = rankLimits.TryGetValue(rankDef);
        }

        return (allowedAmount > currentAmount, allowedAmount, currentAmount);
    }
        
    public int AllowedAmountOfRank(RankDef rankDef)
    {
        var playerPawnAmount = GetColonistForCounting();
                
        var allowedAmount = rankDef.colonyLimitOfRank.y > 0 ? rankDef.colonyLimitOfRank.x + Math.Floor(playerPawnAmount/rankDef.colonyLimitOfRank.y) : rankDef.colonyLimitOfRank.x;

        return (int)allowedAmount;
    }

    public int CurrentAmountOfRank(RankDef rankDef)
    {
        var currentAmount = 0;

        if (rankLimits.ContainsKey(rankDef))
        {
            currentAmount = rankLimits.TryGetValue(rankDef);
        }

        return currentAmount;
    }
        
    private int GetColonistForCounting()
    {
        var tickManager = Find.TickManager;
        var caching = tickManager != null && !tickManager.Paused;

        if (caching && cachedColonistCountTick == tickManager.TicksGame)
        {
            return cachedColonistCount;
        }

        var playerPawnAmount = 0;

        var maps = Find.Maps;
        for (var i = 0; i < maps.Count; i++)
        {
            playerPawnAmount += maps[i].mapPawns.ColonistCount;
        }

        var caravans = Find.WorldObjects.Caravans;
        for (var i = 0; i < caravans.Count; i++)
        {
            var caravan = caravans[i];
            if (!caravan.IsPlayerControlled)
            {
                continue;
            }

            var pawns = caravan.PawnsListForReading;
            for (var j = 0; j < pawns.Count; j++)
            {
                if (pawns[j].Faction is { IsPlayer: true })
                {
                    playerPawnAmount++;
                }
            }
        }

        if (caching)
        {
            cachedColonistCount = playerPawnAmount;
            cachedColonistCountTick = tickManager.TicksGame;
        }

        return playerPawnAmount;
    }
        
    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Collections.Look(ref rankLimits, "rankLimits");
        Scribe_Values.Look(ref eligibilityBaselineDone, "eligibilityBaselineDone", false);
    }
}