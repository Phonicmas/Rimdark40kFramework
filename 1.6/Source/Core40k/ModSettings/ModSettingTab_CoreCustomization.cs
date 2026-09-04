using RimWorld;
using UnityEngine;
using Verse;

namespace Core40k;

public class ModSettingTab_CoreCustomization : ModSettingTab
{
    public override void DrawTab(Rect inRect, ModSettings settings)
    {
        if (settings is not Core40kModSettings core40KModSettings)
        {
            Log.Error("Settings not correct type");
            return;
        }
        
        var viewRect = new Rect(inRect.x, inRect.y, inRect.width - 16f, scrollViewHeight);
        scrollViewHeight = 0f;
            
        Widgets.BeginScrollView(inRect, ref scrollPos, viewRect);
        var listingStandard = new Listing_Standard();
        listingStandard.Begin(viewRect);
        listingStandard.Gap(36);
        scrollViewHeight += ListingHeightIncreaseGap;
        scrollViewHeight += ListingHeightIncrease;
        
        listingStandard.CheckboxLabeled("BEWH.Framework.ModSettings.ShowCustomizationDebugOptions".Translate(), ref core40KModSettings.showCustomizationDebugOptions);
        scrollViewHeight += ListingHeightIncrease;
        
        core40KModSettings.decorationsPerRow = (int)listingStandard.SliderLabeled("BEWH.Framework.ModSettings.DecorationsPerRow".Translate(core40KModSettings.decorationsPerRow),core40KModSettings.decorationsPerRow, 3, 8, tooltip: "BEWH.Framework.ModSettings.DecorationsPerRowTooltip".Translate());
        scrollViewHeight += ListingHeightIncrease;
        
        listingStandard.CheckboxLabeled("BEWH.Framework.ModSettings.DecorationWorkEnabled".Translate(), ref core40KModSettings.decorationWorkEnabled, "BEWH.Framework.ModSettings.DecorationWorkEnabledTooltip".Translate());
        scrollViewHeight += ListingHeightIncrease;

        listingStandard.CheckboxLabeled("BEWH.Framework.ModSettings.DecorationCostEnabled".Translate(), ref core40KModSettings.decorationCostEnabled, "BEWH.Framework.ModSettings.DecorationCostEnabledTooltip".Translate());
        scrollViewHeight += ListingHeightIncrease;

        var showOnGroundBefore = core40KModSettings.showDecorationsOnGround;
        listingStandard.CheckboxLabeled("BEWH.Framework.ModSettings.ShowDecorationsOnGround".Translate(), ref core40KModSettings.showDecorationsOnGround, "BEWH.Framework.ModSettings.ShowDecorationsOnGroundTooltip".Translate());
        scrollViewHeight += ListingHeightIncrease;
        if (showOnGroundBefore != core40KModSettings.showDecorationsOnGround && Current.Game != null)
        {
            foreach (var map in Find.Maps)
            {
                map.mapDrawer.WholeMapChanged(MapMeshFlagDefOf.Things);
            }
        }

        if (core40KModSettings.decorationWorkEnabled)
        {
            core40KModSettings.appearanceChangeWorkAmount = listingStandard.SliderLabeled("BEWH.Framework.ModSettings.AppearanceChangeWork".Translate(core40KModSettings.appearanceChangeWorkAmount.ToString("F0")), core40KModSettings.appearanceChangeWorkAmount, 0f, 2000f, tooltip: "BEWH.Framework.ModSettings.AppearanceChangeWorkTooltip".Translate());
            scrollViewHeight += ListingHeightIncrease;

            core40KModSettings.minimumWorkAmount = listingStandard.SliderLabeled("BEWH.Framework.ModSettings.MinimumWork".Translate(core40KModSettings.minimumWorkAmount.ToString("F0")), core40KModSettings.minimumWorkAmount, 0f, 2000f, tooltip: "BEWH.Framework.ModSettings.MinimumWorkTooltip".Translate());
            scrollViewHeight += ListingHeightIncrease;
        }

        //Check VEF patches
        listingStandard.GapLine(36);
        scrollViewHeight += ListingHeightIncreaseGap;
        listingStandard.Label("\n" + "BEWH.ModSettings.CheckVEFPatches".Translate());
        scrollViewHeight += ListingHeightIncrease;
        
        scrollViewHeight += ListingHeightIncrease;
        
        listingStandard.End();
        Widgets.EndScrollView();
    }
}