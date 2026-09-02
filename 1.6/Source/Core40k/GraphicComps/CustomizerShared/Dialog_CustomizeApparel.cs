using System;
using RimWorld;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace Core40k;

[StaticConstructorOnStartup]
public class Dialog_CustomizeApparel : Window, ICustomizationDialog
{
    public bool Closed { get; private set; }

    private Pawn pawn;

    Vector3 PortraitOffset = new Vector3(0f, 0f, 0.15f);

    private Vector2 apparelColorScrollPosition;
    
    private static readonly Vector2 ButSize = new Vector2(200f, 40f);

    public override Vector2 InitialSize => new Vector2(950f, 750f);
    
    private Dictionary<CustomizationTabDef, CustomizerTabDrawer>  tabDrawers = [];
    private Dictionary<CustomizationTabDef, TabRecord>  tabRecords = [];
    //Was a property materialising a new list from the dictionary on every access, once per
    //DoWindowContents - i.e. several times per frame for as long as the dialog was open.
    private List<TabRecord> cachedTabRecordsToRead;
    private List<TabRecord> tabRecordsToRead => cachedTabRecordsToRead ??= tabRecords.Values.ToList();

    private CustomizationTabDef curTab;
    
    public Dialog_CustomizeApparel()
    {
    }

    public Dialog_CustomizeApparel(Pawn pawn)
    {
        this.pawn = pawn;
            
        foreach (var item in pawn.apparel.WornApparel.Where(a => CustomizationTabResolver.HasAnyTab(a.def)))
        {
            //Tabs are worked out from the comps the item carries and the content that applies to
            //it. The list is owned by the resolver and already sorted - do not mutate it.
            foreach (var tabDef in CustomizationTabResolver.TabsFor(item.def))
            {
                if (!tabRecords.ContainsKey(tabDef))
                {
                    var tabRecord = new TabRecord(tabDef.label, delegate
                    {
                        curTab = tabDef;
                    //Func<bool> overload: the bool overload captures the value at construction time,
                //and curTab is only assigned after this loop, so no tab ever rendered as selected.
                }, () => curTab == tabDef);
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
        //curTab is null when the pawn resolved to no tabs at all, and Dictionary.TryGetValue throws
        //on a null key rather than simply missing.
        if (curTab == null || !tabDrawers.ContainsKey(curTab))
        {
            Close();
            return;
        }

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
        
        tabDrawers[curTab].DrawTab(rect3, pawn, ref apparelColorScrollPosition);
        
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

        Closed = true;
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
        DecorationWorkUtility.TryAccept(pawn, tabDrawers.Values.SelectMany(tab => tab.Comps), () => Close());
    }
}