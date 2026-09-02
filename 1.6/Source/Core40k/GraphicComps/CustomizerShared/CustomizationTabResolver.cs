using System.Collections.Generic;
using System.Linq;
using Verse;

namespace Core40k;

public static class CustomizationTabResolver
{
    private static readonly Dictionary<ThingDef, List<CustomizationTabDef>> cache = new();

    private static readonly List<CustomizationTabDef> Empty = [];
    
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
