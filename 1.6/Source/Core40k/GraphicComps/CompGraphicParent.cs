using System.Collections.Generic;
using RimWorld;
using Verse;

namespace Core40k;

public class CompGraphicParent : ThingComp
{
    protected static GameComponent_CoreUtils coreUtils;
    protected static GameComponent_CoreUtils CoreUtils => coreUtils ??= Current.Game.GetComponent<GameComponent_CoreUtils>();
    
    protected Dictionary<StatDef, float> cachedStatOffset = new();
    public Dictionary<StatDef, float> CachedStatOffset => cachedStatOffset ??= new Dictionary<StatDef, float>();
    protected Dictionary<StatDef, float> cachedStatFactor = new();
    public Dictionary<StatDef, float> CachedStatFactor => cachedStatFactor ??= new Dictionary<StatDef, float>();
    
    protected ThingDef ThingDef => parent.def;
    protected Thing Thing => parent;
    
    protected bool IsApparel => parent is Apparel;

    protected Pawn Wearer => ParentHolder is not Pawn_ApparelTracker pawn_ApparelTracker ? null : pawn_ApparelTracker.pawn;
    protected Pawn Holder => ParentHolder is not Pawn_EquipmentTracker pawn_EquipmentTracker ? null : pawn_EquipmentTracker.pawn;

    protected Pawn Pawn => Wearer ?? Holder;

    private bool initialSet;

    public bool InitialSet
    {
        get => initialSet;
        set => initialSet = value;
    }
    
    public virtual void Notify_GraphicChanged()
    {
        cachedStatOffset = new Dictionary<StatDef, float>();
        cachedStatFactor = new Dictionary<StatDef, float>();
        parent.Notify_ColorChanged();
    }
    
    public virtual void SetOriginals()
    {
    }

    public virtual void Reset()
    {
    }
    
    public virtual void InitialSetup()
    {
        SetOriginals();
        initialSet = true;
    }
    
    public override void PostSpawnSetup(bool respawningAfterLoad)
    {
        base.PostSpawnSetup(respawningAfterLoad);
        if (initialSet)
        {
            return;
        }
        InitialSetup();
    }
    
    public override void PostExposeData()
    {
        Scribe_Values.Look(ref initialSet, "initialColourSet");
        base.PostExposeData();
    }
}