using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using Verse;

namespace Core40k;

/// <summary>
/// Optional integration with Dual Wield (Meme Goddess' 1.6 continuation of roolo's mod). Nothing
/// here is referenced unless that mod is loaded, and every lookup is by name, so the framework
/// carries no dependency on it.
///
/// Dual Wield side, for reference: an off-hand weapon does not go through Verb.TryStartCastOn at
/// all. DualWield.Ext_Verb.OffhandTryStartCastOn is a hand-written copy of it that drives the
/// pawn's second stance tracker, and it decides the aim delay from
///
///     if (instance.CasterIsPawn &amp;&amp; instance.verbProps.warmupTime > 0f)
///     ...
///     int ticks = (instance.verbProps.warmupTime * statValue).SecondsToTicks();
///
/// Reading the raw VerbProperties field skips the Verb.WarmupTime property, and with it every
/// postfix on that getter - so DefModExtension_AmmoChanger.warmupTime and a decoration's
/// verbModifier.additionalWarmupTime silently did nothing while a weapon was held off-hand. A
/// firing mode meant to aim slower fired at the weapon's base speed in the left hand.
///
/// This transpiler collapses both "ldfld Verb::verbProps; ldfld VerbProperties::warmupTime" pairs
/// into a single call to the property. The Verb reference the two field loads were walking is
/// already on the stack, so the result is the same value vanilla would have produced, plus our
/// modifiers. Nothing else in the method is touched.
/// </summary>
public static class DualWieldCompat
{
    private const string ExtVerbTypeName = "DualWield.Ext_Verb";
    private const string OffhandCastMethodName = "OffhandTryStartCastOn";

    private static readonly FieldInfo VerbPropsField = AccessTools.Field(typeof(Verb), nameof(Verb.verbProps));
    private static readonly FieldInfo WarmupTimeField = AccessTools.Field(typeof(VerbProperties), nameof(VerbProperties.warmupTime));
    private static readonly MethodInfo WarmupTimeGetter = AccessTools.PropertyGetter(typeof(Verb), nameof(Verb.WarmupTime));

    public static bool Active { get; private set; }

    /// <summary>
    /// Called from Core40kMod's constructor, after PatchAll. Attribute-driven patches cannot be used
    /// here - PatchAll resolves their targets eagerly and would throw when Dual Wield is absent.
    /// All mod assemblies are loaded before any Mod class is constructed, so this does not depend on
    /// load order.
    /// </summary>
    public static void Apply(Harmony harmony)
    {
        // The type resolving at all is the check: if Dual Wield is not loaded, it is not there.
        var extVerb = AccessTools.TypeByName(ExtVerbTypeName);
        if (extVerb == null)
        {
            return;
        }

        if (VerbPropsField == null || WarmupTimeField == null || WarmupTimeGetter == null)
        {
            Log.Warning("[RimDark Framework] Could not resolve Verb.verbProps, VerbProperties.warmupTime or Verb.WarmupTime, so Dual Wield off-hand weapons will keep ignoring ammo and decoration warmup times.");
            return;
        }

        var target = AccessTools.Method(extVerb, OffhandCastMethodName, [typeof(Verb), typeof(LocalTargetInfo)]);
        if (target == null)
        {
            Log.Warning("[RimDark Framework] Dual Wield is present but DualWield.Ext_Verb.OffhandTryStartCastOn(Verb, LocalTargetInfo) was not found, so off-hand weapons will keep ignoring ammo and decoration warmup times. This usually means Dual Wield changed its off-hand casting code.");
            return;
        }

        try
        {
            harmony.Patch(target, transpiler: new HarmonyMethod(typeof(DualWieldCompat), nameof(UseVerbWarmupTime)));
            Active = true;
        }
        catch (Exception e)
        {
            Log.Warning($"[RimDark Framework] Failed to patch Dual Wield's off-hand casting for warmup times: {e}");
        }
    }

    public static IEnumerable<CodeInstruction> UseVerbWarmupTime(IEnumerable<CodeInstruction> instructions)
    {
        var code = new List<CodeInstruction>(instructions);
        var replaced = 0;

        for (var i = 0; i < code.Count - 1; i++)
        {
            if (!code[i].LoadsField(VerbPropsField) || !code[i + 1].LoadsField(WarmupTimeField))
            {
                continue;
            }

            //The Verb is already on the stack, so the two field loads collapse into one virtual
            //call. Labels and exception blocks from both instructions have to come along or a
            //branch into this spot would point at nothing.
            var call = new CodeInstruction(OpCodes.Callvirt, WarmupTimeGetter);
            call.labels.AddRange(code[i].labels);
            call.labels.AddRange(code[i + 1].labels);
            call.blocks.AddRange(code[i].blocks);
            call.blocks.AddRange(code[i + 1].blocks);

            code[i] = call;
            code.RemoveAt(i + 1);
            replaced++;
        }

        if (replaced == 0)
        {
            Log.Warning("[RimDark Framework] Dual Wield's OffhandTryStartCastOn no longer reads verbProps.warmupTime, so nothing was changed - off-hand weapons will keep ignoring ammo and decoration warmup times.");
        }

        return code;
    }
}
