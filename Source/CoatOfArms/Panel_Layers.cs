using System;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace CoatOfArms;

public static class Panel_Layers
{
    private static Vector2 scrollPosition;

    public static bool Draw(Rect rect, CoatOfArmsData data, ref int selected, Action onLayerSelected = null, Action onLayerAdded = null)
    {
        bool changed = false;
        float cursor = rect.y;

        const float addButtonSize = 24f;
        float labelWidth = rect.width - addButtonSize - 4f;

        Text.Font = GameFont.Small;
        Widgets.Label(new Rect(rect.x, cursor, labelWidth, 24f), "CoA_EmblemLayers".Translate());

        Rect addButtonRect = new Rect(rect.x + labelWidth + 4f, cursor, addButtonSize, addButtonSize);
        if (DrawAddButton(addButtonRect, data, ref selected))
        {
            changed = true;
            onLayerAdded?.Invoke();
        }

        cursor += 28f;

        if (data.emblems.Count == 0)
        {
            Text.Font = GameFont.Tiny;
            GUI.color = Color.gray;
            Rect noEmblemRect = new Rect(rect.x, cursor, rect.width, 36f);
            bool wrap = Text.WordWrap;
            Text.WordWrap = true;
            Widgets.Label(noEmblemRect, "CoA_NoEmblems".Translate());
            Text.WordWrap = wrap;
            GUI.color = Color.white;
            Text.Font = GameFont.Small;
            return false;
        }

        const float rowHeight = 36f;
        const float buttonWidth = 24f;
        const int layerButtonCount = 4;
        float controlAreaWidth = (buttonWidth + 2f) * layerButtonCount;

        float listHeight = rect.yMax - cursor;
        Rect viewRect = new Rect(0f, 0f, rect.width - 16f, data.emblems.Count * rowHeight);
        Rect scrollRect = new Rect(rect.x, cursor, rect.width, listHeight);

        Widgets.BeginScrollView(scrollRect, ref scrollPosition, viewRect);

        for (int i = data.emblems.Count - 1; i >= 0; i--)
        {
            int displayIndex = data.emblems.Count - 1 - i;
            EmblemLayer layer = data.emblems[i];
            Rect row = new Rect(0f, displayIndex * rowHeight, viewRect.width, rowHeight);

            if (i == selected)
            {
                Widgets.DrawBoxSolid(row, new Color(0.25f, 0.25f, 0.35f));
            }
            else if (displayIndex % 2 == 0)
            {
                Widgets.DrawBoxSolid(row, new Color(0.18f, 0.18f, 0.18f));
            }

            Widgets.DrawHighlightIfMouseover(row);

            Rect thumbnail = new Rect(row.x + 4f, row.y + 2f, 32f, 32f);
            if (layer.emblem?.Texture != null)
            {
                GUI.color = layer.color;
                GUI.DrawTexture(thumbnail, layer.emblem.Texture);
                GUI.color = Color.white;
            }

            float controlX = row.xMax - controlAreaWidth;
            Rect labelArea = new Rect(thumbnail.xMax + 6f, row.y, controlX - (thumbnail.xMax + 6f), rowHeight);
            Text.Anchor = TextAnchor.MiddleLeft;
            Text.Font = GameFont.Tiny;
            Widgets.Label(labelArea, layer.emblem?.label ?? "?");
            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.UpperLeft;

            Rect upButton = new Rect(controlX, row.y + 6f, buttonWidth, buttonWidth);
            if (i < data.emblems.Count - 1)
            {
                if (Widgets.ButtonText(upButton, "\u25B2"))
                {
                    (data.emblems[i], data.emblems[i + 1]) = (data.emblems[i + 1], data.emblems[i]);
                    if (selected == i) selected = i + 1;
                    else if (selected == i + 1) selected = i;
                    SoundDefOf.Tick_Tiny.PlayOneShotOnCamera();
                    changed = true;
                }
            }

            Rect downButton = new Rect(controlX + buttonWidth + 2f, row.y + 6f, buttonWidth, buttonWidth);
            if (i > 0)
            {
                if (Widgets.ButtonText(downButton, "\u25BC"))
                {
                    (data.emblems[i], data.emblems[i - 1]) = (data.emblems[i - 1], data.emblems[i]);
                    if (selected == i) selected = i - 1;
                    else if (selected == i - 1) selected = i;
                    SoundDefOf.Tick_Tiny.PlayOneShotOnCamera();
                    changed = true;
                }
            }

            Rect duplicateButton = new Rect(controlX + (buttonWidth + 2f) * 2f, row.y + 6f, buttonWidth, buttonWidth);
            Widgets.DrawBoxSolid(duplicateButton, new Color(0.22f, 0.22f, 0.22f));
            Widgets.DrawHighlightIfMouseover(duplicateButton);
            Widgets.DrawBox(duplicateButton, 1);
            Rect backRect = new Rect(duplicateButton.x + 4f, duplicateButton.y + 10f, 12f, 10f);
            Rect frontRect = new Rect(duplicateButton.x + 8f, duplicateButton.y + 4f, 12f, 10f);
            GUI.color = Color.white;
            Widgets.DrawBoxSolid(backRect, Color.white);
            Widgets.DrawBoxSolid(frontRect, Color.white);
            if (Widgets.ButtonInvisible(duplicateButton))
            {
                if (data.emblems.Count >= CoatOfArmsSettings.EmblemLimit)
                {
                    Messages.Message("CoA_EmblemLimitReached".Translate(CoatOfArmsSettings.EmblemLimit),
                        MessageTypeDefOf.RejectInput, false);
                }
                else
                {
                    EmblemLayer duplicate = layer.Clone();
                    data.emblems.Insert(i + 1, duplicate);
                    selected = i + 1;
                    SoundDefOf.Tick_High.PlayOneShotOnCamera();
                    changed = true;
                }
            }
            if (Mouse.IsOver(duplicateButton))
                TooltipHandler.TipRegion(duplicateButton, "CoA_DuplicateEmblem".Translate());

            Rect deleteButton = new Rect(controlX + (buttonWidth + 2f) * 3f, row.y + 6f, buttonWidth, buttonWidth);
            if (Widgets.ButtonText(deleteButton, "X"))
            {
                data.emblems.RemoveAt(i);
                if (selected == i) selected = -1;
                else if (selected > i) selected--;
                SoundDefOf.Tick_Low.PlayOneShotOnCamera();
                changed = true;
            }

            Rect selectArea = new Rect(row.x, row.y, controlX - row.x, rowHeight);
            if (Widgets.ButtonInvisible(selectArea))
            {
                selected = i;
                onLayerSelected?.Invoke();
                SoundDefOf.Tick_Tiny.PlayOneShotOnCamera();
            }
        }

        Widgets.EndScrollView();
        GUI.color = Color.white;
        Text.Anchor = TextAnchor.UpperLeft;
        Text.Font = GameFont.Small;
        return changed;
    }

