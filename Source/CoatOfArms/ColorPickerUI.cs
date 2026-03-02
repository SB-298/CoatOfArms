using System.Collections.Generic;
using System.Globalization;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace CoatOfArms;

[StaticConstructorOnStartup]
public static class ColorPickerUI
{
    private static Texture2D hueStripTexture;
    private static Dictionary<string, Texture2D> valueStripTextures = new Dictionary<string, Texture2D>();
    private static Dictionary<string, float> valueStripLastH = new Dictionary<string, float>();
    private static Dictionary<string, string> hexBuffers = new Dictionary<string, string>();
    private static Dictionary<string, string> rgbBuffers = new Dictionary<string, string>();
    private static string draggingHueKey;
    private static string draggingValueKey;

    public static string ColorToHex(Color c)
    {
        int r = Mathf.Clamp(Mathf.RoundToInt(c.r * 255f), 0, 255);
        int g = Mathf.Clamp(Mathf.RoundToInt(c.g * 255f), 0, 255);
        int b = Mathf.Clamp(Mathf.RoundToInt(c.b * 255f), 0, 255);
        return r.ToString("X2") + g.ToString("X2") + b.ToString("X2");
    }

    public static bool TryParseHex(string raw, out Color color)
    {
        color = Color.white;
        if (string.IsNullOrWhiteSpace(raw))
            return false;
        raw = raw.Trim().TrimStart('#');
        if (raw.Length != 6 && raw.Length != 8)
            return false;
        if (!int.TryParse(raw.Substring(0, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out int r))
            return false;
        if (!int.TryParse(raw.Substring(2, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out int g))
            return false;
        if (!int.TryParse(raw.Substring(4, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out int b))
            return false;
        float a = 1f;
        if (raw.Length == 8 && int.TryParse(raw.Substring(6, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out int ai))
            a = ai / 255f;
        color = new Color(r / 255f, g / 255f, b / 255f, a);
        return true;
    }

    public static bool DrawColorPicker(Rect rect, ref Color color, string bufferKey)
    {
        bool changed = false;
        string hexKey = bufferKey + "_hex";

        if (!hexBuffers.ContainsKey(hexKey) || ColorChangedFromBuffers(bufferKey, color))
            SyncBuffersFromColor(bufferKey, color);

        float cursor = rect.y;
        const float rowHeight = 24f;
        const float swatchSize = 20f;
        const float swatchSpacing = 2f;

        Rect swatchRow = new Rect(rect.x, cursor, rect.width, rowHeight);
        float swatchX = rect.x;
        foreach (Color candidate in ColorPalette.Colors)
        {
            Rect swatch = new Rect(swatchX, cursor + 2f, swatchSize, swatchSize);
            Widgets.DrawBoxSolid(swatch, candidate);
            if (ColorClose(color, candidate))
                Widgets.DrawBox(swatch, 2);
            Widgets.DrawHighlightIfMouseover(swatch);
            if (Widgets.ButtonInvisible(swatch))
            {
                if (bufferKey == "emblem")
                    color = new Color(candidate.r, candidate.g, candidate.b, color.a);
                else
                    color = candidate;
                SyncBuffersFromColor(bufferKey, color);
                SoundDefOf.Tick_High.PlayOneShotOnCamera();
                changed = true;
            }
            swatchX += swatchSize + swatchSpacing;
            if (swatchX + swatchSize > rect.xMax)
                break;
        }
        cursor += rowHeight + 4f;

        const float labelW = 28f;
        const float hexW = 72f;
        const float rgbW = 36f;
        float gap = 4f;

        Rect hexLabelRect = new Rect(rect.x, cursor, labelW, rowHeight);
        Widgets.Label(hexLabelRect, "CoA_Hex".Translate());
        Rect hexInputRect = new Rect(rect.x + labelW, cursor, hexW, rowHeight);
        string hexBuffer = hexBuffers[hexKey];
        string newHex = Widgets.TextField(hexInputRect, hexBuffer);
        if (newHex != hexBuffer)
        {
            hexBuffers[hexKey] = newHex;
            if (TryParseHex(newHex, out Color parsed))
            {
                color = parsed;
                SyncBuffersFromColor(bufferKey, color);
                changed = true;
            }
        }
        cursor += rowHeight + 4f;

        Rect rLabelRect = new Rect(rect.x, cursor, 12f, rowHeight);
        Rect rInputRect = new Rect(rect.x + 14f, cursor, rgbW, rowHeight);
        Rect gLabelRect = new Rect(rect.x + 14f + rgbW + gap, cursor, 12f, rowHeight);
        Rect gInputRect = new Rect(rect.x + 28f + rgbW + gap, cursor, rgbW, rowHeight);
        Rect bLabelRect = new Rect(rect.x + 28f + (rgbW + gap) * 2f, cursor, 12f, rowHeight);
        Rect bInputRect = new Rect(rect.x + 40f + (rgbW + gap) * 2f, cursor, rgbW, rowHeight);

        Text.Font = GameFont.Tiny;
        Widgets.Label(rLabelRect, "R");
        Widgets.Label(gLabelRect, "G");
        Widgets.Label(bLabelRect, "B");
        Text.Font = GameFont.Small;

        int rInt = Mathf.RoundToInt(color.r * 255f);
        int gInt = Mathf.RoundToInt(color.g * 255f);
        int bInt = Mathf.RoundToInt(color.b * 255f);
        string rKey = bufferKey + "_r";
        string gKey = bufferKey + "_g";
        string bKey = bufferKey + "_b";
        if (!rgbBuffers.ContainsKey(rKey)) rgbBuffers[rKey] = rInt.ToString();
        if (!rgbBuffers.ContainsKey(gKey)) rgbBuffers[gKey] = gInt.ToString();
        if (!rgbBuffers.ContainsKey(bKey)) rgbBuffers[bKey] = bInt.ToString();
        string rBuf = rgbBuffers[rKey];
        string gBuf = rgbBuffers[gKey];
        string bBuf = rgbBuffers[bKey];
        string rNew = Widgets.TextField(rInputRect, rBuf);
        string gNew = Widgets.TextField(gInputRect, gBuf);
        string bNew = Widgets.TextField(bInputRect, bBuf);
        if (rNew != rBuf || gNew != gBuf || bNew != bBuf)
        {
            rgbBuffers[rKey] = rNew;
            rgbBuffers[gKey] = gNew;
            rgbBuffers[bKey] = bNew;
            if (int.TryParse(rNew.Trim(), out int rr) && int.TryParse(gNew.Trim(), out int gg) && int.TryParse(bNew.Trim(), out int bb))
            {
                rr = Mathf.Clamp(rr, 0, 255);
                gg = Mathf.Clamp(gg, 0, 255);
                bb = Mathf.Clamp(bb, 0, 255);
                color = new Color(rr / 255f, gg / 255f, bb / 255f, color.a);
                hexBuffers[hexKey] = ColorToHex(color);
                changed = true;
            }
        }
        cursor += rowHeight + 4f;

        float stripHeight = 14f;
        Rect hueStripRect = new Rect(rect.x, cursor, rect.width, stripHeight);
        if (DrawHueStrip(hueStripRect, ref color, bufferKey))
            changed = true;
        cursor += stripHeight + 4f;

        Rect valueStripRect = new Rect(rect.x, cursor, rect.width, stripHeight);
        if (DrawValueStrip(valueStripRect, ref color, bufferKey))
            changed = true;

        return changed;
    }

    private static void SyncBuffersFromColor(string bufferKey, Color c)
    {
        hexBuffers[bufferKey + "_hex"] = ColorToHex(c);
        rgbBuffers[bufferKey + "_r"] = Mathf.RoundToInt(c.r * 255f).ToString();
        rgbBuffers[bufferKey + "_g"] = Mathf.RoundToInt(c.g * 255f).ToString();
        rgbBuffers[bufferKey + "_b"] = Mathf.RoundToInt(c.b * 255f).ToString();
    }

    private static bool ColorChangedFromBuffers(string bufferKey, Color c)
    {
        if (!hexBuffers.TryGetValue(bufferKey + "_hex", out string hex))
            return false;
        if (!TryParseHex(hex, out Color parsed))
            return false;
        return !ColorClose(c, parsed);
    }

    private static bool DrawHueStrip(Rect rect, ref Color color, string bufferKey)
    {
        if (hueStripTexture == null)
        {
            int w = 256;
            hueStripTexture = new Texture2D(w, 1);
            for (int x = 0; x < w; x++)
            {
                float hueVal = (float)x / w;
                hueStripTexture.SetPixel(x, 0, Color.HSVToRGB(hueVal, 1f, 1f));
            }
            hueStripTexture.Apply();
            hueStripTexture.wrapMode = TextureWrapMode.Clamp;
        }

        GUI.DrawTexture(rect, hueStripTexture);
        Widgets.DrawBox(rect, 1);

        Color.RGBToHSV(color, out float h, out float s, out float v);
        float cursorX = rect.x + h * rect.width;
        Rect indicator = new Rect(cursorX - 2f, rect.y - 1f, 4f, rect.height + 2f);
        Widgets.DrawBoxSolid(indicator, Color.white);

        bool changed = false;
        Event ev = Event.current;
        bool isActive = draggingHueKey == bufferKey;
        bool mouseOver = StripContainsMouse(rect);

        if (ev.type == EventType.MouseDown && ev.button == 0 && mouseOver)
        {
            draggingHueKey = bufferKey;
            ev.Use();
            changed = ApplyHueFromStrip(rect, ref color, bufferKey);
            if (changed)
                SoundDefOf.Tick_Tiny.PlayOneShotOnCamera();
        }
        else if (ev.type == EventType.MouseDrag && isActive)
        {
            changed = ApplyHueFromStrip(rect, ref color, bufferKey);
            ev.Use();
        }
        else if (ev.type == EventType.MouseUp && ev.button == 0 && isActive)
        {
            draggingHueKey = null;
            ev.Use();
        }
        else if (Widgets.ButtonInvisible(rect))
        {
            changed = ApplyHueFromStrip(rect, ref color, bufferKey);
            if (changed)
                SoundDefOf.Tick_Tiny.PlayOneShotOnCamera();
        }

        return changed;
    }

    private static bool ApplyHueFromStrip(Rect rect, ref Color color, string bufferKey)
    {
        float prevA = color.a;
        float newH = StripPositionNormalized(rect);
        Color.RGBToHSV(color, out float outH, out float outS, out float outV);
        if (outS < 0.05f && outV > 0.95f)
            color = Color.HSVToRGB(newH, 1f, 1f);
        else
            color = Color.HSVToRGB(newH, Mathf.Max(0.05f, outS), Mathf.Max(0.05f, outV));
        if (bufferKey != "emblem")
            color.a = 1f;
        else
            color.a = prevA;
        SyncBuffersFromColor(bufferKey, color);
        return true;
    }

    private static bool DrawValueStrip(Rect rect, ref Color color, string bufferKey)
    {
        if (!valueStripTextures.TryGetValue(bufferKey, out Texture2D valueStripTexture))
        {
            valueStripTexture = new Texture2D(256, 1);
            valueStripTexture.wrapMode = TextureWrapMode.Clamp;
            valueStripTextures[bufferKey] = valueStripTexture;
        }

        Color.RGBToHSV(color, out float h, out float s, out float v);
        Color saturatedColor = Color.HSVToRGB(h, 1f, 1f);
        bool needRebuild = !valueStripLastH.TryGetValue(bufferKey, out float lastH) || Mathf.Abs(h - lastH) > 0.001f;
        if (needRebuild)
        {
            valueStripLastH[bufferKey] = h;
            int w = valueStripTexture.width;
            for (int x = 0; x < w; x++)
            {
                float t = (float)x / (w - 1);
                Color pixel;
                if (t <= 0.5f)
                {
                    float u = t * 2f;
                    pixel = Color.Lerp(Color.black, saturatedColor, u);
                }
                else
                {
                    float u = (t - 0.5f) * 2f;
                    pixel = Color.Lerp(saturatedColor, Color.white, u);
                }
                valueStripTexture.SetPixel(x, 0, pixel);
            }
            valueStripTexture.Apply();
        }

        GUI.DrawTexture(rect, valueStripTexture);
        Widgets.DrawBox(rect, 1);

        float position = ValueStripPositionFromColor(h, s, v);
        float cursorX = rect.x + position * rect.width;
        Rect indicator = new Rect(cursorX - 2f, rect.y - 1f, 4f, rect.height + 2f);
        Widgets.DrawBoxSolid(indicator, position > 0.5f ? Color.black : Color.white);

        bool changed = false;
        Event ev = Event.current;
        bool isActive = draggingValueKey == bufferKey;
        bool mouseOver = StripContainsMouse(rect);

        if (ev.type == EventType.MouseDown && ev.button == 0 && mouseOver)
        {
            draggingValueKey = bufferKey;
            ev.Use();
            changed = ApplyValueFromStrip(rect, ref color, bufferKey);
            if (changed)
                SoundDefOf.Tick_Tiny.PlayOneShotOnCamera();
        }
        else if (ev.type == EventType.MouseDrag && isActive)
        {
            changed = ApplyValueFromStrip(rect, ref color, bufferKey);
            ev.Use();
        }
        else if (ev.type == EventType.MouseUp && ev.button == 0 && isActive)
        {
            draggingValueKey = null;
            ev.Use();
        }
        else if (Widgets.ButtonInvisible(rect))
        {
            changed = ApplyValueFromStrip(rect, ref color, bufferKey);
            if (changed)
                SoundDefOf.Tick_Tiny.PlayOneShotOnCamera();
        }

        return changed;
    }

    private static float ValueStripPositionFromColor(float h, float s, float v)
    {
        if (v >= 0.99f && s < 1f)
            return 0.5f + (1f - s) * 0.5f;
        return v * 0.5f;
    }

    private static bool ApplyValueFromStrip(Rect rect, ref Color color, string bufferKey)
    {
        float prevA = color.a;
        float position = StripPositionNormalized(rect);
        Color.RGBToHSV(color, out float h, out float outS, out float outV);
        Color saturatedColor = Color.HSVToRGB(h, 1f, 1f);
        if (position <= 0.5f)
        {
            float u = position * 2f;
            color = Color.Lerp(Color.black, saturatedColor, u);
        }
        else
        {
            float u = (position - 0.5f) * 2f;
            color = Color.Lerp(saturatedColor, Color.white, u);
        }
        if (bufferKey != "emblem")
            color.a = 1f;
        else
            color.a = prevA;
        SyncBuffersFromColor(bufferKey, color);
        return true;
    }

    public static bool ColorClose(Color a, Color b)
    {
        return Mathf.Abs(a.r - b.r) < 0.01f && Mathf.Abs(a.g - b.g) < 0.01f && Mathf.Abs(a.b - b.b) < 0.01f;
    }

    /// <summary>Single source: position 0-1 along the strip using Event.current.mousePosition and rect in the same GUI space.</summary>
    private static float StripPositionNormalized(Rect rect)
    {
        float localX = Event.current.mousePosition.x - rect.x;
        float position = rect.width > 0f ? localX / rect.width : 0.5f;
        return Mathf.Clamp01(position);
    }

    private static bool StripContainsMouse(Rect rect)
    {
        return rect.Contains(Event.current.mousePosition);
    }
}
