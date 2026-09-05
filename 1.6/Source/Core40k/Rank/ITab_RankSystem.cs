using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using UnityEngine;
using VEF.Utils;
using Verse;

namespace Core40k;

[StaticConstructorOnStartup]
public class ITab_RankSystem : ITab
{
    private RankInfoForTab currentlySelectedRank = null;
        
    private RankCategoryDef currentlySelectedRankCategory = null;
        
    private List<RankCategoryDef> availableCategories = [];
        
    private List<RankInfoForTab> availableRanksForCategory = [];

    private Pawn pawn;
        
    private CompRankInfo compRankInfo;

    private bool redoRankInfo = false;

    private TaggedString? noCategorySelectedText;
    private string noneText;
    
    Dictionary<RankDef, Vector2> rankPos = new Dictionary<RankDef, Vector2>();

    private Core40kModSettings modSettings;
    private Core40kModSettings ModSettings => modSettings ??= LoadedModManager.GetMod<Core40kMod>().GetSettings<Core40kModSettings>();
        
    private static readonly CachedTexture LockedIcon = new CachedTexture("UI/Misc/LockedIcon");
        
    const float rankIconRectSize = 40f;
    const float rankIconGapSize = 40f;
    const float rankPlacementMult = rankIconGapSize + rankIconRectSize;
    const float rankIconMargin = 20f;

    //IsVisible runs on every GUI event while a pawn is selected, and the category walk behind it is
    //not cheap, so the answer is held for a short window per selected pawn.
    private const int VisibilityCacheFrames = 30;
    private Pawn visibilityCachedFor;
    private int visibilityCachedFrame = -1;
    private bool visibilityCached;

    public override bool IsVisible
    {
        get
        {
            if (SelPawn == null)
            {
                return false;
            }

            if (Find.Selector?.SingleSelectedThing is not Pawn p)
            {
                return ModSettings?.alwaysShowRankTab ?? false;
            }

            var frame = Time.frameCount;
            if (p == visibilityCachedFor && frame - visibilityCachedFrame < VisibilityCacheFrames && frame >= visibilityCachedFrame)
            {
                return visibilityCached;
            }

            visibilityCachedFor = p;
            visibilityCachedFrame = frame;
            visibilityCached = ComputeVisible(p);
            return visibilityCached;
        }
    }

    private bool ComputeVisible(Pawn p)
    {
        var defaultRes = ModSettings?.alwaysShowRankTab ?? false;
        if (!p.HasComp<CompRankInfo>() || p.Faction == null || !p.Faction.IsPlayer || p.IsSlaveOfColony || p.IsPrisonerOfColony || availableCategories.NullOrEmpty())
        {
            return defaultRes;
        }

        foreach (var rankCategoryDef in availableCategories)
        {
            if (rankCategoryDef.RankCategoryUnlockedFor(p))
            {
                return true;
            }
        }

        return defaultRes;
    }

    public ITab_RankSystem()
    {
        labelKey = "BEWH.Framework.RankSystem.RankTab";
        UpdateRankCategoryList();
    }
        
    public override void OnOpen()
    {
        base.OnOpen();
        pawn = SelPawn;

        size = new Vector2(UI.screenWidth, PaneTopY - 100);
        
        compRankInfo = pawn.GetComp<CompRankInfo>();
        if (compRankInfo == null)
        {
            CloseTab();
            return;
        }
        rankPos.Clear();
        cachedYAndX = null;
        UpdateRankCategoryList();
        if (compRankInfo.LastOpenedRankCategory != null && compRankInfo.LastOpenedRankCategory.RankCategoryUnlockedFor(SelPawn))
        {
            currentlySelectedRankCategory = compRankInfo.LastOpenedRankCategory;
        }
        else
        {
            currentlySelectedRankCategory = availableCategories.FirstOrDefault(availableCategory => availableCategory.RankCategoryUnlockedFor(pawn));
        }
        GetRanksForCategory();
        if (!compRankInfo.UnlockedRanks.NullOrEmpty())
        {
            var highestRank = compRankInfo.HighestRankDef(true) ?? compRankInfo.HighestRankDef(false);
            currentlySelectedRank = availableRanksForCategory.FirstOrFallback(rank => rank.rankDef == highestRank, fallback: null);
        }
        else
        {
            currentlySelectedRank = availableRanksForCategory.FirstOrFallback(rank => rank.rankDef.defaultFirstRank, fallback: null);
        }
    }

