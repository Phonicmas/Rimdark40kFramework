using System.Collections.Generic;
using System.Text;
using RimWorld;
using Verse;

namespace Core40k;

//TODO: Rename to ArmorDecorationDef at 1.7
public class ExtraDecorationDef : DecorationDef
{
    public bool drawInHeadSpace = false;
    
    public bool decoSizeMatchesThingSize = false;
    
    public List<Rot4> defaultShowRotation = [Rot4.North, Rot4.South, Rot4.East, Rot4.West];

    //CanDrawNow runs per render node, per pawn, per frame. It used to allocate a List<Rot4> on
    //every call, and run a LINQ projection to build the flipped set from scratch each time.
    [Unsaved]
    private List<Rot4> flippedShowRotation;

    public List<Rot4> ShowRotation(bool flipped)
    {
        if (!flipped)
        {
            return defaultShowRotation;
        }

        if (flippedShowRotation != null)
        {
            return flippedShowRotation;
        }

        flippedShowRotation = [];
        if (defaultShowRotation != null)
        {
            foreach (var rotation in defaultShowRotation)
            {
                flippedShowRotation.Add(rotation.Opposite);
            }
        }

        return flippedShowRotation;
    }
    
    public List<BodyTypeDef> appliesToBodyTypes = [];

    public override bool HasRequirements(Pawn pawn, out string lockedReason)
    {
        var requirementFulfilled = base.HasRequirements(pawn, out lockedReason);
        if (appliesToBodyTypes.NullOrEmpty())
        {
            return requirementFulfilled;
        }
        //Same reason as the base checks: no apparel tracker means there is nothing to measure against.
        if (pawn?.apparel == null)
        {
            return requirementFulfilled;
        }
        var bodyApparel = pawn.apparel.WornApparel.FirstOrFallback(a => a.HasComp<CompDecorative>());
        if (bodyApparel == null)
        {
            return requirementFulfilled;
        }
        
        var reason = new StringBuilder();
        reason.AppendLine(lockedReason);
        
        var pawnBodyType = BodyTypeUtils.SafeBodyType(
            pawn, bodyApparel.def?.GetModExtension<DefModExtension_ForcesBodyType>()?.forcedBodyType);
        if (!BodyTypeUtils.MatchesAny(pawnBodyType, appliesToBodyTypes, out _))
        {
            reason.AppendLine("BEWH.Framework.Customization.InvalidBodytype".Translate());
            lockedReason = reason.ToString();
            requirementFulfilled = false;
        }
        
        return requirementFulfilled;
    }
}