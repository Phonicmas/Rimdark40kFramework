using System;
using RimWorld;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace Core40k;

[StaticConstructorOnStartup]
public class Dialog_CustomizeApparel : Window
{
    private Pawn pawn;

    Vector3 PortraitOffset = new Vector3(0f, 0f, 0.15f);

    private Vector2 apparelColorScrollPosition;
    
    private static readonly Vector2 ButSize = new Vector2(200f, 40f);

    public override Vector2 InitialSize => new Vector2(950f, 750f);
    
    private Dictionary<CustomizationTabDef, CustomizerTabDrawer>  tabDrawers = [];
    private Dictionary<CustomizationTabDef, TabRecord>  tabRecords = [];
    private List<TabRecord> tabRecordsToRead => tabRecords.Values.ToList();

    private CustomizationTabDef curTab;
    
    public Dialog_CustomizeApparel()
    {
    }

    public Dialog_CustomizeApparel(Pawn pawn)
    {
        this.pawn = pawn;
            
        foreach (var item in pawn.apparel.WornApparel.Where(a => a.def.HasModExtension<DefModExtension_AvailableDrawerTabDefs>() || a.HasComp<CompMultiColor>()))
        {
            //var defMod = item.def.GetModExtension<DefModExtension_AvailableDrawerTabDefs>();
            
            //TEMP CODE START
            var defMod = item.def.GetModExtension<DefModExtension_AvailableDrawerTabDefs>();
            List<CustomizationTabDef> tabDefs;
            if (defMod == null)
            {
                tabDefs = item.GetComp<CompMultiColor>().Props.tabDefs;
                tabDefs.Add(Core40kDefOf.BEWH_ArmorColoring);
            }
            else
            {
                tabDefs = defMod.tabDefs;
            }
            //TEMP CODE END
            
            tabDefs.SortBy(def => def.sortOrder);
            foreach (var tabDef in tabDefs)
            {
                if (!tabRecords.ContainsKey(tabDef))
                {
                    var tabRecord = new TabRecord(tabDef.label, delegate
                    {
                        curTab = tabDef;
                    }, curTab == tabDef);
                    tabRecords.Add(tabDef, tabRecord);
                }

                if (!tabDrawers.ContainsKey(tabDef))
                {
                    var tabDrawer = (CustomizerTabDrawer)Activator.CreateInstance(tabDef.tabDrawerClass);
                    tabDrawer.Setup(pawn);
                    tabDrawers.Add(tabDef, tabDrawer);
                }
            }
        }

        curTab = tabRecords.Keys.FirstOrFallback();
        Find.TickManager.Pause();
    }

    public override void DoWindowContents(Rect inRect)
    {
        Text.Font = GameFont.Medium;
        var rect = new Rect(inRect)
        {
            height = Text.LineHeight * 2f
        };
        Widgets.Label(rect, "StylePawn".Translate().CapitalizeFirst() + ": " + Find.ActiveLanguageWorker.WithDefiniteArticle(pawn.Name.ToStringShort, pawn.gender, plural: false, name: true).ApplyTag(TagType.Name));
        Text.Font = GameFont.Small;
        inRect.yMin = rect.yMax + 4f;
        var rect2 = inRect;
        rect2.width *= 0.3f;
        rect2.yMax -= ButSize.y + 4f;
        DrawPawn(rect2);
        var rect3 = inRect;
        rect3.xMin = rect2.xMax + 10f;
        rect3.yMax -= ButSize.y + 4f;
        Widgets.DrawMenuSection(rect3);
        TabDrawer.DrawTabs(rect3, tabRecordsToRead);
        rect3 = rect3.ContractedBy(18f);
        
        tabDrawers.TryGetValue(curTab).DrawTab(rect3, pawn, ref apparelColorScrollPosition);
        
        DrawBottomButtons(inRect);
    }

    private void DrawPawn(Rect rect)
    {
        Widgets.BeginGroup(rect);
        for (var i = 0; i < 4; i++)
        {
            var position = new Rect(0f, rect.height / 4f * i, rect.width, rect.height / 4f).ContractedBy(4f);
            var image = PortraitsCache.Get(pawn, new Vector2(position.width, position.height), new Rot4(3 - i), PortraitOffset, 1.1f, supersample: true, compensateForUIScale: true, true, true, null, null, stylingStation: true);
            GUI.DrawTexture(position, image);
        }
        Widgets.EndGroup();
    }

    private void DrawBottomButtons(Rect inRect)
    {
        if (Widgets.ButtonText(new Rect(inRect.x, inRect.yMax - ButSize.y, ButSize.x, ButSize.y), "Cancel".Translate()))
        {
            Close();
        }
        if (Widgets.ButtonText(new Rect(inRect.xMin + inRect.width / 2f - ButSize.x / 2f, inRect.yMax - ButSize.y, ButSize.x, ButSize.y), "Reset".Translate()))
        {
            Reset();
            SoundDefOf.Tick_Low.PlayOneShotOnCamera();
        }
        if (Widgets.ButtonText(new Rect(inRect.xMax - ButSize.x, inRect.yMax - ButSize.y, ButSize.x, ButSize.y), "Accept".Translate()))
        {
            Accept();
            Close();
        }
    }

    public override void Close(bool doCloseSound = true)
    {
        foreach (var tab in tabDrawers)
        {
            tab.Value.OnClose(pawn, closeOnCancel, closeOnClickedOutside);
        }
        
        if (closeOnCancel || closeOnClickedOutside)
        {
            Reset();
        }
        
        base.Close(doCloseSound);
    }

    private void Reset()
    {
        foreach (var tab in tabDrawers)
        {
            tab.Value.OnReset(pawn);
        }
        
        pawn.Drawer.renderer.SetAllGraphicsDirty();
    }

    private void Accept()
    {
        foreach (var tab in tabDrawers)
        {
            tab.Value.OnAccept(pawn);
        }
    }
}