    protected override void FillTab()
    {
        if (pawn != SelPawn)
        {
            CloseTab();
            return;
        }

        var font = Text.Font;
        var anchor = Text.Anchor;
            
        var rect = new Rect(Vector2.one * 20f, size - Vector2.one * 40f);
        var rect2 = rect.TakeLeftPart(size.x * 0.25f);
            
        var curY = rect.y;
            
        Text.Font = GameFont.Medium;
        Text.Anchor = TextAnchor.MiddleCenter;
            
        //Button to switch between rank categories
        var categoryText = currentlySelectedRankCategory != null
            ? currentlySelectedRankCategory.LabelCap
            : (noCategorySelectedText ??= "BEWH.Framework.RankSystem.NoCategorySelected".Translate());
            
        var categoryTextRect = new Rect(rect2)
        {
            height = 30f
        };
        categoryTextRect.width /= 2;
        categoryTextRect.x += categoryTextRect.width/2;

        curY += categoryTextRect.height;
            
        //Dev Options
        if (Prefs.DevMode)
        {
            const float width = 80f;
            const float padding = 30f;
            var debugResetRankRect = new Rect(categoryTextRect)
            {
                width = width,
            };
            debugResetRankRect.height += 10f;
            debugResetRankRect.y += -5f;
            debugResetRankRect.x -= debugResetRankRect.width + padding;
                
            var debugUnlockRankRect = new Rect(categoryTextRect)
            {
                width = width,
            };
            debugUnlockRankRect.height += 10f;
            debugUnlockRankRect.y += -5f;
            debugUnlockRankRect.x += categoryTextRect.width + padding;
                
            Text.Font = GameFont.Small;
            if (Widgets.ButtonText(debugResetRankRect,"dev:\nreset ranks"))
            {
                compRankInfo.ResetRanks(currentlySelectedRankCategory);
            }
            
            if (Widgets.ButtonText(debugUnlockRankRect,"dev:\nunlock rank") && currentlySelectedRank != null)
            {
                UnlockRank(currentlySelectedRank.rankDef);
            }
            Text.Font = GameFont.Medium;
        }

        //Select rank category
        if (Widgets.ButtonText(categoryTextRect,categoryText))
        {
            var list = new List<FloatMenuOption>();
            foreach (var category in availableCategories)
            {
                if (currentlySelectedRankCategory == category)
                {
                    continue;
                }   
                var menuOption = new FloatMenuOption(category.label.CapitalizeFirst(), delegate
                {
                    currentlySelectedRankCategory = category;
                    compRankInfo.OpenedRankCategory(category);
                    currentlySelectedRank = null;
                    GetRanksForCategory();
                    rankPos.Clear();
                }, Widgets.PlaceholderIconTex, Color.white);
                if (!category.RankCategoryUnlockedFor(pawn))
                {
                    var newLabel = category.RankCategoryRequirementsNotMetFor(pawn);
                    menuOption.Disabled = true;
                    menuOption.tooltip = newLabel;
                }
                list.Add(menuOption);
            }

            if (list.NullOrEmpty())
            {
                var menuOptionNone = new FloatMenuOption("NoneBrackets".Translate(), null);
                list.Add(menuOptionNone);
            }
            
            Find.WindowStack.Add(new FloatMenu(list));
        }
            
        var toolTip = currentlySelectedRankCategory != null ? currentlySelectedRankCategory.LabelCap.ToString() : (noneText ??= "BEWH.Framework.CommonKeyword.None".Translate().ToString());
        TooltipHandler.TipRegion(categoryTextRect, toolTip);

        curY += 12f;

        if (redoRankInfo)
        {
            GetRanksForCategory();
            redoRankInfo = false;
        }
            
        //Rank info
        var rectRankInfo = new Rect(rect2);
        rectRankInfo.height -= curY;
        rectRankInfo.y = curY;

        Widgets.DrawMenuSection(rectRankInfo);
            
        FillRankInfo(rectRankInfo);
            
        //Rank tree
        var rectRankTree = new Rect(rect)
        {
            xMin = rectRankInfo.xMax,
            yMin = rectRankInfo.yMin,
            yMax = rectRankInfo.yMax,
        };
        rectRankTree.xMin += 50f;

        Widgets.DrawMenuSection(rectRankTree);
        FillRankTree(rectRankTree);
            
        Text.Font = font;
        Text.Anchor = anchor;
    }

