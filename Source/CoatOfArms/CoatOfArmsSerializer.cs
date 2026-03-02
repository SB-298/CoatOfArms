using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;
using Verse;

namespace CoatOfArms;

public static class CoatOfArmsSerializer
{
    private const string VersionPrefix = "v=";
    private const int FormatVersion = 1;

    public static string ToString(CoatOfArmsData data)
    {
        if (data == null)
            return string.Empty;

        StringBuilder builder = new StringBuilder();
        builder.Append(VersionPrefix).Append(FormatVersion).AppendLine();
        builder.Append("f=").AppendLine(data.frame?.defName ?? "None");
        builder.Append("outline=").AppendLine(data.showOutline ? "1" : "0");
        builder.Append("ot=").AppendLine(data.outlineThickness.ToString("R"));
        builder.Append("bp=").AppendLine(data.background.pattern?.defName ?? "None");
        builder.Append("pr=").AppendLine(ColorToString(data.background.primary));
        builder.Append("ps=").AppendLine(ColorToString(data.background.secondary));
        builder.Append("pt=").AppendLine(ColorToString(data.background.tertiary));
        builder.Append("ec=").AppendLine(data.emblems.Count.ToString());

        for (int i = 0; i < data.emblems.Count; i++)
        {
            EmblemLayer layer = data.emblems[i];
            string prefix = "e" + i + "_";
            builder.Append(prefix).Append("d=").AppendLine(layer.emblem?.defName ?? "Circle");
            builder.Append(prefix).Append("c=").AppendLine(ColorToString(layer.color));
            builder.Append(prefix).Append("x=").AppendLine(layer.position.x.ToString("R"));
            builder.Append(prefix).Append("y=").AppendLine(layer.position.y.ToString("R"));
            builder.Append(prefix).Append("r=").AppendLine(layer.rotation.ToString("R"));
            builder.Append(prefix).Append("s=").AppendLine(layer.scale.ToString("R"));
            builder.Append(prefix).Append("sx=").AppendLine(layer.scaleX.ToString("R"));
            builder.Append(prefix).Append("sy=").AppendLine(layer.scaleY.ToString("R"));
            builder.Append(prefix).Append("fx=").AppendLine(layer.flipX ? "1" : "0");
            builder.Append(prefix).Append("fy=").AppendLine(layer.flipY ? "1" : "0");
        }

        return builder.ToString();
    }

    public static bool TryParse(string value, out CoatOfArmsData data)
    {
        data = null;
        if (string.IsNullOrWhiteSpace(value))
            return false;

        Dictionary<string, string> pairs = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        using (StringReader reader = new StringReader(value))
        {
            string line;
            while ((line = reader.ReadLine()) != null)
            {
                line = line.Trim();
                if (line.Length == 0)
                    continue;
                int eq = line.IndexOf('=');
                if (eq <= 0)
                    continue;
                string key = line.Substring(0, eq).Trim();
                string val = line.Substring(eq + 1).Trim();
                pairs[key] = val;
            }
        }

        if (!pairs.TryGetValue("v", out string verStr) || !int.TryParse(verStr, out int ver) || ver != FormatVersion)
            return false;

        data = new CoatOfArmsData();

        string frameName = pairs.TryGetValue("f", out string f) ? f : "Square";
        if (frameName == "None" || string.IsNullOrEmpty(frameName))
            data.frame = null;
        else
            data.frame = DefDatabase<FrameDef>.GetNamedSilentFail(frameName) ?? DefDatabase<FrameDef>.GetNamedSilentFail("Square");
        data.showOutline = pairs.TryGetValue("outline", out string outlineStr) && outlineStr == "1";
        float parsedThickness = ParseFloat(pairs, "ot", 1f);
        data.outlineThickness = Mathf.Clamp(parsedThickness, 0.25f, 10f);

        string patternName = pairs.TryGetValue("bp", out string bp) ? bp : "Solid";
        if (patternName == "None" || string.IsNullOrEmpty(patternName))
            data.background.pattern = null;
        else
            data.background.pattern = DefDatabase<BackgroundPatternDef>.GetNamedSilentFail(patternName) ?? DefDatabase<BackgroundPatternDef>.GetNamedSilentFail("Solid");
        data.background.primary = ParseColor(pairs, "pr", new Color(0.2f, 0.4f, 0.8f, 1f));
        data.background.secondary = ParseColor(pairs, "ps", new Color(248f / 255f, 252f / 255f, 0f, 1f));
        data.background.tertiary = ParseColor(pairs, "pt", new Color(0.8f, 0.1f, 0.1f, 1f));

        int emblemCount = 0;
        if (pairs.TryGetValue("ec", out string ecStr))
            int.TryParse(ecStr, out emblemCount);

        data.emblems.Clear();
        for (int i = 0; i < emblemCount; i++)
        {
            string prefix = "e" + i + "_";
            EmblemLayer layer = new EmblemLayer();
            string defName = pairs.TryGetValue(prefix + "d", out string d) ? d : "Circle";
            EmblemDef emblem = DefDatabase<EmblemDef>.GetNamedSilentFail(defName);
            if (emblem == null || !emblem.IsAvailableAndLoaded())
                emblem = DefDatabase<EmblemDef>.AllDefs.FirstOrDefault(ed => ed.IsAvailableAndLoaded());
            if (emblem == null)
                emblem = DefDatabase<EmblemDef>.GetNamedSilentFail("Circle");
            layer.emblem = emblem;
            layer.color = ParseColor(pairs, prefix + "c", Color.white);
            layer.position.x = ParseFloat(pairs, prefix + "x", 0f);
            layer.position.y = ParseFloat(pairs, prefix + "y", 0f);
            layer.rotation = ParseFloat(pairs, prefix + "r", 0f);
            layer.scale = ParseFloat(pairs, prefix + "s", 1f);
            float parsedScale = layer.scale;
            layer.scaleX = ParseFloat(pairs, prefix + "sx", parsedScale);
            layer.scaleY = ParseFloat(pairs, prefix + "sy", parsedScale);
            layer.flipX = pairs.TryGetValue(prefix + "fx", out string fx) && fx == "1";
            layer.flipY = pairs.TryGetValue(prefix + "fy", out string fy) && fy == "1";
            data.emblems.Add(layer);
        }

        return true;
    }

    private static string ColorToString(Color c)
    {
        return c.r.ToString("R") + "," + c.g.ToString("R") + "," + c.b.ToString("R") + "," + c.a.ToString("R");
    }

    private static Color ParseColor(Dictionary<string, string> pairs, string key, Color fallback)
    {
        if (!pairs.TryGetValue(key, out string raw))
            return fallback;
        string[] parts = raw.Split(',');
        if (parts.Length != 4)
            return fallback;
        if (!float.TryParse(parts[0].Trim(), out float r)) r = fallback.r;
        if (!float.TryParse(parts[1].Trim(), out float g)) g = fallback.g;
        if (!float.TryParse(parts[2].Trim(), out float b)) b = fallback.b;
        if (!float.TryParse(parts[3].Trim(), out float a)) a = fallback.a;
        return new Color(r, g, b, a);
    }

    private static float ParseFloat(Dictionary<string, string> pairs, string key, float fallback)
    {
        if (!pairs.TryGetValue(key, out string raw))
            return fallback;
        return float.TryParse(raw.Trim(), out float v) ? v : fallback;
    }
}
