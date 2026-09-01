using System;
using System.Collections.Generic;
using Verse;

namespace Core40k;

//Deprecated. Customization tabs are resolved automatically by CustomizationTabResolver from the
//comps an item carries and the content that applies to it, and this extension is never read.
//The class is kept for one version so existing XML across the content mods still parses instead of
//erroring on load; DecorationIndex logs one aggregated warning naming the defs that still have it.
//Remove at 1.7 alongside the other renames queued there.
[Obsolete("Customization tabs are detected automatically. This extension is ignored and will be removed at 1.7.")]
public class DefModExtension_AvailableDrawerTabDefs : DefModExtension
{
    public List<CustomizationTabDef> tabDefs = [];
}
