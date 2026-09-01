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

    //Deferred changes.
    //
    //Accepting the customization dialog no longer applies anything straight away. The edited state
    //is snapshotted, the comp is rolled back to what it was, and the pawn works at the station for
    //the accumulated work amount. Only when that work finishes is the snapshot committed and the
    //resource cost taken.
    //
    //Two sets of members, because the numbers are needed at two different moments:
    //  Edit*    - live state versus committed state. Used to price the change in the confirm
    //             dialog, before anything has been captured.
    //  Pending* - the captured snapshot. Used by the job once the live state has been rolled back.

    //Live state differs from the committed state.
    public virtual bool HasEdits => false;

    //Live colour / mask / alternate form differs. Drives the flat per item appearance charge.
    public virtual bool HasAppearanceEdit => false;

    //Structural work (things added and removed) for the live edits. Excludes the appearance charge.
    public virtual float EditWork => 0f;

    public virtual void CollectEditCost(List<ThingDefCountClass> into)
    {
    }

    public virtual bool HasPendingChange => false;

    public virtual bool PendingAppearanceChange => false;

    public virtual float PendingWork => 0f;

    public virtual void CollectPendingCost(List<ThingDefCountClass> into)
    {
    }

    //Snapshot the live state and roll back to the committed state.
    public virtual void CapturePending()
    {
    }

    //Apply the snapshot for real.
    public virtual void CommitPending()
    {
    }

    //Throw the snapshot away, leaving the committed state untouched.
    public virtual void DiscardPending()
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