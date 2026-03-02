using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace CoatOfArms;

public static class Panel_Emblems
{
    private static string activeCategory = "All";
    private static Vector2 gridScroll;
    private static Dictionary<string, string> sliderBuffers = new Dictionary<string, string>();
    private static bool scaleLinked = true;

    private static List<string> cachedCategories;
    private static string cachedCategoryForEmblems = null;
    private static List<EmblemDef> cachedEmblemList = new List<EmblemDef>();

    private static Texture2D linkedScaleIcon;
    private static Texture2D unlinkedScaleIcon;

    public static bool Draw(Rect rect, CoatOfArmsData data, ref int selected)
    {
        bool changed = false;
        float cursor = rect.y;

        bool hasSelection = selected >= 0 && selected < data.emblems.Count;
        float propsHeight = hasSelection ? 398f : 0f;

        Rect catRect = new Rect(rect.x, cursor, rect.width, 28f);
        DrawCategoryBar(catRect);
        cursor += 32f;

        float gridHeight = rect.height - (cursor - rect.y) - propsHeight - 8f;
        if (gridHeight < 80f)
            gridHeight = 80f;

        Rect gridRect = new Rect(rect.x, cursor, rect.width, gridHeight);
        changed |= DrawEmblemGrid(gridRect, data, ref selected);
        cursor += gridHeight + 4f;

        if (hasSelection)
        {
            Rect propsRect = new Rect(rect.x, cursor, rect.width, propsHeight);
            changed |= DrawProperties(propsRect, data.emblems[selected]);
        }

        return changed;
    }

    private static void DrawCategoryBar(Rect rect)
    {
        if (cachedCategories == null)
        {
            cachedCategories = new List<string> { "All" };
            foreach (EmblemDef def in DefDatabase<EmblemDef>.AllDefs)
            {
                if (def.category.NullOrEmpty() || cachedCategories.Contains(def.category))
                    continue;
                if (!def.IsAvailable())
                    continue;
                cachedCategories.Add(def.category);
            }
            if (!ModsConfig.IdeologyActive)
            {
                cachedCategories.RemoveAll(c => c == "Roles" || c == "Precepts" || c == "Structures" || c == "Ideologies");
            }
            SortCategoryTabs();
            if (!cachedCategories.Contains(activeCategory))
            {
                activeCategory = "All";
                cachedCategoryForEmblems = null;
            }
        }

        Text.Font = GameFont.Tiny;
        float minButtonWidth = 0f;
        foreach (string category in cachedCategories)
        {
            float needed = Text.CalcSize(category).x + 10f;
            if (needed > minButtonWidth)
                minButtonWidth = needed;
        }
        Text.Font = GameFont.Small;
        float buttonWidth = Mathf.Max(minButtonWidth, Mathf.Min(100f, rect.width / cachedCategories.Count));
        float x = rect.x;

        foreach (string category in cachedCategories)
        {
            Rect button = new Rect(x, rect.y, buttonWidth, rect.height);
            bool active = category == activeCategory;

            if (active)
            {
                Widgets.DrawBoxSolid(button, new Color(0.3f, 0.3f, 0.3f));
            }
            else
            {
                Widgets.DrawHighlightIfMouseover(button);
            }

            Text.Anchor = TextAnchor.MiddleCenter;
            Text.Font = GameFont.Tiny;
            Widgets.Label(button, category);
            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.UpperLeft;

            if (!active && Widgets.ButtonInvisible(button))
            {
                activeCategory = category;
                SoundDefOf.Tick_Tiny.PlayOneShotOnCamera();
            }

            x += buttonWidth + 2f;
        }
    }

    private static readonly string[] CategoryTabOrder = { "Shapes", "Symbols", "Lettering", "Roles", "Precepts", "Structures", "Ideologies" };

    private static void SortCategoryTabs()
    {
        cachedCategories.Sort((a, b) =>
        {
            if (a == "All") return -1;
            if (b == "All") return 1;
            int indexA = System.Array.IndexOf(CategoryTabOrder, a);
            int indexB = System.Array.IndexOf(CategoryTabOrder, b);
            if (indexA >= 0 && indexB >= 0) return indexA.CompareTo(indexB);
            if (indexA >= 0) return -1;
            if (indexB >= 0) return 1;
            return string.CompareOrdinal(a, b);
        });
    }

