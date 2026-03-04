using HarmonyLib;
using RimWorld.Planet;
using UnityEngine;

namespace CoatOfArms;

[HarmonyPatch(typeof(WorldObject), nameof(WorldObject.ExpandingIconColor), MethodType.Getter)]
public static class Patch_ExpandingIconColor
{
    [HarmonyPriority(Priority.Last)]
    public static bool Prefix(WorldObject __instance, ref Color __result)
    {
        if (CoatOfArmsComponent.Instance == null)
            return true;
        if (__instance.Faction == null)
            return true;

        bool hasCustom = CoatOfArmsComponent.Instance.HasCustomCoatOfArms(__instance.Faction);

        if (Patch_ExpandableWorldObjectsOnGUI.Drawing)
        {
            if (hasCustom)
            {
                Texture2D texture = CoatOfArmsComponent.Instance.GetTextureForFaction(__instance.Faction);
                if (texture != null)
                {
                    Patch_ExpandableWorldObjectsOnGUI.SaveAndSwap(__instance.Faction.def, texture);
                    __result = Color.white;
                    return false;
                }
            }
            else
            {
                Patch_ExpandableWorldObjectsOnGUI.Restore(__instance.Faction.def);
            }
        }

        if (hasCustom)
        {
            __result = Color.white;
            return false;
        }

        return true;
    }
}
