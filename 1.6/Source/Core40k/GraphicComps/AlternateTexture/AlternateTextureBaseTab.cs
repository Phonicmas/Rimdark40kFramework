using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace Core40k;

public class AlternateTextureBaseTab : CustomizerTabDrawer
{
    protected static Core40kModSettings modSettings = null;
    public static Core40kModSettings ModSettings => modSettings ??= LoadedModManager.GetMod<Core40kMod>().GetSettings<Core40kModSettings>();

    protected List<CompAlternateTexture> alternateComps = [];
    private Dictionary<CompAlternateTexture, List<AlternateBaseFormDef>> alternateBaseFormDefs = [];
    private Dictionary<CompAlternateTexture, Dictionary<DecorationTypeDef, List<AlternateBaseFormDef>>> alternateBaseFormByTypeForComp = new();//TODO: add type stuff for alternate to split them

    private static float listScrollViewHeight = 0f;
    protected float curY;
    
    public override void Setup(Pawn pawn)
    {
        SetupHook(pawn);

        var compsWithContent = new List<CompAlternateTexture>();

        foreach (var alternateComp in alternateComps)
        {
            alternateComp.SetOriginals();

            var alternateBaseFormForApparel = DecorationIndex.AlternatesFor(alternateComp.parent.def);

            if (alternateBaseFormForApparel.Count == 0)
            {
                //No alternate forms for this item, so it gets no header and no empty section.
                continue;
            }

            var decoGroupings = alternateBaseFormForApparel.Where(def => def.appliesTo.Contains(alternateComp.parent.def.defName) || def.appliesToAll).GroupBy(def => def.decorationType);
            var tempDictionary = decoGroupings.ToDictionary(decoGrouping => decoGrouping.Key, decoGrouping => decoGrouping.ToList());

            if (tempDictionary.NullOrEmpty())
            {
                continue;
            }

            foreach (var value in tempDictionary.Values)
            {
                value.SortBy(def => def.sortOrder);
            }
            alternateBaseFormByTypeForComp.Add(alternateComp, tempDictionary);
            compsWithContent.Add(alternateComp);

            var toAppendToApparel = new List<AlternateBaseFormDef> { null };
            toAppendToApparel.AddRange(alternateBaseFormForApparel);

            alternateBaseFormDefs.Add(alternateComp, toAppendToApparel);
        }

        alternateComps = compsWithContent;
    }

    protected virtual void SetupHook(Pawn pawn) { }

    public override IEnumerable<CompGraphicParent> Comps => alternateComps;

     public override void DrawTab(Rect rect, Pawn pawn, ref Vector2 scrollPosition)
    {
        GUI.BeginGroup(rect);
        var outRect = new Rect(0f, 0f, rect.width, rect.height);
        var viewRect = new Rect(0f, 0f, rect.width - 16f, listScrollViewHeight);
        Widgets.BeginScrollView(outRect, ref scrollPosition, viewRect);

        curY = viewRect.y;
        var curX = viewRect.x;
        var iconSize = new Vector2(viewRect.width/ModSettings.decorationsPerRow, viewRect.width/ModSettings.decorationsPerRow);
        
        foreach (var alternateComp in alternateComps)
        {
            var nameRect = new Rect(viewRect)
            {
                height = rect.height/12,
                y = curY
            };
            curY += nameRect.height;
            nameRect = nameRect.ContractedBy(5f);
            Widgets.DrawMenuSection(nameRect.ContractedBy(-1));
            Text.Font = GameFont.Medium;
            Text.Anchor = TextAnchor.MiddleCenter;
            Widgets.Label(nameRect, alternateComp.parent.LabelCap);
            Text.Anchor = TextAnchor.UpperLeft;
            Text.Font = GameFont.Small;
            
            foreach (var alternateByType in alternateBaseFormByTypeForComp[alternateComp])
            {
                if (alternateByType.Value.NullOrEmpty())
                {
                    continue;
                }
                
                var headerRect = new Rect(viewRect)
                {
                    height = nameRect.height * 0.8f,
                    y = curY
                };
                curY += headerRect.height;
                headerRect = headerRect.ContractedBy(5f);
        
                Widgets.DrawMenuSection(headerRect.ContractedBy(-1));
                Text.Anchor = TextAnchor.MiddleCenter;
                Widgets.Label(headerRect, alternateByType.Key.label);
                Text.Anchor = TextAnchor.UpperLeft;
                
                
                for (var i = 0; i < alternateByType.Value.Count; i++)
                {
                    var position = new Vector2(curX, curY);
                    var iconRect = new Rect(position, iconSize);
            
                    curX += iconRect.width;
                    
                    iconRect = iconRect.ContractedBy(5f);
            
                    var alternateFormSelected = alternateComp.CurrentAlternateBaseForm == alternateByType.Value[i];
            
                    if (alternateFormSelected)
                    {
                        Widgets.DrawStrongHighlight(iconRect.ExpandedBy(3f));
                    }
                
                    var tipTooltip = alternateByType.Value[i]?.LabelCap ?? "BEWH.Framework.Customization.BaseTexture".Translate() + alternateComp.parent.def.LabelCap;
                    GUI.color = Mouse.IsOver(iconRect) ? GenUI.MouseoverColor : Color.white;
                    GUI.DrawTexture(iconRect, Command.BGTexShrunk);
                    GUI.color = Color.white;
                    GUI.DrawTexture(iconRect, alternateByType.Value[i]?.Icon ?? alternateComp.parent.def.uiIcon);
                    TooltipHandler.TipRegion(iconRect, tipTooltip);
                
                    if (Widgets.ButtonInvisible(iconRect))
                    {
                        alternateComp.SetAlternateBaseForm(alternateByType.Value[i], alternateComp.parent is Apparel);
                    }
                
                    if (i != 0 && (i+1) % 4 == 0 || i == alternateByType.Value.Count - 1)
                    {
                        curY += iconSize.x;
                        curX = viewRect.x;
                    }
                }
            }

            curY += 22;
            listScrollViewHeight = curY;
        }
            
        Widgets.EndScrollView();
        GUI.EndGroup();
    }

    public override void OnClose(Pawn pawn, bool closeOnCancel, bool closeOnClickedOutside)
    {
        OnReset(pawn);
    }
    
    public override void OnReset(Pawn pawn)
    {
        foreach (var alternateComp in alternateComps)
        {
            alternateComp.Reset();
        }
    }
}