    private static int CategorySortOrder(string category)
    {
        if (category.NullOrEmpty()) return CategoryTabOrder.Length;
        int index = System.Array.IndexOf(CategoryTabOrder, category);
        return index >= 0 ? index : CategoryTabOrder.Length;
    }

    private static bool DrawEmblemGrid(Rect rect, CoatOfArmsData data, ref int selected)
    {
        bool changed = false;
        bool hasSelection = selected >= 0 && selected < data.emblems.Count;
        const int tileSize = 48;
        const int spacing = 4;

        if (cachedCategoryForEmblems != activeCategory)
        {
            cachedCategoryForEmblems = activeCategory;
            cachedEmblemList.Clear();
            bool isIdeologyOnlyCategory = activeCategory == "Roles" || activeCategory == "Precepts" || activeCategory == "Structures" || activeCategory == "Ideologies";
            if (ModsConfig.IdeologyActive || !isIdeologyOnlyCategory)
            {
                foreach (EmblemDef def in DefDatabase<EmblemDef>.AllDefs)
                {
                    if (!def.IsAvailable())
                        continue;
                    if (activeCategory != "All" && def.category != activeCategory)
                        continue;
                    cachedEmblemList.Add(def);
                }
            }
            cachedEmblemList.Sort((a, b) =>
            {
                int catOrder = CategorySortOrder(a.category).CompareTo(CategorySortOrder(b.category));
                return catOrder != 0 ? catOrder : a.order.CompareTo(b.order);
            });
        }

        List<EmblemDef> emblems = cachedEmblemList;
        int columns = Mathf.FloorToInt((rect.width - 16f) / (tileSize + spacing));
        if (columns < 1)
            columns = 1;

        int totalRows = Mathf.CeilToInt((float)emblems.Count / columns);
        float viewHeight = totalRows * (tileSize + spacing);
        Rect viewRect = new Rect(0f, 0f, rect.width - 16f, viewHeight);

        Widgets.BeginScrollView(rect, ref gridScroll, viewRect);

        EmblemDef.BeginTextureLoadFrame();
        const int maxTextureLoadsPerFrame = 10;

        for (int row = 0; row < totalRows; row++)
        {
            for (int col = 0; col < columns; col++)
            {
                int i = row * columns + col;
                if (i >= emblems.Count)
                    break;

                EmblemDef emblem = emblems[i];
                emblem.TryLoadTexture(maxTextureLoadsPerFrame);

                Rect tile = new Rect(
                    col * (tileSize + spacing),
                    row * (tileSize + spacing),
                    tileSize,
                    tileSize
                );

                Widgets.DrawBoxSolid(tile, new Color(0.2f, 0.2f, 0.2f));
                Widgets.DrawHighlightIfMouseover(tile);

                if (emblem.IsTextureLoaded)
                {
                    Rect inner = tile.ContractedBy(4f);
                    GUI.DrawTexture(inner, emblem.Texture);
                }

                if (Widgets.ButtonInvisible(tile))
                {
                    if (hasSelection)
                    {
                        data.emblems[selected].emblem = emblem;
                        SoundDefOf.Tick_High.PlayOneShotOnCamera();
                        changed = true;
                    }
                    else
                    {
                        if (data.emblems.Count >= CoatOfArmsSettings.EmblemLimit)
                        {
                            Messages.Message("CoA_EmblemLimitReached".Translate(CoatOfArmsSettings.EmblemLimit),
                                MessageTypeDefOf.RejectInput, false);
                        }
                        else
                        {
                            EmblemLayer layer = new EmblemLayer();
                            layer.emblem = emblem;
                            data.emblems.Add(layer);
                            selected = data.emblems.Count - 1;
                            SoundDefOf.Tick_High.PlayOneShotOnCamera();
                            changed = true;
                        }
                    }
                }
            }
        }

        Widgets.EndScrollView();
        GUI.color = Color.white;
        Text.Anchor = TextAnchor.UpperLeft;
        Text.Font = GameFont.Small;
        return changed;
    }

