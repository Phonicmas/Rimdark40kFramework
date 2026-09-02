using HarmonyLib;
using RimWorld;
using Verse;

namespace Core40k;

[HarmonyPatch(typeof(Apparel), "WornGraphicPath", MethodType.Getter)]
public class HideApparelFromTextureFlags
{
    public static void Postfix(ref string __result, Apparel __instance)
    {
        var wornApparel = __instance?.Wearer?.apparel?.WornApparel;
        if (wornApparel == null)
        {
            return;
        }

        //This is a getter that gets read a lot, and it used to walk every worn item and every one
        //of its flags even after it had already found a match.
        foreach (var apparel in wornApparel)
        {
            var defMod = apparel.def.GetModExtension<DefModExtension_TextureFlags>();
            if (defMod == null)
            {
                continue;
            }

            foreach (var flag in defMod.textureFlags)
            {
                if (!flag.hideThing || flag.thingActivator != __instance.def || flag.hideTexPath.NullOrEmpty())
                {
                    continue;
                }

                __result = flag.hideTexPath;
                return;
            }
        }
    }
}
