using HarmonyLib;
using RimWorld.Planet;
using UnityEngine;

namespace CoatOfArms;

[HarmonyPatch(typeof(WorldObject), nameof(WorldObject.ExpandingIconColor), MethodType.Getter)]
public static class Patch_ExpandingIconColor
{
    public static bool Prefix(WorldObject __instance, ref Color __result)
    {
        if (CoatOfArmsComponent.Instance == null)
            return true;
        if (__instance is not Settlement settlement || settlement.Faction == null)
            return true;
        if (!CoatOfArmsComponent.Instance.HasCustomCoatOfArms(settlement.Faction))
            return true;

        __result = Color.white;
        return false;
    }
}
