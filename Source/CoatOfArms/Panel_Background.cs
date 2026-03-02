using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace CoatOfArms;

public static class Panel_Background
{
    private static Vector2 scrollPosition;
    private static Dictionary<string, Texture2D> thumbnailCacheByFrame = new Dictionary<string, Texture2D>();
    private static Texture2D nonePatternThumbnail;
    private static Color cachedPrimary;
    private static Color cachedSecondary;
    private static Color cachedTertiary;
    private static string outlineThicknessBuffer = "1";

    public static bool Draw(Rect rect, CoatOfArmsData data)
    {
        bool changed = false;
        float cursor = rect.y;

        Text.Font = GameFont.Small;
        Widgets.Label(new Rect(rect.x, cursor, rect.width, 24f), "CoA_Frame".Translate());
        cursor += 28f;

        int frameTileCount = 1 + DefDatabase<FrameDef>.AllDefs.Count();
        const int frameTileSize = 48;
        const int frameSpacing = 4;
        int frameTilesPerRow = Mathf.FloorToInt((rect.width + frameSpacing) / (frameTileSize + frameSpacing));
        if (frameTilesPerRow < 1)
            frameTilesPerRow = 1;
        int frameRows = Mathf.CeilToInt((float)frameTileCount / frameTilesPerRow);
        float frameHeight = frameRows * (frameTileSize + frameSpacing) - frameSpacing;

        Rect frameArea = new Rect(rect.x, cursor, rect.width, frameHeight);
        changed |= DrawFrameSelector(frameArea, data);
        cursor += frameHeight + 4f;

        Rect outlineRow = new Rect(rect.x, cursor, rect.width, 24f);
        changed |= DrawOutlineRow(outlineRow, data);
        cursor += 28f;

        if (data.frame == null)
        {
            data.background.pattern = null;
        }

        bool showPattern = data.frame != null;
        if (showPattern)
        {
            if (data.background.pattern == null)
            {
                data.background.pattern = DefDatabase<BackgroundPatternDef>.GetNamedSilentFail("Solid")
                    ?? DefDatabase<BackgroundPatternDef>.AllDefs.OrderBy(d => d.order).FirstOrDefault();
            }

            Text.Font = GameFont.Small;
            Widgets.Label(new Rect(rect.x, cursor, rect.width, 24f), "CoA_BackgroundPattern".Translate());
            cursor += 28f;

            float gridHeight = rect.yMax - cursor - ColorSectionHeight(data) - 8f;
            if (gridHeight < 80f)
                gridHeight = 80f;

            Rect gridArea = new Rect(rect.x, cursor, rect.width, gridHeight);
            changed |= DrawPatternGrid(gridArea, data);
            cursor += gridHeight + 4f;
        }

        if (data.frame != null)
        {
            Rect colorArea = new Rect(rect.x, cursor, rect.width, rect.yMax - cursor);
            changed |= DrawColorPickers(colorArea, data);
        }

        return changed;
    }

    private static bool DrawOutlineRow(Rect rect, CoatOfArmsData data)
    {
        bool changed = false;
        float left = rect.x;
        float buttonSize = 24f;

        bool oldShowOutline = data.showOutline;
        Rect checkRect = new Rect(left, rect.y, 24f, rect.height);
        Widgets.Checkbox(checkRect.position, ref data.showOutline);
        if (oldShowOutline != data.showOutline)
        {
            changed = true;
            if (data.showOutline && data.outlineThickness < 0.25f)
            {
                data.outlineThickness = 1f;
                outlineThicknessBuffer = "1";
            }
        }
        left += 28f;

        float labelWidth = Text.CalcSize("CoA_Outline".Translate()).x + 4f;
        Rect labelRect = new Rect(left, rect.y, labelWidth, rect.height);
        Widgets.Label(labelRect, "CoA_Outline".Translate());
        left += labelWidth + 8f;

        Rect weightLabelRect = new Rect(left, rect.y, Text.CalcSize("CoA_OutlineWeight".Translate()).x + 4f, rect.height);
        Widgets.Label(weightLabelRect, "CoA_OutlineWeight".Translate());
        left += weightLabelRect.width + 4f;

        Rect decRect = new Rect(left, rect.y, buttonSize, buttonSize);
        if (Widgets.ButtonText(decRect, "-"))
        {
            data.outlineThickness = DecrementOutlineThickness(data.outlineThickness);
            outlineThicknessBuffer = data.outlineThickness.ToString("G4");
            SoundDefOf.Tick_Tiny.PlayOneShotOnCamera();
            changed = true;
        }
        left += buttonSize + 2f;

        Rect inputRect = new Rect(left, rect.y, 48f, rect.height);
        GUI.SetNextControlName("outlineThickness");
        outlineThicknessBuffer = Widgets.TextField(inputRect, outlineThicknessBuffer);
        if (GUI.GetNameOfFocusedControl() != "outlineThickness")
            outlineThicknessBuffer = data.outlineThickness.ToString("G4");
        if (float.TryParse(outlineThicknessBuffer, out float parsed))
        {
            float clamped = Mathf.Clamp(parsed, 0.25f, 10f);
            if (Mathf.Abs(clamped - data.outlineThickness) > 0.001f)
            {
                data.outlineThickness = clamped;
                changed = true;
            }
        }
        left += 52f;

        Rect incRect = new Rect(left, rect.y, buttonSize, buttonSize);
        if (Widgets.ButtonText(incRect, "+"))
        {
            data.outlineThickness = IncrementOutlineThickness(data.outlineThickness);
            outlineThicknessBuffer = data.outlineThickness.ToString("G4");
            SoundDefOf.Tick_Tiny.PlayOneShotOnCamera();
            changed = true;
        }

        return changed;
    }

