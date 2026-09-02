using System;
using System.Collections.Generic;
using RimWorld;
using Verse;

namespace Core40k;

public class GameComponent_CoreUtils : GameComponent
{
    public Dictionary<Pawn, CachedDecoratives> cachedDecoratives = new ();
    
    public Dictionary<Pawn, CachedDecoratives> cachedAlternateTexture = new ();

    public Dictionary<(Pawn, Thing), bool> cachedGizmoToggles = new();

    private List<Pawn> gizmoTogglePawns;
    private List<Thing> gizmoToggleThings;
    private List<bool> gizmoToggleValues;
    
    public GameComponent_CoreUtils(Game game)
    {
    }

    public override void LoadedGame()
    {
        base.LoadedGame();
        VoidfaringUtility.ClearCache();
    }

    public override void StartedNewGame()
    {
        base.StartedNewGame();
        VoidfaringUtility.ClearCache();
    }

    public override void ExposeData()
    {
        base.ExposeData();

        if (Scribe.mode == LoadSaveMode.Saving)
        {
            gizmoTogglePawns = [];
            gizmoToggleThings = [];
            gizmoToggleValues = [];

            foreach (var toggle in cachedGizmoToggles)
            {
                var pawn = toggle.Key.Item1;
                var thing = toggle.Key.Item2;
                
                if (pawn == null || thing == null || pawn.Destroyed || thing.Destroyed)
                {
                    continue;
                }

                gizmoTogglePawns.Add(pawn);
                gizmoToggleThings.Add(thing);
                gizmoToggleValues.Add(toggle.Value);
            }
        }

        Scribe_Collections.Look(ref gizmoTogglePawns, "gizmoTogglePawns", LookMode.Reference);
        Scribe_Collections.Look(ref gizmoToggleThings, "gizmoToggleThings", LookMode.Reference);
        Scribe_Collections.Look(ref gizmoToggleValues, "gizmoToggleValues", LookMode.Value);

        if (Scribe.mode != LoadSaveMode.PostLoadInit)
        {
            return;
        }

        cachedGizmoToggles = new Dictionary<(Pawn, Thing), bool>();

        if (gizmoTogglePawns == null || gizmoToggleThings == null || gizmoToggleValues == null)
        {
            return;
        }

        var count = Math.Min(gizmoTogglePawns.Count, Math.Min(gizmoToggleThings.Count, gizmoToggleValues.Count));
        for (var i = 0; i < count; i++)
        {
            if (gizmoTogglePawns[i] == null || gizmoToggleThings[i] == null)
            {
                continue;
            }

            cachedGizmoToggles[(gizmoTogglePawns[i], gizmoToggleThings[i])] = gizmoToggleValues[i];
        }

        gizmoTogglePawns = null;
        gizmoToggleThings = null;
        gizmoToggleValues = null;
    }
    
    public class CachedDecoratives
    {
        public List<Apparel> apparels = [];
        public ThingWithComps weapon;
    }
}
