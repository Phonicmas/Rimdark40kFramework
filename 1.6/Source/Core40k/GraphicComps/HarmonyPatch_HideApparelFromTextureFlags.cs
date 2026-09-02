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