    private static bool DrawAddButton(Rect rect, CoatOfArmsData data, ref int selected)
    {
        Widgets.DrawBoxSolid(rect, new Color(0.22f, 0.22f, 0.22f));
        Widgets.DrawHighlightIfMouseover(rect);
        if (data.emblems.Count < CoatOfArmsSettings.EmblemLimit)
        {
            Widgets.DrawBox(rect, 1);
        }

        Text.Anchor = TextAnchor.MiddleCenter;
        Text.Font = GameFont.Medium;
        GUI.color = Color.white;
        Widgets.Label(rect, "+");
        Text.Font = GameFont.Small;
        Text.Anchor = TextAnchor.UpperLeft;

        if (Widgets.ButtonInvisible(rect))
        {
            if (data.emblems.Count >= CoatOfArmsSettings.EmblemLimit)
            {
                Messages.Message("CoA_EmblemLimitReached".Translate(CoatOfArmsSettings.EmblemLimit),
                    MessageTypeDefOf.RejectInput, false);
                return false;
            }

            EmblemDef defaultEmblem = DefDatabase<EmblemDef>.GetNamedSilentFail("Circle");
            if (defaultEmblem == null || !defaultEmblem.IsAvailableAndLoaded())
            {
                defaultEmblem = DefDatabase<EmblemDef>.AllDefs
                    .Where(d => d.IsAvailableAndLoaded())
                    .OrderBy(d => d.order)
                    .FirstOrDefault();
            }
            if (defaultEmblem != null)
            {
                EmblemLayer layer = new EmblemLayer();
                layer.emblem = defaultEmblem;
                data.emblems.Add(layer);
                selected = data.emblems.Count - 1;
                SoundDefOf.Tick_High.PlayOneShotOnCamera();
                return true;
            }
        }

        return false;
    }
}