    //Cached alongside rankPos, which is cleared on the same events. This allocated a list and ran
    //three MaxBy/MinBy passes with dictionary lookups on every frame the tab was open.
    private (float yMax, float yMin, float xMax)? cachedYAndX;

    private (float yMax, float yMin, float xMax) GetYAndX()
    {
        if (cachedYAndX.HasValue)
        {
            return cachedYAndX.Value;
        }
        
        if (currentlySelectedRankCategory == null || availableRanksForCategory.NullOrEmpty())
        {
            cachedYAndX = (0f, 0f, 0f);
            return cachedYAndX.Value;
        }

        var ranks = availableRanksForCategory.Select(rank => rank.rankDef).ToList();
            
        var xMax = ranks.MaxBy(rank => currentlySelectedRankCategory.rankDict[rank].displayPosition.x);
        var yMax = ranks.MaxBy(rank => currentlySelectedRankCategory.rankDict[rank].displayPosition.y);
        var yMin = ranks.MinBy(rank => currentlySelectedRankCategory.rankDict[rank].displayPosition.y);

        cachedYAndX = (currentlySelectedRankCategory.rankDict[yMax].displayPosition.y, currentlySelectedRankCategory.rankDict[yMin].displayPosition.y, currentlySelectedRankCategory.rankDict[xMax].displayPosition.x);
        return cachedYAndX.Value;
    }
        
    private Vector2 scrollPosition;
    private void FillRankTree(Rect rectRankTree)
    {
        if (currentlySelectedRankCategory == null || availableRanksForCategory.NullOrEmpty())
        {
            return;
        }
        
        var outRect = rectRankTree.ContractedBy(10f);

        var (yMax, yMin, xMax) = GetYAndX();
        
        var cellsUp = Mathf.Max(0f, -yMin);
        var cellsDown = Mathf.Max(0f, yMax);
        var cellsRight = Mathf.Max(0f, xMax);
        
        var halfContentHeight = Mathf.Max(cellsUp, cellsDown) * rankPlacementMult + rankIconRectSize / 2f + rankIconMargin;
        var contentWidth = cellsRight * rankPlacementMult + rankIconRectSize + rankIconMargin * 2f;

        const float scrollBarWidth = 16f;
        var contentHeight = halfContentHeight * 2f;

        var needsHorizontal = contentWidth > outRect.width;
        var needsVertical = contentHeight > outRect.height - (needsHorizontal ? scrollBarWidth : 0f);
        if (needsVertical && !needsHorizontal)
        {
            needsHorizontal = contentWidth > outRect.width - scrollBarWidth;
        }

        var viewRect = new Rect(outRect.x, outRect.y,
            Mathf.Max(contentWidth, outRect.width - (needsVertical ? scrollBarWidth : 0f)),
            Mathf.Max(contentHeight, outRect.height - (needsHorizontal ? scrollBarWidth : 0f)));

        var xStart = viewRect.x + rankIconMargin;

        var yStart = viewRect.y + viewRect.height / 2f - rankIconRectSize / 2f;

        Widgets.BeginScrollView(outRect, ref scrollPosition, viewRect);
            
        Widgets.DrawRectFast(viewRect, new Color(0f, 0f, 0f, 0.3f));
        
        if (rankPos.NullOrEmpty())
        {
            foreach (var rank in availableRanksForCategory)
            {
                var rankRect = new Rect
                {
                    width = rankIconRectSize,
                    height = rankIconRectSize,
                    x = xStart + currentlySelectedRankCategory.rankDict[rank.rankDef].displayPosition.x * rankPlacementMult,
                    y = yStart + currentlySelectedRankCategory.rankDict[rank.rankDef].displayPosition.y * rankPlacementMult,
                };

                if (currentlySelectedRankCategory.rankDict[rank.rankDef].displayPosition.x < 0)
                {
                    Log.Error(rank.rankDef.defName + " has display position with x < 0. Should be 0 or above");
                }

                if (!rankPos.ContainsKey(rank.rankDef))
                {
                    rankPos.Add(rank.rankDef, rankRect.position);
                }
            }
        }

        //Draws requirement lines
        foreach (var rank in availableRanksForCategory)
        {
            var requirementData = currentlySelectedRankCategory.rankDict[rank.rankDef];
            DrawRequirementLines(rank.rankDef, requirementData.rankRequirements);
            DrawRequirementLines(rank.rankDef, requirementData.rankRequirementsOneAmong);
        }
            
        //Draws icons
        foreach (var rank in availableRanksForCategory)
        {
            var displayPosition = currentlySelectedRankCategory.rankDict[rank.rankDef].displayPosition;
            var rankRect = new Rect
            {
                width = rankIconRectSize,
                height = rankIconRectSize,
                x = xStart + displayPosition.x * rankPlacementMult,
                y = yStart + displayPosition.y * rankPlacementMult,
            };

            if (rank == currentlySelectedRank)
            {
                Widgets.DrawStrongHighlight(rankRect.ExpandedBy(4f));
            }
                
            DrawIcon(rankRect, rank.rankDef.RankIcon, true);
                
            if (!AlreadyUnlocked(rank.rankDef))
            {
                var colour = rank.requirementsMet ? new Color(0f, 0f, 0f, 0.55f) : new Color(0f, 0f, 0f, 0.9f);
                Widgets.DrawRectFast(rankRect, colour);
            }
                
            if (HasAnyIncompatibleRank(rank.rankDef))
            {
                DrawIcon(rankRect, LockedIcon.Texture, false);
            }
                
            if (Widgets.ButtonInvisible(rankRect))
            {
                currentlySelectedRank = rank;
            }

            TooltipHandler.TipRegion(rankRect, rank.rankDef.LabelCap);
        }
            
        Widgets.EndScrollView();
    }
        
