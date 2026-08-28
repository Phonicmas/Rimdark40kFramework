using System;
using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace Core40k;

public class Dialog_ExtraDecorationMaskSelection : Window
{
    private Vector2 size;
    public override Vector2 InitialSize => size;
    
    private CompDecorative decorativeComp;
    private ExtraDecorationDef decorationDef;
    private DecorationSettings decorationSettings;
    private List<MaskDef> masks = [];
    private int pageNumber = 0;
    private Dictionary<MaskDef, Material> cachedMaterials = new ();
    private bool recache = true;
        
    public Dialog_ExtraDecorationMaskSelection(Apparel apparel, ExtraDecorationDef decorationDef, List<MaskDef> masks, float size)
    {
        decorativeComp = apparel.GetComp<CompDecorative>();
        this.decorationDef = decorationDef;
        decorationSettings = decorativeComp.Decorations[decorationDef];
        this.masks = masks;
        this.size = new Vector2(size, size/2.5f);
    }
        
    public override void DoWindowContents(Rect inRect)
    {
        var maskRect = new Rect(inRect);
        maskRect.height = maskRect.width/3;
        var arrowsEnabled = masks.Count > 3;
        if (arrowsEnabled)
        {
            maskRect.height += maskRect.height / 5;
        }
        Widgets.DrawMenuSection(maskRect.ContractedBy(-10));

        var posRect = new Rect(maskRect);
        posRect.width /= 3;
        posRect.height = posRect.width;
        var num = masks.Count - pageNumber * 3;
        num = num > 3 ? 3 : num;
        var curPageMasks = masks.GetRange(pageNumber * 3, num);
        for (var i = 0; i < curPageMasks.Count; i++)
        {
            var curPosRect = new Rect(posRect);
            curPosRect.x += curPosRect.width * i;
                        
            curPosRect = curPosRect.ContractedBy(15);
            if (!cachedMaterials.ContainsKey(curPageMasks[i]) || recache)
            {
                if (recache)
                {
                    cachedMaterials = new Dictionary<MaskDef, Material>();
                }

                var path = decorationDef.drawnTextureIconPath;
                var shader = Core40kDefOf.BEWH_CutoutThreeColor.Shader;
                var graphic = MultiColorUtils.GetGraphic<Graphic_Multi>(path, shader, Vector2.one, decorationSettings.Color, decorationSettings.ColorTwo, decorationSettings.ColorThree, null, curPageMasks[i]?.maskPath ?? decorationDef.drawnTextureIconPath + "_mask");
                var material = graphic?.MatSouth ?? BaseContent.BadMat;
                cachedMaterials.Add(curPageMasks[i], material);
                recache = false;
            }

            if (decorationSettings.maskDef == curPageMasks[i] || (decorationSettings.maskDef.setsNull && curPageMasks[i].setsNull))
            {
                Widgets.DrawStrongHighlight(curPosRect.ExpandedBy(6f));
            }
                        
            Widgets.DrawMenuSection(curPosRect.ContractedBy(-1));
            Graphics.DrawTexture(curPosRect, cachedMaterials[curPageMasks[i]].mainTexture, cachedMaterials[curPageMasks[i]]);
                        
            TooltipHandler.TipRegion(curPosRect, curPageMasks[i].label);
                        
            Widgets.DrawHighlightIfMouseover(curPosRect);

            if (Widgets.ButtonInvisible(curPosRect))
            {
                decorativeComp.SetDecorationMask(decorationDef, curPageMasks[i]);
            }
        }

        if (arrowsEnabled)
        {
            var arrowBack = new Rect(maskRect)
            {
                height = maskRect.height / 5,
                width = maskRect.height / 5
            };
            arrowBack.y = maskRect.yMax - arrowBack.height;
                        
            if (pageNumber > 0)
            {
                if (Widgets.ButtonInvisible(arrowBack))
                {
                    pageNumber--;
                }
                arrowBack = arrowBack.ContractedBy(5);
                Widgets.DrawTextureFitted(arrowBack, Core40kUtils.ScrollBackwardIcon, 1);
            }
                        
            if (pageNumber < Math.Ceiling((float)masks.Count / 3)-1)
            {
                var arrowForward = new Rect(arrowBack)
                {
                    x = maskRect.xMax - arrowBack.width
                };
                if (Widgets.ButtonInvisible(arrowForward))
                {
                    pageNumber++;
                }
                arrowForward = arrowForward.ContractedBy(5);
                Widgets.DrawTextureFitted(arrowForward, Core40kUtils.ScrollForwardIcon, 1);
            }
        }
    }
}