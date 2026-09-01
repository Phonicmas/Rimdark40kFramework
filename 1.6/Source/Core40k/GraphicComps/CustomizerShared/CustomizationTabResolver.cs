using System.Collections.Generic;
using System.Linq;
using Verse;

namespace Core40k;

//Which customization tabs an item shows, derived from its comps and the content that applies to it.
//Nothing overrides this - DefModExtension_AvailableDrawerTabDefs is deprecated and ignored.
//Resolved lazily per ThingDef and cached; the first customize on an item pays a handful of
//dictionary lookups and everything after is a cache hit.
public static class CustomizationTabResolver
{
    private static readonly Dictionary<ThingDef, List<CustomizationTabDef>> cache = new();

    private static readonly List<CustomizationTabDef> Empty = [];

    //The returned list is owned by the resolver. Callers must treat it as read only - the old code
    //handed out the def's own list and then mutated it, which grew that list on every dialog open.
    public static List<CustomizationTabDef> TabsFor(ThingDef thingDef)
    {
        if (thingDef == null)
        {
            return Empty;
        }

        if (cache.TryGetValue(thingDef, out var cached))
        {
            return cached;
        }

        var tabs = DefDatabase<CustomizationTabDef>.AllDefs
            .Where(tab => tab.Worker.AppliesTo(thingDef))
            .ToList();

        tabs.SortBy(tab => tab.sortOrder);
        cache.Add(thingDef, tabs);
        return tabs;
    }

    public static bool HasAnyTab(ThingDef thingDef)
    {
        return TabsFor(thingDef).Count > 0;
    }
}
