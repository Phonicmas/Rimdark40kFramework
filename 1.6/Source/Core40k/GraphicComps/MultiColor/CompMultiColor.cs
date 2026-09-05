using RimWorld;
using UnityEngine;
using Verse;

namespace Core40k;

public class CompMultiColor : CompGraphicParent
{
    public CompProperties_MultiColor Props => (CompProperties_MultiColor)props; 
    
        
    private Color drawColorOne = Color.white;
    private Color originalColorOne = Color.white;

    public Color DrawColor
    {
        get => drawColorOne;
        set
        {
            drawColorOne = value.a == 0 ? Color.white : value;
            Notify_GraphicChanged();
        }
    }

    private Color drawColorTwo = Color.white;
    private Color originalColorTwo = Color.white;
    public Color DrawColorTwo
    {
        get => drawColorTwo;
        set
        {
            drawColorTwo = value.a == 0 ? drawColorOne : value;
            Notify_GraphicChanged();
        }
    }
    
    
    private Color drawColorThree = Color.white;
    private Color originalColorThree = Color.white;
    public Color DrawColorThree
    {
        get => drawColorThree;
        set
        {
            drawColorThree = value.a == 0 ? drawColorTwo : value;
            Notify_GraphicChanged();
        }
    }

    
    private MaskDef originalMaskDef;
    private MaskDef maskDef;
    public MaskDef MaskDef
    {
        get => maskDef;
        set
        {
            maskDef = value;
            Notify_GraphicChanged();
        } 
    }

    
    private bool recacheMultiGraphics = true;
    public bool RecacheMultiGraphics => recacheMultiGraphics;
    private Graphic_Multi cachedGraphicMulti;
    public Graphic_Multi CachedGraphicMulti
    {
        get => cachedGraphicMulti;
        set
        {
            cachedGraphicMulti = value;
            recacheMultiGraphics = false;
            if (IsApparel)
            {
                apparelGraphicRecord = new ApparelGraphicRecord(cachedGraphicMulti, parent as Apparel);
            }
        }
    }
    private ApparelGraphicRecord? apparelGraphicRecord;
    public ApparelGraphicRecord ApparelGraphicRecord
    {
        get
        {
            if (!IsApparel)
            {
                return new ApparelGraphicRecord(null, null);
            }
            apparelGraphicRecord ??= new ApparelGraphicRecord(CachedGraphicMulti, parent as Apparel);
            return apparelGraphicRecord.Value;
        }
    }
    
    private bool pawnKindDefSetupDone = false;

    private CompAlternateTexture AlternateTexture => parent?.GetComp<CompAlternateTexture>();
    
    private bool recacheSingleGraphics = true;
    public bool RecacheSingleGraphics => recacheSingleGraphics;
    private Graphic cachedGraphic;
    private Graphic cachedDefaultGraphic;
    public Graphic GetSingleGraphic(bool onlyDefaultGraphic = false)
    {
        if (onlyDefaultGraphic)
        {
            if (cachedDefaultGraphic != null)
            {
                return cachedDefaultGraphic;
            }
        }
        else
        {
            if (cachedGraphic != null)
            {
                return cachedGraphic;
            }
        }
        
        SetSingleGraphic(onlyDefaultGraphic);
        return GetSingleGraphic(onlyDefaultGraphic);
    }
    public void SetSingleGraphic(bool onlyDefaultGraphic = false)
    {
        recacheSingleGraphics = false;
        var path = onlyDefaultGraphic ? ThingDef.graphicData.texPath : AlternateTexture?.CurrentAlternateBaseForm?.drawnTextureIconPath ?? ThingDef.graphicData.texPath;
        var drawSize = AlternateTexture?.CurrentAlternateBaseForm?.newDrawSize ?? ThingDef.graphicData.drawSize;
        var shader = Core40kDefOf.BEWH_CutoutThreeColor.Shader;
        var drawMult = IsApparel ? 0.9f : 1f;
        var graphic = MultiColorUtils.GetGraphic<Graphic_Single>(path, shader, drawSize*drawMult, DrawColor, DrawColorTwo, DrawColorThree, null, maskDef?.maskPath);
        if (onlyDefaultGraphic)
        {
            cachedDefaultGraphic = new Graphic_RandomRotated(graphic, 35f);
        }
        else
        {
            cachedGraphic = new Graphic_RandomRotated(graphic, 35f);
        }
    }


    public void ResetFieldsByAlternateTexture(AlternateBaseFormDef alternateBaseFormDef)
    {
        if (alternateBaseFormDef.incompatibleMaskDefs.Contains(maskDef))
        {
            maskDef = Core40kDefOf.BEWH_DefaultMask;
        }

        if (alternateBaseFormDef.newPrimaryColor.HasValue)
        {
            DrawColor = alternateBaseFormDef.newPrimaryColor.Value;
        }
        if (alternateBaseFormDef.newSecondaryColor.HasValue)
        {
            DrawColorTwo = alternateBaseFormDef.newSecondaryColor.Value;
        }
        if (alternateBaseFormDef.newTertiaryColor.HasValue)
        {
            DrawColorThree = alternateBaseFormDef.newTertiaryColor.Value;
        }
    }
    
