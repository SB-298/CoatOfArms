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

        bool hasCustom = CoatOfArmsComponent.Instance.HasCustomCoatOfArms(settlement.Faction);

        if (Patch_ExpandableWorldObjectsOnGUI.Drawing)
        {
            if (hasCustom)
            {
                Texture2D texture = CoatOfArmsComponent.Instance.GetTextureForFaction(settlement.Faction);
                if (texture != null)
                {
                    Patch_ExpandableWorldObjectsOnGUI.SaveAndSwap(settlement.Faction.def, texture);
                    __result = Color.white;
                    return false;
                }
            }
            else
            {
                Patch_ExpandableWorldObjectsOnGUI.Restore(settlement.Faction.def);
            }
        }

        if (settlement.Faction.IsPlayer && hasCustom)
        {
            __result = Color.white;
            return false;
        }

        return true;
    }
}
