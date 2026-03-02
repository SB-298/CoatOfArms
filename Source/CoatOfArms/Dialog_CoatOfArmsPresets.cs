using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace CoatOfArms;

public class Dialog_CoatOfArmsPresets : Window
{
    private CoatOfArmsData working;
    private System.Action onDataChanged;
    private string saveName = "";
    private Vector2 presetListScroll;
    private string message;
    private bool messageError;
    private Dictionary<string, Texture2D> presetPreviewCache = new Dictionary<string, Texture2D>();

    public Dialog_CoatOfArmsPresets(CoatOfArmsData data, System.Action onDataChanged = null)
    {
        working = data;
        this.onDataChanged = onDataChanged;
        absorbInputAroundWindow = true;
        closeOnClickedOutside = true;
        doCloseX = true;
    }

    public override void PreClose()
    {
        foreach (Texture2D texture in presetPreviewCache.Values)
        {
            if (texture != null)
                Object.Destroy(texture);
        }
        presetPreviewCache.Clear();
        base.PreClose();
    }

    public override Vector2 InitialSize => new Vector2(420f, 432f);

    public override void DoWindowContents(Rect rect)
    {
        float cursor = rect.y;
        const float rowHeight = 32f;
        const float buttonWidth = 200f;
        const float gap = 8f;

        Text.Font = GameFont.Medium;
        Widgets.Label(new Rect(rect.x, cursor, rect.width, 28f), "CoA_Presets".Translate());
        Text.Font = GameFont.Small;
        cursor += 32f;

        Widgets.Label(new Rect(rect.x, cursor, rect.width, 22f), "CoA_SavedPresets".Translate());
        cursor += 26f;

        List<string> presetNames = CoatOfArmsPresets.GetPresetNames();
        float listHeight = 180f;
        Rect listRect = new Rect(rect.x, cursor, rect.width, listHeight);
        Rect viewRect = new Rect(0f, 0f, rect.width - 20f, presetNames.Count * (rowHeight + 2f));
        Widgets.BeginScrollView(listRect, ref presetListScroll, viewRect);

        const float previewSize = 28f;
        const float previewGap = 6f;

        for (int i = 0; i < presetNames.Count; i++)
        {
            string name = presetNames[i];
            Rect rowRect = new Rect(0f, i * (rowHeight + 2f), viewRect.width - 4f, rowHeight);
            Widgets.DrawHighlightIfMouseover(rowRect);

            Rect previewRect = new Rect(rowRect.x + 2f, rowRect.y + (rowRect.height - previewSize) * 0.5f, previewSize, previewSize);
            Texture2D preview = GetPresetPreview(name);
            if (preview != null)
            {
                Widgets.DrawBoxSolid(previewRect, new Color(0.15f, 0.15f, 0.15f));
                GUI.DrawTexture(previewRect, preview);
            }

            Rect labelRect = new Rect(previewRect.xMax + previewGap, rowRect.y, rowRect.width - (previewRect.xMax + previewGap) - 230f, rowHeight);
            Widgets.Label(labelRect, name);

            Rect loadRect = new Rect(rowRect.xMax - 230f, rowRect.y, 70f, rowHeight - 4f);
            if (Widgets.ButtonText(loadRect, "CoA_Load".Translate()))
            {
                if (CoatOfArmsPresets.LoadPreset(name, out CoatOfArmsData loaded))
                {
                    working.ApplyFrom(loaded);
                    message = "CoA_Loaded".Translate(name);
                    messageError = false;
                    onDataChanged?.Invoke();
                    SoundDefOf.Tick_High.PlayOneShotOnCamera();
                    Close();
                }
                else
                {
                    message = "CoA_LoadFailed".Translate(name);
                    messageError = true;
                }
            }

            Rect overwriteRect = new Rect(rowRect.xMax - 155f, rowRect.y, 70f, rowHeight - 4f);
            if (Widgets.ButtonText(overwriteRect, "CoA_Overwrite".Translate()))
            {
                Dialog_MessageBox confirmOverwrite = Dialog_MessageBox.CreateConfirmation(
                    "CoA_OverwriteConfirmRow".Translate(name),
                    () =>
                    {
                        if (CoatOfArmsPresets.SavePreset(name, working))
                        {
                            message = "CoA_Saved".Translate(name);
                            messageError = false;
                            RemovePresetPreviewFromCache(name);
                            SoundDefOf.Tick_High.PlayOneShotOnCamera();
                        }
                        else
                        {
                            message = "CoA_SaveFailed".Translate();
                            messageError = true;
                        }
                    },
                    true);
                Find.WindowStack.Add(confirmOverwrite);
            }

            Rect deleteRect = new Rect(rowRect.xMax - 80f, rowRect.y, 70f, rowHeight - 4f);
            if (Widgets.ButtonText(deleteRect, "CoA_Delete".Translate()))
            {
                if (CoatOfArmsPresets.DeletePreset(name))
                {
                    RemovePresetPreviewFromCache(name);
                    message = "CoA_Deleted".Translate(name);
                    messageError = false;
                    SoundDefOf.Tick_Low.PlayOneShotOnCamera();
                }
            }
        }

        Widgets.EndScrollView();
        cursor += listHeight + gap + 8f;

        Widgets.Label(new Rect(rect.x, cursor, rect.width, 22f), "CoA_SaveAsPreset".Translate());
        cursor += 24f;

        Rect nameRect = new Rect(rect.x, cursor, rect.width - 90f, 28f);
        saveName = Widgets.TextField(nameRect, saveName);
        cursor += 32f;

        Rect saveRect = new Rect(rect.x, cursor, buttonWidth, rowHeight);
        if (Widgets.ButtonText(saveRect, "CoA_SavePreset".Translate()))
        {
            if (string.IsNullOrWhiteSpace(saveName))
            {
                message = "CoA_EnterName".Translate();
                messageError = true;
            }
            else
            {
                string trimmedName = saveName.Trim();
                bool nameExists = presetNames.Contains(trimmedName);
                if (nameExists)
                {
                    Dialog_MessageBox confirmOverwrite = Dialog_MessageBox.CreateConfirmation(
                        "CoA_OverwriteConfirm".Translate(trimmedName),
                        () =>
                        {
                            if (CoatOfArmsPresets.SavePreset(trimmedName, working))
                            {
                                message = "CoA_Saved".Translate(trimmedName);
                                messageError = false;
                                RemovePresetPreviewFromCache(trimmedName);
                                SoundDefOf.Tick_High.PlayOneShotOnCamera();
                            }
                            else
                            {
                                message = "CoA_SaveFailed".Translate();
                                messageError = true;
                            }
                        },
                        true);
                    Find.WindowStack.Add(confirmOverwrite);
                }
                else if (CoatOfArmsPresets.SavePreset(trimmedName, working))
                {
                    message = "CoA_Saved".Translate(trimmedName);
                    messageError = false;
                    SoundDefOf.Tick_High.PlayOneShotOnCamera();
                }
                else
                {
                    message = "CoA_SaveFailed".Translate();
                    messageError = true;
                }
            }
        }
        cursor += rowHeight + gap;

        if (!string.IsNullOrEmpty(message))
        {
            GUI.color = messageError ? Color.red : Color.green;
            Widgets.Label(new Rect(rect.x, cursor, rect.width, 40f), message);
            GUI.color = Color.white;
        }
    }

    private Texture2D GetPresetPreview(string presetName)
    {
        if (presetPreviewCache.TryGetValue(presetName, out Texture2D cached) && cached != null)
            return cached;

        if (!CoatOfArmsPresets.LoadPreset(presetName, out CoatOfArmsData data))
            return null;

        Texture2D texture = CoatOfArmsRenderer.Render(data, 32);
        if (texture != null)
            presetPreviewCache[presetName] = texture;
        return texture;
    }

    private void RemovePresetPreviewFromCache(string presetName)
    {
        if (presetPreviewCache.TryGetValue(presetName, out Texture2D texture) && texture != null)
        {
            Object.Destroy(texture);
            presetPreviewCache.Remove(presetName);
        }
    }
}
