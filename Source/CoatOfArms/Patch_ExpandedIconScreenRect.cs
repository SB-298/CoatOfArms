using HarmonyLib;
using RimWorld.Planet;
using UnityEngine;

namespace CoatOfArms;

[HarmonyPatch(typeof(ExpandableWorldObjectsUtility), nameof(ExpandableWorldObjectsUtility.ExpandedIconScreenRect))]
public static class Patch_ExpandedIconScreenRect
{
    public static void Postfix(WorldObject o, ref Rect __result)
    {
        bool hasCoat = o is Settlement settlement
            && settlement.Faction != null
            && CoatOfArmsComponent.Instance != null
            && CoatOfArmsComponent.Instance.HasCustomCoatOfArms(settlement.Faction);

        float scale = hasCoat ? CoatOfArmsSettings.CoatIconScale : CoatOfArmsSettings.VanillaIconScale;
        if (scale == 1f)
            return;

        float width = __result.width * scale;
        float height = __result.height * scale;
        Vector2 center = __result.center;
        __result = new Rect(center.x - width / 2f, center.y - height / 2f, width, height);
    }
}