    private static bool DrawProperties(Rect rect, EmblemLayer layer)
    {
        bool changed = false;
        Widgets.DrawBoxSolid(rect, new Color(0.14f, 0.14f, 0.14f));
        rect = rect.ContractedBy(6f);
        float cursor = rect.y;

        Text.Font = GameFont.Small;
        Widgets.Label(new Rect(rect.x, cursor, rect.width, 22f),
            "CoA_EmblemProperties".Translate(layer.emblem?.label ?? "?"));
        cursor += 26f;

        changed |= DrawSlider(rect.x, ref cursor, rect.width, "px", "CoA_PositionX".Translate(),
            ref layer.position.x, -0.5f, 0.5f);
        changed |= DrawSlider(rect.x, ref cursor, rect.width, "py", "CoA_PositionY".Translate(),
            ref layer.position.y, -0.5f, 0.5f);
        changed |= DrawSlider(rect.x, ref cursor, rect.width, "rot", "CoA_Rotation".Translate(),
            ref layer.rotation, 0f, 360f);
        changed |= DrawScaleXYWithLink(rect.x, ref cursor, rect.width, layer);
        Rect flipRow = new Rect(rect.x, cursor, rect.width, 24f);
        bool oldFlipX = layer.flipX;
        bool oldFlipY = layer.flipY;

        float flipXLabelWidth = Text.CalcSize("CoA_FlipX".Translate()).x + 24f + 4f;
        float flipYLabelWidth = Text.CalcSize("CoA_FlipY".Translate()).x + 24f + 4f;
        Widgets.CheckboxLabeled(new Rect(flipRow.x, flipRow.y, flipXLabelWidth, flipRow.height),
            "CoA_FlipX".Translate(), ref layer.flipX);
        Widgets.CheckboxLabeled(
            new Rect(flipRow.x + flipXLabelWidth + 8f, flipRow.y, flipYLabelWidth, flipRow.height),
            "CoA_FlipY".Translate(), ref layer.flipY);

        if (oldFlipX != layer.flipX || oldFlipY != layer.flipY)
            changed = true;

        cursor += 28f;

        changed |= DrawSlider(rect.x, ref cursor, rect.width, "opacity", "CoA_Opacity".Translate(),
            ref layer.color.a, 0f, 1f);

        Rect colorLabel = new Rect(rect.x, cursor, 60f, 24f);
        Widgets.Label(colorLabel, "CoA_Color".Translate());
        cursor += 26f;
        Rect colorPickerRect = new Rect(rect.x, cursor, rect.width, 138f);
        changed |= ColorPickerUI.DrawColorPicker(colorPickerRect, ref layer.color, "emblem");

        return changed;
    }

    private static bool DrawSlider(float x, ref float cursor, float width, string bufferKey, string label,
        ref float value, float min, float max)
    {
        const float labelWidth = 70f;
        const float inputWidth = 52f;
        float sliderWidth = width - labelWidth - inputWidth - 8f;
        if (sliderWidth < 40f)
            sliderWidth = 40f;

        float original = value;

        if (!sliderBuffers.ContainsKey(bufferKey))
            sliderBuffers[bufferKey] = value.ToString("G3");
        string buffer = sliderBuffers[bufferKey];
        if (float.TryParse(buffer.Trim(), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float parsed)
            && Mathf.Abs(parsed - value) > 0.0001f)
            sliderBuffers[bufferKey] = value.ToString("G3");

        Rect labelRect = new Rect(x, cursor, labelWidth, 22f);
        Text.Font = GameFont.Tiny;
        Widgets.Label(labelRect, label);
        Text.Font = GameFont.Small;

        Rect sliderRect = new Rect(x + labelWidth, cursor, sliderWidth, 22f);
        value = Widgets.HorizontalSlider(sliderRect, value, min, max, false, null, null, null, -1f);

        if (Mathf.Abs(value - original) > 0.0001f)
            sliderBuffers[bufferKey] = value.ToString("G3");

        Rect inputRect = new Rect(x + labelWidth + sliderWidth + 4f, cursor, inputWidth, 22f);
        buffer = sliderBuffers[bufferKey];
        string newText = Widgets.TextField(inputRect, buffer);
        if (newText != buffer)
        {
            sliderBuffers[bufferKey] = newText;
            if (float.TryParse(newText.Trim(), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float parsedInput))
            {
                value = Mathf.Clamp(parsedInput, min, max);
                sliderBuffers[bufferKey] = value.ToString("G3");
            }
        }

        cursor += 26f;
        return Mathf.Abs(value - original) > 0.001f;
    }

