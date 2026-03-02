using HarmonyLib;
using RimWorld.Planet;
using UnityEngine;

namespace CoatOfArms;

[HarmonyPatch(typeof(WorldObject), nameof(WorldObject.ExpandingIcon), MethodType.Getter)]
public static class Patch_ExpandingIcon
{
    public static bool Prefix(WorldObject __instance, ref Texture2D __result)
    {
        if (CoatOfArmsComponent.Instance == null)
            return true;
        if (__instance is not Settlement settlement || settlement.Faction == null)
            return true;
        if (!CoatOfArmsComponent.Instance.HasCustomCoatOfArms(settlement.Faction))
            return true;

        Texture2D texture = settlement.Faction.def.factionIcon;
        if (texture != null)
        {
            __result = texture;
            return false;
        }
        return true;
    }
}
