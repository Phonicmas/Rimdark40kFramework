using System;
using System.Collections.Generic;
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

    public override void PostExposeData()
    {
        Scribe_Values.Look(ref pawnKindDefSetupDone, "pawnKindDefSetupDone");
        
        //TODO: Remove at later point
        Scribe_Collections.Look(ref originalExtraDecorations, "originalExtraDecorations");
        Scribe_Collections.Look(ref extraDecorations, "extraDecorations");
        if (Scribe.mode == LoadSaveMode.PostLoadInit && !extraDecorations.NullOrEmpty() && !originalExtraDecorations.NullOrEmpty())
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