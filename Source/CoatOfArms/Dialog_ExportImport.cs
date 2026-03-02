using System;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace CoatOfArms;

public class Dialog_ExportImport : Window
{
    private CoatOfArmsData working;
    private Action onDataChanged;

    public Dialog_ExportImport(CoatOfArmsData data, Action onDataChanged = null)
    {
        working = data;
        this.onDataChanged = onDataChanged;
        absorbInputAroundWindow = true;
        closeOnClickedOutside = true;
        doCloseX = true;
    }

    public override Vector2 InitialSize => new Vector2(320f, 200f);

    public override void DoWindowContents(Rect rect)
    {
        float cursor = rect.y;
        const float rowHeight = 36f;
        const float buttonWidth = 260f;
        const float gap = 10f;

        Text.Font = GameFont.Medium;
        Widgets.Label(new Rect(rect.x, cursor, rect.width, 28f), "CoA_ExportImportTitle".Translate());
        Text.Font = GameFont.Small;
        cursor += 34f;

        Rect copyRect = new Rect(rect.x, cursor, buttonWidth, rowHeight);
        if (Widgets.ButtonText(copyRect, "CoA_ExportClipboard".Translate()))
        {
            string serialized = CoatOfArmsSerializer.ToString(working);
            if (!string.IsNullOrEmpty(serialized))
            {
                GUIUtility.systemCopyBuffer = serialized;
                Messages.Message("CoA_Exported".Translate(), MessageTypeDefOf.PositiveEvent);
                SoundDefOf.Tick_High.PlayOneShotOnCamera();
            }
        }
        cursor += rowHeight + gap;

        Rect importRect = new Rect(rect.x, cursor, buttonWidth, rowHeight);
        if (Widgets.ButtonText(importRect, "CoA_ImportClipboard".Translate()))
        {
            string raw = GUIUtility.systemCopyBuffer;
            if (CoatOfArmsSerializer.TryParse(raw, out CoatOfArmsData imported))
            {
                working.ApplyFrom(imported);
                Messages.Message("CoA_Imported".Translate(), MessageTypeDefOf.PositiveEvent);
                onDataChanged?.Invoke();
                SoundDefOf.Tick_High.PlayOneShotOnCamera();
            }
            else
            {
                Messages.Message("CoA_ImportFailed".Translate(), MessageTypeDefOf.RejectInput);
            }
        }
        cursor += rowHeight + gap;

        Rect pngRect = new Rect(rect.x, cursor, buttonWidth, rowHeight);
        if (Widgets.ButtonText(pngRect, "CoA_ExportPng".Translate()))
        {
            string savedPath = CoatOfArmsPresets.ExportAsPng(working);
            if (!string.IsNullOrEmpty(savedPath))
            {
                Messages.Message("CoA_ExportedPng".Translate(savedPath), MessageTypeDefOf.PositiveEvent);
                SoundDefOf.Tick_High.PlayOneShotOnCamera();
            }
            else
            {
                Messages.Message("CoA_ExportPngFailed".Translate(), MessageTypeDefOf.NegativeEvent);
            }
        }
    }
}
