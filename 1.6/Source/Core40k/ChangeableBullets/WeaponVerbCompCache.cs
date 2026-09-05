using System.Runtime.CompilerServices;
using Verse;

namespace Core40k;

/// <summary>
/// Resolves the ammo changer and weapon decoration comps of a piece of equipment once and keeps
/// them for the life of the instance, so the verb getters do not scan the comp list on every read.
/// </summary>
public static class WeaponVerbCompCache
{
    private sealed class Entry
    {
        public Comp_AmmoChanger ammoChanger;
        public CompWeaponDecoration weaponDecoration;
    }

    private static readonly ConditionalWeakTable<ThingWithComps, Entry> entries = new();

    public static bool TryGet(ThingWithComps equipment, out Comp_AmmoChanger ammoChanger, out CompWeaponDecoration weaponDecoration)
    {
        ammoChanger = null;
        weaponDecoration = null;
        if (!Core40kUtils.CanModifyVerbs(equipment))
        {
            return false;
        }

        var entry = entries.GetValue(equipment, static weapon => new Entry
        {
            ammoChanger = weapon.GetComp<Comp_AmmoChanger>(),
            weaponDecoration = weapon.GetComp<CompWeaponDecoration>(),
        });

        ammoChanger = entry.ammoChanger;
        weaponDecoration = entry.weaponDecoration;
        return ammoChanger != null || weaponDecoration != null;
    }
}
