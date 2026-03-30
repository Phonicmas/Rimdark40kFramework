using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace Core40k;

public class CompMultiColor : CompGraphicParent
{
    public void TempSetInitialValues(ApparelMultiColor multiColor)
    {
        drawColorOne = multiColor.DrawColor;
        originalColorOne = multiColor.DrawColor;
        
        drawColorTwo = multiColor.DrawColorTwo;
        originalColorTwo = multiColor.DrawColorTwo;
        
        drawColorThree = multiColor.DrawColorThree;
        originalColorThree = multiColor.DrawColorThree;
        
        maskDef = multiColor.MaskDef;
        originalMaskDef = multiColor.MaskDef;
    }
    
    public void TempSetInitialValues(WeaponMultiColor multiColor)
    {
        drawColorOne = multiColor.DrawColor;
        originalColorOne = multiColor.DrawColor;
        
        drawColorTwo = multiColor.DrawColorTwo;
        originalColorTwo = multiColor.DrawColorTwo;
        
        drawColorThree = multiColor.DrawColorThree;
        originalColorThree = multiColor.DrawColorThree;
    }
    
    public CompProperties_MultiColor Props => (CompProperties_MultiColor)props; 

    private ThingDef thingDef => parent.def;
    private Thing thing => parent;
    public Pawn Wearer
    {
        get
        {
            if (ParentHolder is not Pawn_ApparelTracker pawn_ApparelTracker)
            {
                return null;
            }
            return pawn_ApparelTracker.pawn;
        }
    }
    
    private bool isApparel => parent is Apparel;
    
    private BodyTypeDef originalBodyType = null;
        
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
            if (isApparel)
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
            if (!isApparel)
            {
                return new ApparelGraphicRecord(null, null);
            }
            apparelGraphicRecord ??= new ApparelGraphicRecord(CachedGraphicMulti, parent as Apparel);
            return apparelGraphicRecord.Value;
        }
    }
    
    
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
        var path = onlyDefaultGraphic ? thingDef.graphicData.texPath : currentAlternateBaseForm?.drawnTextureIconPath ?? thingDef.graphicData.texPath;
        var drawSize = currentAlternateBaseForm?.newDrawSize ?? thingDef.graphicData.drawSize;
        var shader = Core40kDefOf.BEWH_CutoutThreeColor.Shader;
        var drawMult = isApparel ? 0.9f : 1f;
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
    
    
    private AlternateBaseFormDef originalCurrentAlternateBaseForm = null;
    private AlternateBaseFormDef currentAlternateBaseForm = null;
    public AlternateBaseFormDef CurrentAlternateBaseForm => currentAlternateBaseForm;

    public void SetAlternateBaseForm(AlternateBaseFormDef alternateBaseFormDef, bool isForApparel)
    {
        if (isForApparel)
        {
            var compArmorDeco = thing.TryGetComp<CompDecorative>();
            compArmorDeco?.RemoveDecorationsIncompatibleWithAlternate(alternateBaseFormDef);
        }
        else
        {
            var compWeaponDeco = thing.TryGetComp<CompWeaponDecoration>();
            compWeaponDeco?.RemoveDecorationsIncompatibleWithAlternate(alternateBaseFormDef);
        }

        if (alternateBaseFormDef != null)
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
        
        currentAlternateBaseForm = alternateBaseFormDef;
        Notify_GraphicChanged();
    }
    
    public override void InitialSetup()
    {
        InitialColors();
        base.InitialSetup();
        recacheMultiGraphics = true;
    }

    public virtual void InitialColors()
    {
        drawColorOne = Props.defaultPrimaryColor ?? (thingDef.MadeFromStuff ? thingDef.GetColorForStuff(parent.Stuff) : Color.white);
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
    
    public override void SetOriginals()
    {
        originalColorOne = drawColorOne;
        originalColorTwo = drawColorTwo;
        originalColorThree = drawColorThree;
        originalMaskDef = maskDef;
        originalCurrentAlternateBaseForm = currentAlternateBaseForm;
        Notify_GraphicChanged();
    }

    public override void Reset()
    {
        drawColorOne = originalColorOne;
        drawColorTwo = originalColorTwo;
        drawColorThree = originalColorThree;
        maskDef = originalMaskDef;
        currentAlternateBaseForm = originalCurrentAlternateBaseForm;
        Notify_GraphicChanged();
    }
    
    public override void Notify_GraphicChanged()
    {
        recacheMultiGraphics = true;
        recacheSingleGraphics = true;
        base.Notify_GraphicChanged();
    }
    
    public override void Notify_Equipped(Pawn pawn)
    {
        Notify_GraphicChanged();
        base.Notify_Equipped(pawn);
    }
    
    public override void PostExposeData()
    {
        Scribe_Values.Look(ref originalColorOne, "originalColorOne", Color.white);
        Scribe_Values.Look(ref originalColorTwo, "originalColorTwo", Color.white);
        Scribe_Values.Look(ref originalColorThree, "originalColorThree", Color.white);
        Scribe_Values.Look(ref drawColorOne, "drawColorOne", Color.white);
        Scribe_Values.Look(ref drawColorTwo, "drawColorTwo", Color.white);
        Scribe_Values.Look(ref drawColorThree, "drawColorThree", Color.white);
        Scribe_Defs.Look(ref originalMaskDef, "originalMaskDef");
        Scribe_Defs.Look(ref originalCurrentAlternateBaseForm, "originalCurrentAlternateBaseForm");
        Scribe_Defs.Look(ref maskDef, "maskDef");
        Scribe_Defs.Look(ref currentAlternateBaseForm, "currentAlternateBaseForm");
        Scribe_Defs.Look(ref originalBodyType, "originalBodyType");
        
        base.PostExposeData();
        
        if (Scribe.mode == LoadSaveMode.PostLoadInit)
        {
            Notify_GraphicChanged();
        }
    }
}