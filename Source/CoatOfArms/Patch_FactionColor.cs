using HarmonyLib;
using RimWorld;
using UnityEngine;

namespace CoatOfArms;

[HarmonyPatch(typeof(Faction), nameof(Faction.Color), MethodType.Getter)]
public static class Patch_FactionColor
{
    public static bool Prefix(Faction __instance, ref Color __result)
    {
        if (CoatOfArmsComponent.Instance == null)
            return true;
        if (!CoatOfArmsComponent.Instance.HasCustomCoatOfArms(__instance))
            return true;

        __result = Color.white;
        return false;
    }
}
