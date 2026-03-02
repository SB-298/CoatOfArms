using RimWorld;
using UnityEngine;
using Verse;

namespace CoatOfArms;

public class CoatOfArmsMod : Mod
{
    public static CoatOfArmsSettings Settings;

    /// <summary>Edit buffer for export path so the text field is not reset each frame when showing the default path.</summary>
    private static string exportPathEditBuffer = null;

    public CoatOfArmsMod(ModContentPack content) : base(content)
    {
        Settings = GetSettings<CoatOfArmsSettings>();
    }

    public override string SettingsCategory()
    {
        return "Coat of Arms";
    }

    public override void DoSettingsWindowContents(Rect rect)
    {
        Listing_Standard listing = new Listing_Standard();
        listing.Begin(rect);

        listing.Label("CoA_SettingsEmblemLimit".Translate(CoatOfArmsSettings.EmblemLimit));
        CoatOfArmsSettings.EmblemLimit = (int)listing.Slider(CoatOfArmsSettings.EmblemLimit, 1, 100);

        listing.GapLine();
        listing.Label("CoA_SettingsResolutionLabel".Translate());
        int[] resolutions = CoatOfArmsSettings.PredefinedResolutions;
        for (int i = 0; i < resolutions.Length; i++)
        {
            int resolution = resolutions[i];
            if (listing.RadioButton("CoA_ResolutionPx".Translate(resolution), CoatOfArmsSettings.Resolution == resolution))
            {
                CoatOfArmsSettings.Resolution = resolution;
                CoatOfArmsComponent.Instance?.MarkDirty();
                CoatOfArmsComponent.Instance?.Apply();
            }
        }
        GUI.color = Color.grey;
        listing.Label("CoA_ResolutionTooltip".Translate());
        GUI.color = Color.white;

        listing.GapLine();
        bool canOpenEditor = Current.Game != null && CoatOfArmsComponent.Instance != null;
        GUI.enabled = canOpenEditor;
        if (listing.ButtonText("CoA_EditCoatOfArms".Translate()))
        {
            Find.WindowStack.Add(new Dialog_CoatOfArmsEditor(Faction.OfPlayer));
        }
        GUI.enabled = true;
        if (!canOpenEditor)
        {
            listing.Gap(2f);
            GUI.color = Color.grey;
            listing.Label("CoA_OpenEditorInGameOnly".Translate());
            GUI.color = Color.white;
        }

        listing.GapLine();
        listing.CheckboxLabeled("CoA_SettingsAllowEditOtherFactions".Translate(), ref CoatOfArmsSettings.AllowEditOtherFactions, "CoA_SettingsAllowEditOtherFactionsTip".Translate());

        listing.GapLine();
        listing.Label("CoA_SettingsExportFolderLabel".Translate());
        listing.Label("CoA_SettingsExportFolderPrompt".Translate());
        string defaultPath = CoatOfArmsPresets.GetExportsDirectory() ?? "";
        string effectivePath = CoatOfArmsSettings.ExportFolderOverride ?? defaultPath;
        if (exportPathEditBuffer == null || exportPathEditBuffer == effectivePath)
            exportPathEditBuffer = effectivePath;
        Rect fieldRect = listing.GetRect(24f);
        string editedPath = Widgets.TextField(fieldRect, exportPathEditBuffer);
        exportPathEditBuffer = editedPath;
        if (string.IsNullOrWhiteSpace(editedPath) || editedPath.Trim() == defaultPath)
            CoatOfArmsSettings.ExportFolderOverride = null;
        else
            CoatOfArmsSettings.ExportFolderOverride = editedPath.Trim();
        if (listing.ButtonText("CoA_OpenExportFolder".Translate()))
        {
            CoatOfArmsPresets.OpenExportsFolder();
        }

        listing.End();
    }
}