    private static bool DrawScaleXYWithLink(float x, ref float cursor, float width, EmblemLayer layer)
    {
        const float labelWidth = 58f;
        const float linkButtonSize = 28f;
        const float inputWidth = 52f;
        const float gap = 4f;
        const float labelToButtonGap = 0f;
        const float rowHeight = 26f;

        float sliderWidth = width - labelWidth - linkButtonSize - labelToButtonGap - gap - inputWidth - 8f;
        if (sliderWidth < 40f)
            sliderWidth = 40f;

        float row1Y = cursor;
        float row2Y = cursor + rowHeight;
        float linkCenterY = row1Y + 24f;
        Rect linkButtonRect = new Rect(x + labelWidth + labelToButtonGap, linkCenterY - linkButtonSize * 0.5f, linkButtonSize, linkButtonSize);
        float sliderX = linkButtonRect.xMax + gap;

        float originalScaleX = layer.scaleX;
        float originalScaleY = layer.scaleY;

        if (!sliderBuffers.ContainsKey("sx"))
            sliderBuffers["sx"] = layer.scaleX.ToString("G3");
        if (!sliderBuffers.ContainsKey("sy"))
            sliderBuffers["sy"] = layer.scaleY.ToString("G3");

        Text.Font = GameFont.Tiny;
        Widgets.Label(new Rect(x, row1Y, labelWidth, 22f), "CoA_ScaleX".Translate());
        Widgets.Label(new Rect(x, row2Y, labelWidth, 22f), "CoA_ScaleY".Translate());
        Text.Font = GameFont.Small;

        Rect sliderXRect = new Rect(sliderX, row1Y, sliderWidth, 22f);
        layer.scaleX = Widgets.HorizontalSlider(sliderXRect, layer.scaleX, 0.1f, 2f, false, null, null, null, -1f);
        if (scaleLinked && Mathf.Abs(layer.scaleX - originalScaleX) > 0.0001f)
        {
            layer.scaleY = layer.scaleX;
            layer.scale = layer.scaleX;
            sliderBuffers["sy"] = layer.scaleY.ToString("G3");
        }
        if (Mathf.Abs(layer.scaleX - originalScaleX) > 0.0001f)
            sliderBuffers["sx"] = layer.scaleX.ToString("G3");

        Rect inputXRect = new Rect(sliderX + sliderWidth + gap, row1Y, inputWidth, 22f);
        string bufferX = sliderBuffers["sx"];
        string newTextX = Widgets.TextField(inputXRect, bufferX);
        if (newTextX != bufferX)
        {
            sliderBuffers["sx"] = newTextX;
            if (float.TryParse(newTextX.Trim(), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float parsedX))
            {
                layer.scaleX = Mathf.Clamp(parsedX, 0.1f, 2f);
                sliderBuffers["sx"] = layer.scaleX.ToString("G3");
                if (scaleLinked)
                {
                    layer.scaleY = layer.scaleX;
                    layer.scale = layer.scaleX;
                    sliderBuffers["sy"] = layer.scaleY.ToString("G3");
                }
            }
        }

        Rect sliderYRect = new Rect(sliderX, row2Y, sliderWidth, 22f);
        layer.scaleY = Widgets.HorizontalSlider(sliderYRect, layer.scaleY, 0.1f, 2f, false, null, null, null, -1f);
        if (scaleLinked && Mathf.Abs(layer.scaleY - originalScaleY) > 0.0001f)
        {
            layer.scaleX = layer.scaleY;
            layer.scale = layer.scaleY;
            sliderBuffers["sx"] = layer.scaleX.ToString("G3");
        }
        if (Mathf.Abs(layer.scaleY - originalScaleY) > 0.0001f)
            sliderBuffers["sy"] = layer.scaleY.ToString("G3");

        Rect inputYRect = new Rect(sliderX + sliderWidth + gap, row2Y, inputWidth, 22f);
        string bufferY = sliderBuffers["sy"];
        string newTextY = Widgets.TextField(inputYRect, bufferY);
        if (newTextY != bufferY)
        {
            sliderBuffers["sy"] = newTextY;
            if (float.TryParse(newTextY.Trim(), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float parsedY))
            {
                layer.scaleY = Mathf.Clamp(parsedY, 0.1f, 2f);
                sliderBuffers["sy"] = layer.scaleY.ToString("G3");
                if (scaleLinked)
                {
                    layer.scaleX = layer.scaleY;
                    layer.scale = layer.scaleY;
                    sliderBuffers["sx"] = layer.scaleX.ToString("G3");
                }
            }
        }

        DrawScaleLinkButton(linkButtonRect);

        if (Widgets.ButtonInvisible(linkButtonRect))
        {
            scaleLinked = !scaleLinked;
            SoundDefOf.Tick_Tiny.PlayOneShotOnCamera();
        }

        if (Mouse.IsOver(linkButtonRect))
            TooltipHandler.TipRegion(linkButtonRect, scaleLinked ? "CoA_ScaleUnlink".Translate() : "CoA_ScaleLink".Translate());

        cursor += rowHeight * 2f;
        return Mathf.Abs(layer.scaleX - originalScaleX) > 0.001f || Mathf.Abs(layer.scaleY - originalScaleY) > 0.001f;
    }