    private static float IncrementOutlineThickness(float value)
    {
        if (value < 1f)
            return Mathf.Min(1f, value + 0.25f);
        return Mathf.Min(10f, value + 1f);
    }

    private static float DecrementOutlineThickness(float value)
    {
        if (value <= 1f)
            return Mathf.Max(0.25f, value - 0.25f);
        return value - 1f;
    }

    private static bool DrawFrameSelector(Rect rect, CoatOfArmsData data)
    {
        bool changed = false;
        const int tileSize = 48;
        const int spacing = 4;

        List<FrameDef> frames = DefDatabase<FrameDef>.AllDefs
            .OrderBy(d => d.order)
            .ToList();

        int totalTiles = 1 + frames.Count;
        int tilesPerRow = Mathf.FloorToInt((rect.width + spacing) / (tileSize + spacing));
        if (tilesPerRow < 1)
            tilesPerRow = 1;

        for (int i = 0; i < totalTiles; i++)
        {
            int row = i / tilesPerRow;
            int col = i % tilesPerRow;
            float x = rect.x + col * (tileSize + spacing);
            float y = rect.y + row * (tileSize + spacing);
            Rect tile = new Rect(x, y, tileSize, tileSize);

            if (i == 0)
            {
                Widgets.DrawBoxSolid(tile, new Color(0.2f, 0.2f, 0.2f));
                Widgets.DrawHighlightIfMouseover(tile);
                if (data.frame == null)
                    Widgets.DrawBox(tile, 2);
                Text.Anchor = TextAnchor.MiddleCenter;
                Text.Font = GameFont.Tiny;
                Widgets.Label(tile, "CoA_None".Translate());
                Text.Font = GameFont.Small;
                Text.Anchor = TextAnchor.UpperLeft;
                if (Widgets.ButtonInvisible(tile))
                {
                    data.frame = null;
                    SoundDefOf.Tick_High.PlayOneShotOnCamera();
                    changed = true;
                }
            }
            else
            {
                FrameDef frame = frames[i - 1];
                Widgets.DrawBoxSolid(tile, new Color(0.2f, 0.2f, 0.2f));
                Widgets.DrawHighlightIfMouseover(tile);

                if (frame == data.frame)
                {
                    Widgets.DrawBox(tile, 2);
                }

                if (frame.Texture != null)
                {
                    Rect inner = tile.ContractedBy(4f);
                    GUI.DrawTexture(inner, frame.Texture);
                }

                if (Widgets.ButtonInvisible(tile))
                {
                    data.frame = frame;
                    SoundDefOf.Tick_High.PlayOneShotOnCamera();
                    changed = true;
                }
            }
        }

        return changed;
    }

    private static float ColorSectionHeight(CoatOfArmsData data)
    {
        int count = data.background.pattern?.colorCount ?? 1;
        const float pickerHeight = 138f;
        return count * pickerHeight + 28f;
    }

