using System;
using System.Collections.Generic;
using Verse;

namespace Core40k;

public class CustomizationTabDef : Def
{
    public Type tabDrawerClass;
    public int sortOrder = 1;
    
    public Type workerClass = typeof(CustomizationTabWorker_Comp);
    
    public List<Type> requiredComps = [];

    public TabTargetKind targetKind = TabTargetKind.Any;

    [Unsaved]
    private CustomizationTabWorker workerInt;

    public CustomizationTabWorker Worker
    {
        get
        {
            if (workerInt != null)
            {
                return workerInt;
            }

            workerInt = (CustomizationTabWorker)Activator.CreateInstance(workerClass);
            workerInt.def = this;
            return workerInt;
        }
    }

    public override IEnumerable<string> ConfigErrors()
    {
        foreach (var configError in base.ConfigErrors())
        {
            yield return configError;
        }

        if (tabDrawerClass == null)
        {
            yield return "tabDrawerClass is null";
        }

        if (workerClass == null)
        {
            yield return "workerClass is null";
        }
        else if (!typeof(CustomizationTabWorker).IsAssignableFrom(workerClass))
        {
            yield return "workerClass " + workerClass + " does not derive from CustomizationTabWorker";
        }
    }
}
