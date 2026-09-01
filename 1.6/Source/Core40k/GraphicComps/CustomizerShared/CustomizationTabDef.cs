using System;
using System.Collections.Generic;
using Verse;

namespace Core40k;

public class CustomizationTabDef : Def
{
    public Type tabDrawerClass;
    public int sortOrder = 1;

    //Decides which items this tab shows up on. See CustomizationTabWorker.
    public Type workerClass = typeof(CustomizationTabWorker_Comp);

    //Comps the tab's drawer operates on. Matched against subclasses too, so listing
    //CompDecorativeBase also matches CompDecorative and CompWeaponDecoration.
    public List<Type> requiredComps = [];

    //Needed because the armor and weapon coloring tabs both require only CompMultiColor and are
    //otherwise indistinguishable. Also keeps weapon tabs out of the apparel dialog.
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
