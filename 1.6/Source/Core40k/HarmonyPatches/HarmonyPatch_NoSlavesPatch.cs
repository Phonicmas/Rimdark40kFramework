using HarmonyLib;
using RimWorld;
using System.Collections.Generic;
using Verse;

namespace Core40k;

[HarmonyPatch(typeof(StockGenerator_Slaves), "GenerateThings")]
public class DisallowSlaveBuyingPatch
{
	public static IEnumerable<Thing> Postfix(IEnumerable<Thing> __result)
	{
		var newResult = new List<Thing>();

		foreach (var thing in __result)
		{
			var addThing = true;

			if (thing is Pawn pawn && pawn.genes?.Xenotype != null
			    && pawn.genes.Xenotype.HasModExtension<DefModExtension_UntradeablePawn>())
			{
				addThing = false;
			}
			if (addThing)
			{
				newResult.Add(thing);
			}
		}

		return newResult;
	}
}