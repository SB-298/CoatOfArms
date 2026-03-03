using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace CoatOfArms;

[HarmonyPatch(typeof(FactionUIUtility), nameof(FactionUIUtility.DoWindowContents))]
public static class Patch_FactionTabUI
{
    public static bool Drawing;
    private static Dictionary<FactionDef, Texture2D> savedIcons = new Dictionary<FactionDef, Texture2D>();

    public static void Prefix()
    {
        Drawing = true;
        savedIcons.Clear();
    }

    public static void Postfix()
    {
        foreach (KeyValuePair<FactionDef, Texture2D> pair in savedIcons)
        {
            if (pair.Key != null)
                pair.Key.factionIcon = pair.Value;
        }
        savedIcons.Clear();
        Drawing = false;
    }

    public static void SaveAndSwap(FactionDef definition, Texture2D icon)
    {
        if (definition == null || icon == null)
            return;
        if (!savedIcons.ContainsKey(definition))
            savedIcons[definition] = definition.factionIcon;
        definition.factionIcon = icon;
    }

    public static void Restore(FactionDef definition)
    {
        if (definition == null)
            return;
        if (savedIcons.TryGetValue(definition, out Texture2D original))
            definition.factionIcon = original;
    }
}

[HarmonyPatch(typeof(FactionUIUtility), "DrawFactionRow")]
public static class Patch_DrawFactionRow
{
    public static Faction CurrentFaction;

    public static void Prefix(Faction faction)
    {
        CurrentFaction = faction;

        if (!Patch_FactionTabUI.Drawing)
            return;
        if (CoatOfArmsComponent.Instance == null || faction?.def == null)
            return;

        if (CoatOfArmsComponent.Instance.HasCustomCoatOfArms(faction))
        {
            Texture2D texture = CoatOfArmsComponent.Instance.GetTextureForFaction(faction);
            if (texture != null)
            {
                Patch_FactionTabUI.SaveAndSwap(faction.def, texture);
                return;
            }
        }

        Patch_FactionTabUI.Restore(faction.def);
    }

    public static void Postfix()
    {
        CurrentFaction = null;
    }
}
