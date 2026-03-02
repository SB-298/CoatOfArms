using UnityEngine;
using Verse;

namespace CoatOfArms;

public class BackgroundLayer : IExposable
{
    public BackgroundPatternDef pattern;
    public Color primary = new Color(0.2f, 0.4f, 0.8f);
    public Color secondary = new Color(248f / 255f, 252f / 255f, 0f, 1f);
    public Color tertiary = new Color(0.8f, 0.1f, 0.1f);

    public void ExposeData()
    {
        Scribe_Defs.Look(ref pattern, "pattern");
        Scribe_Values.Look(ref primary, "primary");
        Scribe_Values.Look(ref secondary, "secondary");
        Scribe_Values.Look(ref tertiary, "tertiary");
    }

    public BackgroundLayer Clone()
    {
        BackgroundLayer copy = new BackgroundLayer();
        copy.pattern = pattern;
        copy.primary = primary;
        copy.secondary = secondary;
        copy.tertiary = tertiary;
        return copy;
    }
}
