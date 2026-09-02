using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace Core40k;

public interface ICustomizationDialog
{
    bool Closed { get; }
}

public static class DecorationWorkUtility
{
    private static Core40kModSettings modSettings;
    private static Core40kModSettings ModSettings => modSettings ??= LoadedModManager.GetMod<Core40kMod>().GetSettings<Core40kModSettings>();
    
    public static bool WorkEnabled => ModSettings.decorationWorkEnabled;
    public static bool CostEnabled => ModSettings.decorationCostEnabled;

    public static List<CompGraphicParent> GraphicComps(Pawn pawn)
    {
        var result = new List<CompGraphicParent>();
        if (pawn == null)
        {
            return result;
        }

        if (pawn.apparel != null)
        {
            foreach (var apparel in pawn.apparel.WornApparel)
            {
                Collect(apparel, result);
            }
        }

        if (pawn.equipment != null)
        {
            foreach (var equipment in pawn.equipment.AllEquipmentListForReading)
            {
                Collect(equipment, result);
            }
        }

        return result;
    }

    private static void Collect(ThingWithComps thing, List<CompGraphicParent> into)
    {
        if (thing?.AllComps == null)
        {
            return;
        }

        foreach (var comp in thing.AllComps)
        {
            if (comp is CompGraphicParent graphicComp)
            {
                into.Add(graphicComp);
            }
        }
    }

    private static float AppearanceWork(IEnumerable<CompGraphicParent> comps, bool pending)
    {
        var items = comps
            .Where(comp => pending ? comp.PendingAppearanceChange : comp.HasAppearanceEdit)
            .Select(comp => comp.parent)
            .Distinct()
            .Count();

        return items * ModSettings.appearanceChangeWorkAmount;
    }

    private static float Total(List<CompGraphicParent> comps, bool pending)
    {
        var relevant = comps.Where(comp => pending ? comp.HasPendingChange : comp.HasEdits).ToList();
        if (relevant.Count == 0)
        {
            return 0f;
        }

        var work = relevant.Sum(comp => pending ? comp.PendingWork : comp.EditWork);
        work += AppearanceWork(relevant, pending);

        return Mathf.Max(work, ModSettings.minimumWorkAmount);
    }

    public static float PreviewWork(List<CompGraphicParent> comps)
    {
        return Total(comps, pending: false);
    }

    public static List<ThingDefCountClass> PreviewCost(List<CompGraphicParent> comps)
    {
        var cost = new List<ThingDefCountClass>();
        foreach (var comp in comps)
        {
            comp.CollectEditCost(cost);
        }
        return cost;
    }

    public static float PendingWork(Pawn pawn)
    {
        return Total(GraphicComps(pawn), pending: true);
    }

    public static List<ThingDefCountClass> PendingCost(Pawn pawn)
    {
        var cost = new List<ThingDefCountClass>();
        if (!CostEnabled)
        {
            return cost;
        }

        foreach (var comp in GraphicComps(pawn))
        {
            comp.CollectPendingCost(cost);
        }
        return cost;
    }

    public static bool AnyPending(Pawn pawn)
    {
        return GraphicComps(pawn).Any(comp => comp.HasPendingChange);
    }

    public static void CaptureAll(List<CompGraphicParent> comps)
    {
        foreach (var comp in comps)
        {
            comp.CapturePending();
        }
    }

    public static void CommitAll(Pawn pawn)
    {
        foreach (var comp in GraphicComps(pawn))
        {
            comp.CommitPending();
        }

        pawn?.Drawer?.renderer?.SetAllGraphicsDirty();
    }

    public static void DiscardAll(Pawn pawn)
    {
        foreach (var comp in GraphicComps(pawn))
        {
            comp.DiscardPending();
        }
    }

    public static void CommitImmediately(Pawn pawn, List<CompGraphicParent> comps)
    {
        foreach (var comp in comps)
        {
            comp.SetOriginals();
        }

        pawn?.Drawer?.renderer?.SetAllGraphicsDirty();
    }

    public static void TryAccept(Pawn pawn, IEnumerable<CompGraphicParent> dialogComps, System.Action closeDialog)
    {
        var comps = dialogComps.Distinct().ToList();

        var cost = PreviewCost(comps);
        var work = WorkEnabled ? PreviewWork(comps) : 0f;

        if (work <= 0f && cost.NullOrEmpty())
        {
            CommitImmediately(pawn, comps);
            closeDialog();
            return;
        }

        if (!UpgradeCostUtility.CanAfford(pawn, cost, out var missing))
        {
            Messages.Message(
                "BEWH.Framework.Customization.MissingResource".Translate(missing.thingDef.LabelCap, missing.count),
                MessageTypeDefOf.RejectInput,
                false);
            return;
        }

        var costText = cost.NullOrEmpty()
            ? "BEWH.Framework.Customization.CostNothing".Translate().ToString()
            : UpgradeCostUtility.CostToString(cost);

        var text = work > 0f
            ? "BEWH.Framework.Customization.ConfirmWork".Translate(costText, work.ToString("F0"))
            : "BEWH.Framework.Customization.ConfirmCostOnly".Translate(costText);

        Find.WindowStack.Add(Dialog_MessageBox.CreateConfirmation(text, delegate
        {
            CaptureAll(comps);

            if (work <= 0f)
            {
                SettleFromMap(pawn);
            }

            closeDialog();
        }));
    }

    public static void SettleFromMap(Pawn pawn)
    {
        Settle(pawn, UpgradeCostUtility.Consume(pawn, PendingCost(pawn)));
    }

    public static void SettleFromInventory(Pawn pawn)
    {
        Settle(pawn, UpgradeCostUtility.ConsumeFromInventory(pawn, PendingCost(pawn)));
    }

    private static void Settle(Pawn pawn, bool paid)
    {
        if (!paid)
        {
            Messages.Message(
                "BEWH.Framework.Customization.NotEnoughResources".Translate(),
                pawn,
                MessageTypeDefOf.NegativeEvent,
                false);
            DiscardAll(pawn);
            return;
        }

        CommitAll(pawn);
    }
}
