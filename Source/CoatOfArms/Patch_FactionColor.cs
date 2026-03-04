using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace CoatOfArms;

[HarmonyPatch(typeof(Faction), nameof(Faction.Color), MethodType.Getter)]
public static class Patch_FactionColor
{
    [HarmonyPriority(Priority.Last)]
    public static bool Prefix(Faction __instance, ref Color __result)
    {
        if (CoatOfArmsComponent.Instance == null)
            return true;

        bool hasCustom = CoatOfArmsComponent.Instance.HasCustomCoatOfArms(__instance);

        if (Patch_FactionTabUI.Drawing && __instance.def != null)
        {
            if (hasCustom)
            {
                Texture2D texture = CoatOfArmsComponent.Instance.GetTextureForFaction(__instance);
                if (texture != null)
                    Patch_FactionTabUI.SaveAndSwap(__instance.def, texture);
                __result = Color.white;
                return false;
            }
            Patch_FactionTabUI.Restore(__instance.def);
            return true;
        }

        if (!hasCustom)
            return true;

        if (__instance.IsPlayer)
        {
            __result = Color.white;
            return false;
        }

        if (Patch_ExpandableWorldObjectsOnGUI.Drawing)
        {
            __result = Color.white;
            return false;
        }

        if (Patch_DrawFactionRow.CurrentFaction == __instance)
        {
            __result = Color.white;
            return false;
        }

        return true;
    }
}