    private bool HasAnyIncompatibleRank(RankDef rankDef)
    {
        var incompatibleRanks = rankDef.incompatibleRanks;
        if (incompatibleRanks == null)
        {
            return false;
        }

        for (var i = 0; i < incompatibleRanks.Count; i++)
        {
            if (compRankInfo.HasRank(incompatibleRanks[i]))
            {
                return true;
            }
        }

        return false;
    }

    private void DrawRequirementLines(RankDef rankDef, List<RankData> requirements)
    {
        if (requirements == null || !rankPos.TryGetValue(rankDef, out var rankPosition))
        {
            return;
        }

        var startPos = new Vector2(rankPosition.x + rankIconRectSize/2, rankPosition.y + rankIconRectSize/2);
        foreach (var rankReq in requirements)
        {
            if (!rankPos.TryGetValue(rankReq.rankDef, out var reqPosition))
            {
                continue;
            }
            var endPos = new Vector2(reqPosition.x + rankIconRectSize/2, reqPosition.y + rankIconRectSize/2);
                
            var rankUnlocked = compRankInfo.HasRank(rankReq.rankDef) ? Color.white : Color.grey;

            if (currentlySelectedRank != null && currentlySelectedRank.rankDef == rankReq.rankDef)
            {
                rankUnlocked = new Color(0.0f, 0.5f, 1f, 0.9f);
            }
                
            Widgets.DrawLine(startPos, endPos, rankUnlocked, 2f);
        }
    }