    private static void DrawScaleLinkButton(Rect rect)
    {
        Widgets.DrawHighlightIfMouseover(rect);

        Texture2D linkedIcon = linkedScaleIcon ?? (linkedScaleIcon = ContentFinder<Texture2D>.Get("CoatOfArms/UI/Linked", false));
        Texture2D unlinkedIcon = unlinkedScaleIcon ?? (unlinkedScaleIcon = ContentFinder<Texture2D>.Get("CoatOfArms/UI/Unlinked", false));

        Texture2D icon = scaleLinked ? linkedIcon : unlinkedIcon;
        if (icon != null)
        {
            float size = Mathf.Min(rect.width, rect.height);
            float aspect = (float)icon.width / icon.height;
            float drawWidth = aspect >= 1f ? size : size * aspect;
            float drawHeight = aspect >= 1f ? size / aspect : size;
            if (drawWidth > rect.width)
            {
                drawWidth = rect.width;
                drawHeight = rect.width / aspect;
            }
            if (drawHeight > rect.height)
            {
                drawHeight = rect.height;
                drawWidth = rect.height * aspect;
            }
            float left = rect.x + (rect.width - drawWidth) * 0.5f;
            float top = rect.y + (rect.height - drawHeight) * 0.5f;
            Rect iconRect = new Rect(left, top, drawWidth, drawHeight);
            GUI.color = Color.white;
            GUI.DrawTexture(iconRect, icon);
            return;
        }

        float cx = rect.x + rect.width * 0.5f;
        float cy = rect.y + rect.height * 0.5f;
        float barWidth = 10f;
        float barHeight = 12f;
        float gap = 5f;
        float overlap = 5f;

        if (scaleLinked)
        {
            float topY = cy - barHeight - overlap * 0.5f;
            float bottomY = cy - overlap * 0.5f;
            Rect topBar = new Rect(cx - barWidth * 0.5f, topY, barWidth, barHeight);
            Rect bottomBar = new Rect(cx - barWidth * 0.5f, bottomY, barWidth, barHeight);
            Rect overlapFill = new Rect(cx - barWidth * 0.5f, bottomY, barWidth, overlap);
            GUI.color = Color.white;
            Widgets.DrawBox(topBar, 1);
            Widgets.DrawBox(bottomBar, 1);
            Widgets.DrawBoxSolid(overlapFill, Color.white);
        }
        else
        {
            float topY = cy - barHeight - gap * 0.5f;
            float bottomY = cy + gap * 0.5f;
            Rect topBar = new Rect(cx - barWidth * 0.5f, topY, barWidth, barHeight);
            Rect bottomBar = new Rect(cx - barWidth * 0.5f, bottomY, barWidth, barHeight);
            GUI.color = Color.white;
            Widgets.DrawBox(topBar, 1);
            Widgets.DrawBox(bottomBar, 1);
        }
        GUI.color = Color.white;
    }
}
