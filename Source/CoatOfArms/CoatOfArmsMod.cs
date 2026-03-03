using RimWorld;
using UnityEngine;
using Verse;

namespace CoatOfArms;

public class CoatOfArmsMod : Mod
{
    public static CoatOfArmsSettings Settings;

    /// <summary>Edit buffer for export path so the text field is not reset each frame when showing the default path.</summary>
    private static string exportPathEditBuffer = null;

    private static string vanillaScaleBuffer = null;
    private static string coatScaleBuffer = null;

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

        bool canOpenEditor = Current.Game != null && CoatOfArmsComponent.Instance != null;
        GUI.enabled = canOpenEditor;
        if (listing.ButtonText("CoA_EditCoatOfArms".Translate()))
        {
            Find.WindowStack.Add(new Dialog_CoatOfArmsEditor(Faction.OfPlayer));
        }
        GUI.enabled = true;
        if (!canOpenEditor)
        {
            GUI.color = Color.grey;
            listing.Label("CoA_OpenEditorInGameOnly".Translate());
            GUI.color = Color.white;
        }

        listing.Gap(4f);
        listing.GapLine();
        listing.Gap(4f);
        listing.CheckboxLabeled("CoA_SettingsAllowEditOtherFactions".Translate(), ref CoatOfArmsSettings.AllowEditOtherFactions, "CoA_SettingsAllowEditOtherFactionsTip".Translate());

        listing.Gap(8f);
        listing.Label("CoA_SettingsEmblemLimit".Translate(CoatOfArmsSettings.EmblemLimit));
        CoatOfArmsSettings.EmblemLimit = (int)listing.Slider(CoatOfArmsSettings.EmblemLimit, 1, 100);

        listing.Gap(8f);
        listing.Label("CoA_SettingsResolutionLabel".Translate());
        int[] resolutions = CoatOfArmsSettings.PredefinedResolutions;
        float radioSize = 24f;
        float itemSpacing = 16f;
        Rect resolutionRow = listing.GetRect(30f);
        float cursorX = resolutionRow.x;
        for (int i = 0; i < resolutions.Length; i++)
        {
            int resolution = resolutions[i];
            bool selected = CoatOfArmsSettings.Resolution == resolution;
            if (Widgets.RadioButton(new Vector2(cursorX, resolutionRow.y + 3f), selected))
            {
                CoatOfArmsSettings.Resolution = resolution;
                CoatOfArmsComponent.Instance?.MarkDirty();
                CoatOfArmsComponent.Instance?.Apply();
            }
            string label = "CoA_ResolutionPx".Translate(resolution);
            Vector2 labelSize = Text.CalcSize(label);
            float labelY = resolutionRow.y + (resolutionRow.height - labelSize.y) / 2f;
            Rect labelRect = new Rect(cursorX + radioSize + 4f, labelY, labelSize.x, labelSize.y);
            Widgets.Label(labelRect, label);
            cursorX += radioSize + 4f + labelSize.x + itemSpacing;
        }
        GUI.color = Color.grey;
        listing.Label("CoA_ResolutionTooltip".Translate());
        GUI.color = Color.white;

        listing.Gap(12f);
        listing.Label("CoA_SettingsVanillaIconScale".Translate(CoatOfArmsSettings.VanillaIconScale.ToString("F2")));
        CoatOfArmsSettings.VanillaIconScale = ScaleSliderWithInput(listing, CoatOfArmsSettings.VanillaIconScale, 0.5f, 3f, ref vanillaScaleBuffer);
        listing.Gap(6f);
        listing.Label("CoA_SettingsCoatIconScale".Translate(CoatOfArmsSettings.CoatIconScale.ToString("F2")));
        CoatOfArmsSettings.CoatIconScale = ScaleSliderWithInput(listing, CoatOfArmsSettings.CoatIconScale, 0.5f, 3f, ref coatScaleBuffer);

        listing.Gap(4f);
        listing.GapLine();
        listing.Gap(4f);
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
        listing.Gap(6f);
        if (listing.ButtonText("CoA_OpenExportFolder".Translate()))
        {
            CoatOfArmsPresets.OpenExportsFolder();
        }

        listing.End();
    }

    private static float ScaleSliderWithInput(Listing_Standard listing, float value, float min, float max, ref string buffer)
    {
        Rect row = listing.GetRect(28f);
        Rect sliderRect = new Rect(row.x, row.y, row.width - 70f, row.height);
        Rect fieldRect = new Rect(sliderRect.xMax + 6f, row.y, 64f, row.height);

        float sliderValue = Widgets.HorizontalSlider(sliderRect, value, min, max);

        if (buffer == null || sliderValue != value)
            buffer = sliderValue.ToString("F2");

        string typed = Widgets.TextField(fieldRect, buffer);
        if (typed != buffer)
        {
            buffer = typed;
            if (float.TryParse(typed, out float parsed))
                sliderValue = Mathf.Clamp(parsed, min, max);
        }

        return sliderValue;
    }
}
