using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace CoatOfArms;

[HarmonyPatch(typeof(ExpandableWorldObjectsUtility), nameof(ExpandableWorldObjectsUtility.ExpandableWorldObjectsOnGUI))]
public static class Patch_ExpandableWorldObjectsOnGUI
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
