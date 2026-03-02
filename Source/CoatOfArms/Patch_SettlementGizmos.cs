using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace CoatOfArms;

[HarmonyPatch(typeof(Settlement), nameof(Settlement.GetGizmos))]
public static class Patch_SettlementGizmos
{
    public static IEnumerable<Gizmo> Postfix(IEnumerable<Gizmo> __result, Settlement __instance)
    {
        foreach (Gizmo gizmo in __result)
        {
            yield return gizmo;
        }

        bool canEdit = __instance.Faction != null
            && (__instance.Faction.IsPlayer || DebugSettings.godMode || CoatOfArmsSettings.AllowEditOtherFactions);

        if (canEdit)
        {
            Faction faction = __instance.Faction;
            yield return new Command_Action
            {
                defaultLabel = "CoA_EditCoatOfArms".Translate(),
                defaultDesc = "CoA_EditCoatOfArmsDesc".Translate(),
                icon = ContentFinder<Texture2D>.Get("CoatOfArms/UI/OpenEditor", false) ?? BaseContent.BadTex,
                action = delegate { Find.WindowStack.Add(new Dialog_CoatOfArmsEditor(faction)); }
            };
        }
    }
}
