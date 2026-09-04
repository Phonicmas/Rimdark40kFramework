using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace Core40k;

//TODO: Rename to CompArmorDecorative on 1.7?
public class CompDecorative : CompDecorativeBase
{
    private bool pawnKindDefSetupDone = false;
    public CompProperties_Decorative Props => (CompProperties_Decorative)props;
    public override void InitialSetup()
    {
        ApplyDecorationsFromList(Props.decorations, free: true);
        base.InitialSetup();
    }
    
    public override void Notify_Equipped(Pawn pawn)
    {
        if (!pawnKindDefSetupDone)
        {
            pawnKindDefSetupDone = true;

            Core40kUtils.SetupCustomizationForPawn(pawn, false, true);
        }
        base.Notify_Equipped(pawn);
    }

    private GroundDecorationRenderer.GroundCache groundCache;
    private bool groundCacheDirty = true;

    public GroundDecorationRenderer.GroundCache GroundCache
    {
        get
        {
            if (groundCacheDirty)
            {
                groundCacheDirty = false;
                groundCache = GroundDecorationRenderer.Build(this);
            }
            return groundCache;
        }
    }

    public override void Notify_ColorChanged()
    {
        groundCacheDirty = true;
        base.Notify_ColorChanged();
    }

    public override bool DontDrawParent()
    {
        return GroundDecorationRenderer.Enabled && GroundCache is { replacesParent: true };
    }

    public override void DrawAt(Vector3 drawLoc, bool flip = false)
    {
        if (!GroundDecorationRenderer.Enabled)
        {
            return;
        }
        GroundDecorationRenderer.Draw(this, drawLoc);
    }

    public override void PostPrintOnto(SectionLayer layer)
    {
        if (!GroundDecorationRenderer.Enabled)
        {
            return;
        }
        GroundDecorationRenderer.Print(this, layer);
    }

    public override void PostExposeData()
    {
        Scribe_Values.Look(ref pawnKindDefSetupDone, "pawnKindDefSetupDone");
        
        //TODO: Remove at later point
        Scribe_Collections.Look(ref originalExtraDecorations, "originalExtraDecorations");
        Scribe_Collections.Look(ref extraDecorations, "extraDecorations");
        //Either one on its own is still worth migrating: an old save where the item was never
        //opened in the customization dialog has decorations but no originals snapshot, and the "and"
        //here meant those were silently dropped on the next save.
        if (Scribe.mode == LoadSaveMode.PostLoadInit && (!extraDecorations.NullOrEmpty() || !originalExtraDecorations.NullOrEmpty()))
        {
            FixDecos();
        }
        base.PostExposeData();
    }
    
    [Obsolete]
    private Dictionary<ExtraDecorationDef, ExtraDecorationSettings> originalExtraDecorations = new ();
    [Obsolete]
    public Dictionary<ExtraDecorationDef, ExtraDecorationSettings> extraDecorations = new ();
    
    [Obsolete]
    private void FixDecos()
    {
        decorations ??= new Dictionary<DecorationDef, DecorationSettings>();
        originalDecorations ??= new Dictionary<DecorationDef, DecorationSettings>();
        foreach (var weapDecos in extraDecorations)
        {
            decorations.SetOrAdd(weapDecos.Key, weapDecos.Value);
        }
        foreach (var orgWeapDecos in originalExtraDecorations)
        {
            originalDecorations.SetOrAdd(orgWeapDecos.Key, orgWeapDecos.Value);
        }
        
        extraDecorations = new Dictionary<ExtraDecorationDef, ExtraDecorationSettings>();
        originalExtraDecorations = new Dictionary<ExtraDecorationDef, ExtraDecorationSettings>();
    }
}