    private static bool DrawPatternGrid(Rect rect, CoatOfArmsData data)
    {
        bool changed = false;
        const int tileSize = 48;
        const int spacing = 4;

        List<BackgroundPatternDef> patterns = DefDatabase<BackgroundPatternDef>.AllDefs.OrderBy(d => d.order).ToList();

        int columns = Mathf.FloorToInt((rect.width - 16f) / (tileSize + spacing));
        if (columns < 1)
            columns = 1;

        int rows = Mathf.CeilToInt((float)patterns.Count / columns);
        float viewHeight = rows * (tileSize + spacing);
        Rect viewRect = new Rect(0f, 0f, rect.width - 16f, viewHeight);

        Widgets.BeginScrollView(rect, ref scrollPosition, viewRect);

        for (int i = 0; i < patterns.Count; i++)
        {
            BackgroundPatternDef pattern = patterns[i];
            int col = i % columns;
            int row = i / columns;

            Rect tile = new Rect(
                col * (tileSize + spacing),
                row * (tileSize + spacing),
                tileSize,
                tileSize
            );

            Widgets.DrawBoxSolid(tile, new Color(0.2f, 0.2f, 0.2f));
            Widgets.DrawHighlightIfMouseover(tile);

            if (pattern == data.background.pattern)
            {
                Widgets.DrawBox(tile, 2);
            }

            if (pattern != null)
            {
                Rect inner = tile.ContractedBy(2f);
                DrawPatternThumbnail(inner, pattern, data.background, data.frame);
            }

            if (Widgets.ButtonInvisible(tile))
            {
                data.background.pattern = pattern;
                SoundDefOf.Tick_High.PlayOneShotOnCamera();
                changed = true;
            }
        }

        Widgets.EndScrollView();
        return changed;
    }

    private static void DrawPatternThumbnail(Rect rect, BackgroundPatternDef pattern, BackgroundLayer background, FrameDef frame)
    {
        if (!ColorPickerUI.ColorClose(cachedPrimary, background.primary)
            || !ColorPickerUI.ColorClose(cachedSecondary, background.secondary)
            || !ColorPickerUI.ColorClose(cachedTertiary, background.tertiary))
        {
            foreach (Texture2D texture in thumbnailCacheByFrame.Values)
            {
                if (texture != null)
                    Object.Destroy(texture);
            }
            thumbnailCacheByFrame.Clear();
            if (nonePatternThumbnail != null)
            {
                Object.Destroy(nonePatternThumbnail);
                nonePatternThumbnail = null;
            }
            cachedPrimary = background.primary;
            cachedSecondary = background.secondary;
            cachedTertiary = background.tertiary;
        }

        if (pattern == null)
        {
            if (frame == null)
            {
                Widgets.DrawBoxSolid(rect, background.primary);
            }
            else if (nonePatternThumbnail == null)
            {
                nonePatternThumbnail = new Texture2D(48, 48);
                Color[] fill = new Color[48 * 48];
                for (int i = 0; i < fill.Length; i++)
                    fill[i] = background.primary;
                nonePatternThumbnail.SetPixels(fill);
                nonePatternThumbnail.Apply();
            }
            if (frame != null)
                GUI.DrawTexture(rect, nonePatternThumbnail);
            return;
        }

        string cacheKey = frame != null ? frame.defName : "_null";
        string cacheKeyFull = pattern.defName + "_" + cacheKey;
        if (!thumbnailCacheByFrame.TryGetValue(cacheKeyFull, out Texture2D thumbnail) || thumbnail == null)
        {
            BackgroundLayer temp = new BackgroundLayer();
            temp.pattern = pattern;
            temp.primary = background.primary;
            temp.secondary = background.secondary;
            temp.tertiary = background.tertiary;

            CoatOfArmsData tempData = new CoatOfArmsData();
            tempData.background = temp;
            tempData.frame = frame;

            thumbnail = CoatOfArmsRenderer.Render(tempData, 48);
            thumbnailCacheByFrame[cacheKeyFull] = thumbnail;
        }

        GUI.DrawTexture(rect, thumbnail);
    }

    private static bool DrawColorPickers(Rect rect, CoatOfArmsData data)
    {
        bool changed = false;
        float cursor = rect.y;
        int count = data.background.pattern?.colorCount ?? 1;

        Text.Font = GameFont.Small;
        Widgets.Label(new Rect(rect.x, cursor, rect.width, 24f), "CoA_Colors".Translate());
        cursor += 28f;

        if (count >= 1)
        {
            Rect primaryRect = new Rect(rect.x, cursor, rect.width, 138f);
            changed |= ColorPickerUI.DrawColorPicker(primaryRect, ref data.background.primary, "bg_primary");
            cursor += 142f;
        }

        if (count >= 2)
        {
            Rect secondaryRect = new Rect(rect.x, cursor, rect.width, 138f);
            changed |= ColorPickerUI.DrawColorPicker(secondaryRect, ref data.background.secondary, "bg_secondary");
            cursor += 142f;
        }

        if (count >= 3)
        {
            Rect tertiaryRect = new Rect(rect.x, cursor, rect.width, 138f);
            changed |= ColorPickerUI.DrawColorPicker(tertiaryRect, ref data.background.tertiary, "bg_tertiary");
        }

        return changed;
    }
}
