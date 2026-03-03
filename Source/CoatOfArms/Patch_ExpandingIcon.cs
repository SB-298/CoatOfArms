using HarmonyLib;
using RimWorld.Planet;
using UnityEngine;

namespace CoatOfArms;

[HarmonyPatch(typeof(Settlement), nameof(Settlement.ExpandingIcon), MethodType.Getter)]
public static class Patch_ExpandingIcon
{
    public static bool Prefix(Settlement __instance, ref Texture2D __result)
    {
        if (CoatOfArmsComponent.Instance == null)
            return true;
        if (__instance.Faction == null)
            return true;
        if (!CoatOfArmsComponent.Instance.HasCustomCoatOfArms(__instance.Faction))
            return true;

        Texture2D texture = CoatOfArmsComponent.Instance.GetTextureForFaction(__instance.Faction);
        if (texture != null)
        {
            __result = texture;
            return false;
        }
        return true;
    }
}