    public override void InitialSetup()
    {
        InitialColors();
        base.InitialSetup();
        recacheMultiGraphics = true;
    }

    public virtual void InitialColors()
    {
        drawColorOne = Props.defaultPrimaryColor ?? (ThingDef.MadeFromStuff ? ThingDef.GetColorForStuff(parent.Stuff) : Color.white);
        drawColorTwo = Props.defaultSecondaryColor ?? Color.white;
        drawColorThree = Props.defaultTertiaryColor ?? Color.white;
    }
    
    public void SetColors(Color colorOne, Color colorTwo, Color? colorThree)
    {
        drawColorOne = colorOne;
        drawColorTwo = colorTwo;
        drawColorThree = colorThree ?? Color.white;
    }
    
    public void SetColors(ColourPresetDef preset)
    {
        drawColorOne = preset.primaryColour;
        drawColorTwo = preset.secondaryColour;
        drawColorThree = preset.tertiaryColour ?? preset.secondaryColour;
    }

    public void SetDefaultColors()
    {
        drawColorOne = Props.defaultPrimaryColor ?? parent.DrawColor;
        drawColorTwo = Props.defaultSecondaryColor ?? parent.DrawColorTwo;
        drawColorThree = Props.defaultTertiaryColor ?? parent.DrawColorTwo;
    }
    
    public override void SetOriginals()
    {
        originalColorOne = drawColorOne;
        originalColorTwo = drawColorTwo;
        originalColorThree = drawColorThree;
        originalMaskDef = maskDef;
        Notify_GraphicChanged();
    }

    public override void Reset()
    {
        drawColorOne = originalColorOne;
        drawColorTwo = originalColorTwo;
        drawColorThree = originalColorThree;
        maskDef = originalMaskDef;
        Notify_GraphicChanged();
    }
    
    private Color pendingColorOne = Color.white;
    private Color pendingColorTwo = Color.white;
    private Color pendingColorThree = Color.white;
    private MaskDef pendingMaskDef;
    private bool hasPendingChange;

    public override bool HasEdits => HasAppearanceEdit;

    public override bool HasAppearanceEdit =>
        drawColorOne != originalColorOne
        || drawColorTwo != originalColorTwo
        || drawColorThree != originalColorThree
        || maskDef != originalMaskDef;

    public override bool HasPendingChange => hasPendingChange;

    public override bool PendingAppearanceChange => hasPendingChange;

    public override void CapturePending()
    {
        if (!HasAppearanceEdit)
        {
            return;
        }

        pendingColorOne = drawColorOne;
        pendingColorTwo = drawColorTwo;
        pendingColorThree = drawColorThree;
        pendingMaskDef = maskDef;
        hasPendingChange = true;

        Reset();
    }

    public override void CommitPending()
    {
        if (!hasPendingChange)
        {
            return;
        }

        drawColorOne = pendingColorOne;
        drawColorTwo = pendingColorTwo;
        drawColorThree = pendingColorThree;
        maskDef = pendingMaskDef;
        hasPendingChange = false;

        SetOriginals();
        Notify_GraphicChanged();
    }

    public override void DiscardPending()
    {
        hasPendingChange = false;
    }
    
    public override void Notify_GraphicChanged()
    {
        recacheMultiGraphics = true;
        recacheSingleGraphics = true;
        base.Notify_GraphicChanged();
    }
    
    public override void Notify_Equipped(Pawn pawn)
    {
        if (!pawnKindDefSetupDone)
        {
            pawnKindDefSetupDone = true;
            
            Core40kUtils.SetupCustomizationForThing(pawn, parent, true, false);
        }

        Notify_GraphicChanged();
        base.Notify_Equipped(pawn);
    }
    
    public override void PostExposeData()
    {
        Scribe_Values.Look(ref pawnKindDefSetupDone, "pawnKindDefSetupDone");
        Scribe_Values.Look(ref originalColorOne, "originalColorOne", Color.white);
        Scribe_Values.Look(ref originalColorTwo, "originalColorTwo", Color.white);
        Scribe_Values.Look(ref originalColorThree, "originalColorThree", Color.white);
        Scribe_Values.Look(ref drawColorOne, "drawColorOne", Color.white);
        Scribe_Values.Look(ref drawColorTwo, "drawColorTwo", Color.white);
        Scribe_Values.Look(ref drawColorThree, "drawColorThree", Color.white);
        Scribe_Defs.Look(ref originalMaskDef, "originalMaskDef");
        Scribe_Defs.Look(ref maskDef, "maskDef");
        Scribe_Values.Look(ref hasPendingChange, "hasPendingChange");
        Scribe_Values.Look(ref pendingColorOne, "pendingColorOne", Color.white);
        Scribe_Values.Look(ref pendingColorTwo, "pendingColorTwo", Color.white);
        Scribe_Values.Look(ref pendingColorThree, "pendingColorThree", Color.white);
        Scribe_Defs.Look(ref pendingMaskDef, "pendingMaskDef");
        
        base.PostExposeData();
        
        if (Scribe.mode == LoadSaveMode.PostLoadInit)
        {
            Notify_GraphicChanged();
        }
    }
}