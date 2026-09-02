using System;
using RimWorld;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace Core40k;

[StaticConstructorOnStartup]
public class Dialog_CustomizeWeapon : Window, ICustomizationDialog
{
    public bool Closed { get; private set; }

    private Pawn pawn;

    private ThingWithComps weapon => pawn?.equipment?.Primary;

    private Vector2 apparelColorScrollPosition;
    
    private static readonly Vector2 ButSize = new Vector2(200f, 40f);

    public override Vector2 InitialSize => new Vector2(950f, 750f);
    
    private Dictionary<CustomizationTabDef, CustomizerTabDrawer>  tabDrawers = [];
    private Dictionary<CustomizationTabDef, TabRecord>  tabRecords = [];
    private List<TabRecord> cachedTabRecordsToRead;
    private List<TabRecord> tabRecordsToRead => cachedTabRecordsToRead ??= tabRecords.Values.ToList();

    private CustomizationTabDef curTab;
    
    public Dialog_CustomizeWeapon()
    {
    }

    public Dialog_CustomizeWeapon(Pawn pawn)
    {
        this.pawn = pawn;

        if (weapon == null)
        {
            Closed = true;
            return;
        }

        foreach (var tabDef in CustomizationTabResolver.TabsFor(weapon.def))
        {
            if (!tabRecords.ContainsKey(tabDef))
            {
                var tabRecord = new TabRecord(tabDef.label, delegate
                {
                    curTab = tabDef;
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

        curTab = tabRecords.Keys.FirstOrFallback();
        Find.TickManager.Pause();
    }

    public override void DoWindowContents(Rect inRect)
    {
        if (weapon == null || curTab == null || !tabDrawers.ContainsKey(curTab))
        {
            Close();
            return;
        }

        Text.Font = GameFont.Medium;
        var rect = new Rect(inRect)
        {
            height = Text.LineHeight * 2f
        };
        Widgets.Label(rect, "StylePawn".Translate().CapitalizeFirst() + ": " + weapon.def.LabelCap);
        Text.Font = GameFont.Small;
        inRect.yMin = rect.yMax + 4f;
        var rect2 = inRect;
        rect2.width *= 0.4f;
        rect2.yMax -= ButSize.y + 4f;
        DrawWeapon(rect2);
        var rect3 = inRect;
        rect3.xMin = rect2.xMax + 10f;
        rect3.yMax -= ButSize.y + 4f;
        Widgets.DrawMenuSection(rect3);
        TabDrawer.DrawTabs(rect3, tabRecordsToRead);
        rect3 = rect3.ContractedBy(18f);

        tabDrawers[curTab].DrawTab(rect3, pawn, ref apparelColorScrollPosition);
        
        DrawBottomButtons(inRect);
    }

    private void DrawWeapon(Rect rect)
    {
        rect.y += rect.height / 2;
        rect.height = rect.width;
        rect.y -= rect.height / 2;
        Widgets.DrawMenuSection(rect);

        var iconRect = new Rect(rect);
        iconRect = iconRect.ContractedBy(1);
        
        Widgets.DrawMenuSection(iconRect.ContractedBy(-1));
        
        Widgets.DrawTextureFitted(iconRect, weapon.Graphic.MatSingle.mainTexture, 1f, weapon.Graphic.MatSingle);
        
        var weaponDecorationComp = weapon.TryGetComp<CompWeaponDecoration>();
        
        if (weaponDecorationComp != null)
        {
            foreach (var graphic in weaponDecorationComp.Graphics)
            {
                if (graphic.Key is not WeaponDecorationDef weaponDecoration)
                {
                    continue;
                }
                var offset = Vector3.zero;
                var drawSize = graphic.Key.drawSize;
                if (weaponDecoration.weaponSpecificDrawData != null && weaponDecoration.weaponSpecificDrawData.TryGetValue(weapon.def.defName, out var value))
                {
                    offset = value.OffsetForRot(Rot4.Invalid);
                    drawSize *= value.scale;
                }
                else if(graphic.Key.drawData != null)
                {
                    offset = graphic.Key.drawData.OffsetForRot(Rot4.Invalid);
                    drawSize *= graphic.Key.drawData.scale;
                }
                
                if (weaponDecorationComp.drawDatas.TryGetValue(graphic.Key, out var drawData))
                {
                    offset += drawData.defaultData.offset;
                    drawSize *= drawData.defaultData.scale;
                }
                
                var offsetRect = new Rect(iconRect);
                offsetRect.width *= drawSize.x;
                offsetRect.height *= drawSize.y;
                offsetRect.width /= weapon.def.graphicData.drawSize.x;
                offsetRect.height /= weapon.def.graphicData.drawSize.y;
                offsetRect.x = iconRect.center.x - offsetRect.width / 2;
                offsetRect.y = iconRect.center.y - offsetRect.height / 2;

                var sizeDiff = iconRect.size - offsetRect.size;
                
                offsetRect.x += (offset.x * sizeDiff.x + offsetRect.width * offset.x) / weapon.def.graphicData.drawSize.x;
                offsetRect.y -= (offset.z * sizeDiff.y + offsetRect.height * offset.z) / weapon.def.graphicData.drawSize.y;
                
                Graphics.DrawTexture(offsetRect, graphic.Value.MatSouth.mainTexture, new Rect(0f, 0f, 1f, 1f), 0, 0, 0, 0, Color.white, graphic.Value.MatSingle);
            }
        }
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
        if (closeOnCancel || closeOnClickedOutside)
        {
            Reset();
        }

        foreach (var tab in tabDrawers)
        {
            tab.Value.OnClose(pawn, closeOnCancel, closeOnClickedOutside);
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