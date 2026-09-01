using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace Core40k;

//Implemented by both customization dialogs so the job driver can wait for whichever one it opened
//without caring which.
public interface ICustomizationDialog
{
    bool Closed { get; }
}

//Totals up what a sitting at the styling station costs, and drives the capture / commit / discard
//of deferred changes across everything the pawn is wearing and holding.
//
//Work model:
//  adding a decoration or upgrade   workAmount, in full, unlocked or not
//  removing one                     workAmount * removalWorkFactor
//  any colour / mask / base texture change   a flat charge, once per item
//  anything at all changed          never less than the configured minimum
public static class DecorationWorkUtility
{
    private static Core40kModSettings modSettings;
    private static Core40kModSettings ModSettings => modSettings ??= LoadedModManager.GetMod<Core40kMod>().GetSettings<Core40kModSettings>();

    //The two switches are independent. Work off does not make upgrades free, cost off does not make
    //them instant. Single point of truth so the tab, the def tooltip and the comps all agree.
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

    //The appearance charge is per item rather than per comp, so an item whose colour and whose
    //decoration masks both changed is billed once, not twice.
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

    //Price of the live edits, for the confirm dialog. Nothing has been captured yet at this point.
    //Scoped to the comps the dialog actually edits rather than everything the pawn is carrying:
    //a comp no tab touched this session has no meaningful live-versus-committed diff to read.
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

    //Price of the captured change, for the job.
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

    //Used when there is no work to do at all, so edits (a dev mode draw data tweak, for instance)
    //still stick the way they always did.
    public static void CommitImmediately(Pawn pawn, List<CompGraphicParent> comps)
    {
        foreach (var comp in comps)
        {
            comp.SetOriginals();
        }

        pawn?.Drawer?.renderer?.SetAllGraphicsDirty();
    }

    //Accept handler shared by both customization dialogs.
    //Either commits straight away, or prices the change and asks for confirmation before capturing
    //it for the pawn to work off. `closeDialog` only runs on the paths that actually go through.
    public static void TryAccept(Pawn pawn, IEnumerable<CompGraphicParent> dialogComps, System.Action closeDialog)
    {
        //Distinct because the Decoration tab and the Upgrades tab hold the same comps, and
        //capturing one twice would let the second pass wipe the first snapshot.
        var comps = dialogComps.Distinct().ToList();

        //CollectEditCost already returns nothing when cost is switched off, so this is empty then.
        var cost = PreviewCost(comps);
        var work = WorkEnabled ? PreviewWork(comps) : 0f;

        //Nothing to pay and nothing to work off: behave exactly as the dialog always did.
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
            //Dialog stays open so the player can drop whatever they cannot pay for.
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

            //With work switched off there is no job to run the commit, so settle here. Going
            //through capture/commit rather than CommitImmediately is what records the unlocks.
            if (work <= 0f)
            {
                SettleFromMap(pawn);
            }

            closeDialog();
        }));
    }

    //Take the materials straight out of storage and commit. Only for the work-disabled path, where
    //there is no job and so nothing to haul with.
    public static void SettleFromMap(Pawn pawn)
    {
        Settle(pawn, UpgradeCostUtility.Consume(pawn, PendingCost(pawn)));
    }

    //Spend what the pawn hauled into its inventory, then commit. Used by the job.
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