    private Vector2 scrollPosRankInfo;
    private float scrollViewHeightRankInfo = 0f;
    private void FillRankInfo(Rect rect)
    {
        var rectRankInfo = new Rect(rect);
        rectRankInfo = rectRankInfo.ContractedBy(20f);
        
        if (currentlySelectedRank != null)
        {
            var viewRect = new Rect(rectRankInfo.x, rectRankInfo.y, rectRankInfo.width - 16f,
                Mathf.Max(scrollViewHeightRankInfo, rectRankInfo.height));
            
            var listingRankInfo = new Listing_Standard
            {
                maxOneColumn = true,
            };
            //Start
            Widgets.BeginScrollView(rectRankInfo, ref scrollPosRankInfo, viewRect);
            listingRankInfo.Begin(viewRect);
                
            Text.Font = GameFont.Medium;
            Text.Anchor = TextAnchor.UpperCenter;
                
            //Name
            listingRankInfo.Gap(5);
            var rankLabel = currentlySelectedRank.rankDef.label.CapitalizeFirst();
            listingRankInfo.Label(rankLabel);
                
            //Show day as rank
            var rankDayAmount = compRankInfo.GetDaysAsRank(currentlySelectedRank.rankDef);
            if (rankDayAmount > 0f)
            {
                listingRankInfo.Gap(5);
                Text.Font = GameFont.Small;
                listingRankInfo.Label("BEWH.Framework.RankSystem.DaysSinceRankGiven".Translate(rankDayAmount));
                Text.Font = GameFont.Medium;
            }
            //Unlock button
            else if (currentlySelectedRank.requirementsMet && !AlreadyUnlocked(currentlySelectedRank.rankDef))
            {   
                listingRankInfo.Gap();
                listingRankInfo.Indent(viewRect.width * 0.25f);
                if (listingRankInfo.ButtonText("BEWH.Framework.RankSystem.UnlockRank".Translate(), widthPct: 0.5f))
                {
                    void Action() => UnlockRank(currentlySelectedRank.rankDef);
                    if (ModSettings.confirmRankUnlock)
                    {
                        var window = Dialog_MessageBox.CreateConfirmation("BEWH.Framework.RankSystem.UnlockRankConfirm".Translate(pawn, currentlySelectedRank.rankDef.label), Action, destructive: true);
                        Find.WindowStack.Add(window);
                    }
                    else
                    {
                        Action();
                    }
                }
                listingRankInfo.Outdent(viewRect.width * 0.25f);
            }
                
            listingRankInfo.GapLine(1f);
            listingRankInfo.Indent(viewRect.width * 0.02f);
            
            //Description
            listingRankInfo.Gap();
            Text.Anchor = TextAnchor.UpperLeft;
            listingRankInfo.Label("BEWH.Framework.RankSystem.RankDescription".Translate());
            Text.Font = GameFont.Small;
            listingRankInfo.Label(currentlySelectedRank.rankDef.description);

            //Requirements
            listingRankInfo.Gap();
            Text.Font = GameFont.Medium;
            listingRankInfo.Label("BEWH.Framework.RankSystem.RankRequirements".Translate());
            Text.Font = GameFont.Small;
            listingRankInfo.Label(currentlySelectedRank.rankText);
                
            //Given stats
            listingRankInfo.Gap();
            Text.Font = GameFont.Medium;
            listingRankInfo.Label("BEWH.Framework.RankSystem.RankBonuses".Translate());
            Text.Font = GameFont.Small;
            listingRankInfo.Label(currentlySelectedRank.rankBonusText);

            //End
            listingRankInfo.Outdent(viewRect.width * 0.02f);
            scrollViewHeightRankInfo = listingRankInfo.CurHeight + rankIconMargin;
            listingRankInfo.End();
            Widgets.EndScrollView();
        }
        else
        {
            Text.Anchor = TextAnchor.MiddleCenter;
            var text = "BEWH.Framework.RankSystem.NoRankSelected".Translate();
            Widgets.Label(rectRankInfo, text);
            Text.Anchor = TextAnchor.UpperLeft;
        }
    }

    private void UpdateRankCategoryList()
    {
        availableCategories = DefDatabase<RankCategoryDef>.AllDefsListForReading;
    }

    private void GetRanksForCategory()
    {
        availableRanksForCategory = [];
        cachedYAndX = null;

        if (currentlySelectedRankCategory == null)
        {
            return;
        }
        
        foreach (var rank in currentlySelectedRankCategory.ranks)
        {
            var rankInfo = BuildRankInfoForCategory(rank.rankDef);
            availableRanksForCategory.Add(rankInfo);
        }
    }

    private RankInfoForTab BuildRankInfoForCategory(RankDef rankDef)
    {
        var reqMet = rankDef.RequirementMet(new StringBuilder(), pawn, compRankInfo, currentlySelectedRankCategory, out var reason);
        
        return new RankInfoForTab
        {
            rankDef = rankDef,
            requirementsMet = reqMet,
            rankText = reason,
            rankBonusText = rankDef.GetRankBonusString(),
        };
    }
        
    private bool AlreadyUnlocked(RankDef rankDef)
    {
        return compRankInfo != null && compRankInfo.HasRank(rankDef);
    }

    private static void DrawIcon(Rect inRect, Texture2D icon, bool drawBg)
    {
        var color = Mouse.IsOver(inRect) ? GenUI.MouseoverColor : Color.white;
        GUI.color = color;
        if (drawBg)
        {
            GUI.DrawTexture(inRect, Command.BGTexShrunk);
        }
        GUI.color = Color.white;
        GUI.DrawTexture(inRect, icon);
    }
        
    private void UnlockRank(RankDef rank)
    {
        compRankInfo.UnlockRank(rank);
        redoRankInfo = true;
    }
}

internal class RankInfoForTab
{
    public RankDef rankDef;
    public bool requirementsMet;
    public string rankText;
    public string rankBonusText;
}