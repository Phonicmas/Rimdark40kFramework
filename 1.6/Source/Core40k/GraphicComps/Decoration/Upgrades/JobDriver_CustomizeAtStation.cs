using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace Core40k;

//Walk to the station, open the customization dialog, fetch whatever materials the accepted change
//needs, then work off the combined work amount. Nothing takes effect until that work finishes.
//
//Kept as one job rather than queueing a second one after the dialog closes, so the station stays
//reserved and the pawn cannot wander off between pressing Accept and starting the work.
public abstract class JobDriver_CustomizeAtStation : JobDriver
{
    private int totalWorkTicks = -1;

    private ICustomizationDialog dialog;

    protected abstract Window MakeDialog();

    public override bool TryMakePreToilReservations(bool errorOnFailed)
    {
        return pawn.Reserve(job.targetA, job, 1, -1, null, errorOnFailed);
    }

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Values.Look(ref totalWorkTicks, "totalWorkTicks", -1);
    }

    protected override IEnumerable<Toil> MakeNewToils()
    {
        AddFinishAction(delegate
        {
            //Anything still uncommitted when the job ends was never paid for, so it is simply
            //dropped and the gear is left exactly as it was. Materials the pawn already picked up
            //stay in its inventory and get hauled back to storage by the usual behaviour.
            DecorationWorkUtility.DiscardAll(pawn);
        });

        yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.InteractionCell).FailOnDespawnedOrNull(TargetIndex.A);

        yield return Toils_General.Do(delegate
        {
            var window = MakeDialog();
            dialog = window as ICustomizationDialog;
            Find.WindowStack.Add(window);
        });

        //The dialog pauses the game, so no ticks pass while the player is in it.
        var waitForDialog = new Toil
        {
            defaultCompleteMode = ToilCompleteMode.Never,
        };
        waitForDialog.tickAction = delegate
        {
            if (dialog == null || dialog.Closed)
            {
                ReadyForNextToil();
            }
        };
        yield return waitForDialog;

        //Work out which stacks in storage are paying for this and reserve them, before the pawn
        //walks anywhere.
        yield return Toils_General.Do(QueueIngredients);

        var collectNext = Toils_JobTransforms.ExtractNextTargetFromQueue(TargetIndex.B);
        var afterCollecting = Toils_General.Label();

        yield return Toils_Jump.JumpIf(afterCollecting, () => job.GetTargetQueue(TargetIndex.B).NullOrEmpty());

        yield return collectNext;
        yield return Toils_Goto.GotoThing(TargetIndex.B, PathEndMode.ClosestTouch)
            .FailOnDespawnedNullOrForbidden(TargetIndex.B);
        yield return Toils_Haul.TakeToInventory(TargetIndex.B, () => job.count);
        yield return Toils_Jump.JumpIf(collectNext, () => !job.GetTargetQueue(TargetIndex.B).NullOrEmpty());

        //Back to the station with the materials.
        yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.InteractionCell).FailOnDespawnedOrNull(TargetIndex.A);

        yield return afterCollecting;

        var work = new Toil
        {
            defaultCompleteMode = ToilCompleteMode.Delay,
        };
        work.initAction = delegate
        {
            var totalWork = DecorationWorkUtility.PendingWork(pawn);
            if (totalWork <= 0f)
            {
                ReadyForNextToil();
                return;
            }

            //ticksLeftThisToil is assigned from defaultDuration before initAction runs, so setting
            //it here is what actually drives the delay. It is scribed by JobDriver, so an
            //interrupted refit resumes where it left off.
            var speed = Mathf.Max(pawn.GetStatValue(StatDefOf.GeneralLaborSpeed), 0.01f);
            totalWorkTicks = Mathf.Max(1, Mathf.CeilToInt(totalWork / speed));
            ticksLeftThisToil = totalWorkTicks;
        };
        //Not WithProgressBarToilDelay: that one divides by Toil.defaultDuration, which is 0 here
        //because the duration is worked out at runtime rather than baked into the toil.
        work.WithProgressBar(TargetIndex.A, () => totalWorkTicks <= 0
            ? 0f
            : 1f - (float)ticksLeftThisToil / totalWorkTicks);
        work.FailOnDespawnedOrNull(TargetIndex.A);
        yield return work;

        //Spend what was hauled and apply everything. All or nothing.
        yield return Toils_General.Do(delegate { DecorationWorkUtility.SettleFromInventory(pawn); });
    }

    private void QueueIngredients()
    {
        var cost = DecorationWorkUtility.PendingCost(pawn);
        if (cost.NullOrEmpty())
        {
            return;
        }

        var ingredients = UpgradeCostUtility.FindIngredients(pawn, cost);
        if (ingredients == null)
        {
            Messages.Message(
                "BEWH.Framework.Customization.NotEnoughResources".Translate(),
                pawn,
                MessageTypeDefOf.NegativeEvent,
                false);
            EndJobWith(JobCondition.Incompletable);
            return;
        }

        var targetQueue = job.GetTargetQueue(TargetIndex.B);
        job.countQueue ??= [];

        foreach (var ingredient in ingredients)
        {
            //Someone else may already be spoken for this stack; bail rather than double-book it.
            if (!pawn.Reserve(ingredient.Thing, job, 1, ingredient.Count, null, false))
            {
                Messages.Message(
                    "BEWH.Framework.Customization.NotEnoughResources".Translate(),
                    pawn,
                    MessageTypeDefOf.NegativeEvent,
                    false);
                EndJobWith(JobCondition.Incompletable);
                return;
            }

            targetQueue.Add(ingredient.Thing);
            job.countQueue.Add(ingredient.Count);
        }
    }
}
