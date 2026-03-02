using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace CoatOfArms;

public class CoatOfArmsData : IExposable
{
    public BackgroundLayer background = new BackgroundLayer();
    public List<EmblemLayer> emblems = new List<EmblemLayer>();
    public FrameDef frame;
    public bool showOutline;
    public float outlineThickness = 1f;

    public void ExposeData()
    {
        Scribe_Deep.Look(ref background, "background");
        Scribe_Collections.Look(ref emblems, "emblems", LookMode.Deep);
        Scribe_Defs.Look(ref frame, "frame");
        Scribe_Values.Look(ref showOutline, "showOutline", false);
        Scribe_Values.Look(ref outlineThickness, "outlineThickness", 1f);

        if (Scribe.mode == LoadSaveMode.PostLoadInit)
        {
            background ??= new BackgroundLayer();
            emblems ??= new List<EmblemLayer>();
        }
    }

    public CoatOfArmsData Clone()
    {
        CoatOfArmsData copy = new CoatOfArmsData();
        copy.background = background.Clone();
        copy.frame = frame;
        copy.showOutline = showOutline;
        copy.outlineThickness = outlineThickness;
        copy.emblems = new List<EmblemLayer>();
        foreach (EmblemLayer layer in emblems)
        {
            copy.emblems.Add(layer.Clone());
        }
        return copy;
    }

    /// <summary>Replaces this instance's background, frame, and emblems with cloned data from another.</summary>
    public void ApplyFrom(CoatOfArmsData other)
    {
        if (other == null)
            return;
        background = other.background.Clone();
        frame = other.frame;
        showOutline = other.showOutline;
        outlineThickness = other.outlineThickness;
        emblems.Clear();
        foreach (EmblemLayer layer in other.emblems)
            emblems.Add(layer.Clone());
    }
}
